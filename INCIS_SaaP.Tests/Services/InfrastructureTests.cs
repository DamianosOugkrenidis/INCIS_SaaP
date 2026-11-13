using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using INCIS_SaaP.Infrastructure.Encryption;
using INCIS_SaaP.Infrastructure.Resilience;
using INCIS_SaaP.Infrastructure.Security.ABAC;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>Unit tests for infrastructure components (resilience, encryption, ABAC).</summary>
public class InfrastructureTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public InfrastructureTests()
    {
        _provider = TestServiceProvider.Create(out _);
    }

    // ---------------- CircuitBreaker ----------------
    [Fact]
    public async Task CircuitBreaker_Success_ReturnsValue_StaysClosed()
    {
        var cb = new CircuitBreaker("test-success", failureThreshold: 2);
        var result = await cb.ExecuteAsync(() => Task.FromResult(42));
        result.Should().Be(42);
        cb.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task CircuitBreaker_OpensAfterThreshold_ThenThrowsOpenException()
    {
        var cb = new CircuitBreaker("test-open", failureThreshold: 2);

        for (int i = 0; i < 2; i++)
        {
            await cb.Invoking(c => c.ExecuteAsync<int>(() => throw new InvalidOperationException("boom")))
                .Should().ThrowAsync<InvalidOperationException>();
        }

        cb.State.Should().Be(CircuitState.Open);

        await cb.Invoking(c => c.ExecuteAsync(() => Task.FromResult(1)))
            .Should().ThrowAsync<CircuitBreakerOpenException>();
    }

    // ---------------- KeyManagement + Tokenization ----------------
    [Fact]
    public async Task KeyManagement_And_Tokenization_RoundTrip()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:KEK"] = Convert.ToBase64String(new byte[32]) // 256-bit test KEK
            })
            .Build();

        var km = new KeyManagementService(_provider, NullLogger<KeyManagementService>.Instance, config);
        var key = await km.GetActiveKeyAsync("test-purpose");
        key.Should().NotBeNullOrEmpty();

        var tok = new TokenizationService(_provider, km, NullLogger<TokenizationService>.Instance);
        var token = await tok.TokenizeAsync("123456789", "AFM");
        token.Should().NotBeNullOrEmpty();
        token.Should().NotBe("123456789");

        var back = await tok.DetokenizeAsync(token);
        back.Should().Be("123456789");
    }

    // ---------------- ABAC ----------------
    [Fact]
    public async Task AbacEngine_Evaluate_ReturnsDecision()
    {
        var subject = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "user-1"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")
            }, "test"));

        var abac = new AbacEngine(_provider, NullLogger<AbacEngine>.Instance);
        var decision = await abac.EvaluateAsync(new AccessDecisionRequest
        {
            Subject = subject,
            ResourceType = "Formula",
            Action = "Read"
        });
        decision.Should().NotBeNull();
    }

    public void Dispose() => _provider.Dispose();
}
