using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using INCIS_SaaP.Data;
using INCIS_SaaP.Infrastructure.Security.ABAC;
using INCIS_SaaP.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace INCIS_SaaP.Tests.Security;

/// <summary>
/// Tests for the ABAC (attribute-based access control) policy engine.
/// </summary>
public class AbacEngineTests
{
    private static ClaimsPrincipal User(string userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static void AddPolicy(ServiceProvider provider, AbacPolicy policy)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCISDbContext>();
        db.Set<AbacPolicy>().Add(policy);
        db.SaveChanges();
    }

    [Fact]
    public async Task EvaluateAsync_NoPolicies_PermitsByDefault()
    {
        var provider = TestServiceProvider.Create(out _);
        var engine = new AbacEngine(provider, NullLogger<AbacEngine>.Instance);

        var decision = await engine.EvaluateAsync(new AccessDecisionRequest
        {
            Subject = User("u1"),
            ResourceType = "Formula",
            Action = "Read"
        });

        decision.IsPermitted.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_PermitPolicy_WithMatchingRole_Permits()
    {
        var provider = TestServiceProvider.Create(out _);
        AddPolicy(provider, new AbacPolicy
        {
            Name = "AdminRead",
            ResourceType = "Formula",
            Action = "Read",
            Effect = "Permit",
            IsActive = true,
            Priority = 10,
            ConditionRulesJson = JsonSerializer.Serialize(new PolicyConditions
            {
                RequiredRoles = new List<string> { "Admin" }
            })
        });

        var engine = new AbacEngine(provider, NullLogger<AbacEngine>.Instance);

        var decision = await engine.EvaluateAsync(new AccessDecisionRequest
        {
            Subject = User("u1", "Admin"),
            ResourceType = "Formula",
            Action = "Read"
        });

        decision.IsPermitted.Should().BeTrue();
        decision.PolicyName.Should().Be("AdminRead");
    }

    [Fact]
    public async Task EvaluateAsync_PermitPolicy_WithoutMatchingRole_Denies()
    {
        var provider = TestServiceProvider.Create(out _);
        AddPolicy(provider, new AbacPolicy
        {
            Name = "AdminRead",
            ResourceType = "Formula",
            Action = "Read",
            Effect = "Permit",
            IsActive = true,
            ConditionRulesJson = JsonSerializer.Serialize(new PolicyConditions
            {
                RequiredRoles = new List<string> { "Admin" }
            })
        });

        var engine = new AbacEngine(provider, NullLogger<AbacEngine>.Instance);

        var decision = await engine.EvaluateAsync(new AccessDecisionRequest
        {
            Subject = User("u2", "User"),
            ResourceType = "Formula",
            Action = "Read"
        });

        decision.IsPermitted.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_DenyPolicy_InsufficientClearance_Denies()
    {
        var provider = TestServiceProvider.Create(out _);
        AddPolicy(provider, new AbacPolicy
        {
            Name = "HighClearance",
            ResourceType = "Formula",
            Action = "Export",
            Effect = "Deny",
            IsActive = true,
            ConditionRulesJson = JsonSerializer.Serialize(new PolicyConditions
            {
                MinClearanceLevel = 5
            })
        });

        var engine = new AbacEngine(provider, NullLogger<AbacEngine>.Instance);

        var decision = await engine.EvaluateAsync(new AccessDecisionRequest
        {
            Subject = User("u3"),
            ResourceType = "Formula",
            Action = "Export"
        });

        decision.IsPermitted.Should().BeFalse();
        decision.Reason.Should().Contain("clearance");
    }

    [Fact]
    public async Task CanAccessFieldAsync_NoPolicy_ReturnsTrue()
    {
        var provider = TestServiceProvider.Create(out _);
        var engine = new AbacEngine(provider, NullLogger<AbacEngine>.Instance);

        var allowed = await engine.CanAccessFieldAsync(User("u1"), "Formula", "Cost");

        allowed.Should().BeTrue();
    }
}
