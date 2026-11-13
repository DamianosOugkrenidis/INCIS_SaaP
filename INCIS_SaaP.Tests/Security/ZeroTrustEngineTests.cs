using FluentAssertions;
using INCIS_SaaP.Infrastructure.Security.ZeroTrust;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace INCIS_SaaP.Tests.Security;

/// <summary>
/// Tests for the Zero Trust risk scoring engine (pure ComputeRiskScore).
/// </summary>
public class ZeroTrustEngineTests
{
    private static ZeroTrustEngine CreateEngine()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        return new ZeroTrustEngine(provider, NullLogger<ZeroTrustEngine>.Instance);
    }

    [Fact]
    public void ComputeRiskScore_TrustedInternalRequest_HasLowScore()
    {
        var engine = CreateEngine();
        var context = new TrustVerificationContext
        {
            UserId = "user-1",
            IsInternalNetwork = true,
            CertificateThumbprint = "ABC123",
            RequestTime = new DateTime(2026, 1, 1, 12, 0, 0), // midday
            Action = "Read"
        };

        engine.ComputeRiskScore(context).Should().Be(0);
    }

    [Fact]
    public void ComputeRiskScore_AnonymousExternalNightDelete_HasHighScore()
    {
        var engine = CreateEngine();
        var context = new TrustVerificationContext
        {
            UserId = null,
            ServiceId = null,
            IsInternalNetwork = false,
            CertificateThumbprint = null,
            RequestTime = new DateTime(2026, 1, 1, 3, 0, 0), // night
            Action = "Delete"
        };

        // 50 (no identity) + 15 (external) + 10 (no cert) + 10 (night) + 15 (high-risk action) = 100
        engine.ComputeRiskScore(context).Should().BeGreaterThanOrEqualTo(70);
    }

    [Fact]
    public void ComputeRiskScore_ExternalNetworkAddsRisk()
    {
        var engine = CreateEngine();
        var baseContext = new TrustVerificationContext
        {
            UserId = "u",
            IsInternalNetwork = true,
            CertificateThumbprint = "cert",
            RequestTime = new DateTime(2026, 1, 1, 12, 0, 0),
            Action = "Read"
        };
        var externalContext = new TrustVerificationContext
        {
            UserId = "u",
            IsInternalNetwork = false,
            CertificateThumbprint = "cert",
            RequestTime = new DateTime(2026, 1, 1, 12, 0, 0),
            Action = "Read"
        };

        engine.ComputeRiskScore(externalContext)
            .Should().BeGreaterThan(engine.ComputeRiskScore(baseContext));
    }
}
