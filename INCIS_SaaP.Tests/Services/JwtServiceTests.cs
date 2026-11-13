using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using INCIS_SaaP.Services;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>Unit tests for JwtService (token generation/validation).</summary>
public class JwtServiceTests
{
    private static JwtService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "SuperSecretTestKey_ForUnitTests_1234567890",
                ["Jwt:Issuer"] = "INCIS_SaaP_Test",
                ["Jwt:Audience"] = "INCIS_SaaP_Test_Users",
                ["Jwt:ExpirationHours"] = "2"
            })
            .Build();
        return new JwtService(config, NullLogger<JwtService>.Instance);
    }

    [Fact]
    public void GenerateToken_ThenValidate_RoundTrips()
    {
        var svc = CreateService();
        var token = svc.GenerateToken("user-123", "u@x.gr", new[] { "Admin", "User" });
        token.Should().NotBeNullOrEmpty();

        var principal = svc.ValidateToken(token);
        principal.Should().NotBeNull();

        svc.GetUserIdFromToken(token).Should().Be("user-123");
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        var svc = CreateService();
        svc.ValidateToken("garbage.token.value").Should().BeNull();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueNonEmpty()
    {
        var svc = CreateService();
        var a = svc.GenerateRefreshToken();
        var b = svc.GenerateRefreshToken();
        a.Should().NotBeNullOrEmpty();
        a.Should().NotBe(b);
    }
}
