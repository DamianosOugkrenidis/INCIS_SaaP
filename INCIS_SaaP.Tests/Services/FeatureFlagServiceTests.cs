using FluentAssertions;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// Tests for FeatureFlagService (config defaults + in-memory cache overrides).
/// </summary>
public class FeatureFlagServiceTests
{
    private static FeatureFlagService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureFlags:AdvancedSearch"] = "true",
                ["FeatureFlags:BetaExport"] = "false"
            })
            .Build();

        var provider = TestServiceProvider.Create(out _);
        return new FeatureFlagService(configuration, provider);
    }

    [Fact]
    public void IsEnabled_ReturnsTrue_ForConfiguredEnabledFlag()
    {
        CreateService().IsEnabled("AdvancedSearch").Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_ReturnsFalse_ForConfiguredDisabledFlag()
    {
        CreateService().IsEnabled("BetaExport").Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_ReturnsFalse_ForUnknownFlag()
    {
        CreateService().IsEnabled("DoesNotExist").Should().BeFalse();
    }

    [Fact]
    public void SetEnabled_UpdatesCache()
    {
        var service = CreateService();

        service.SetEnabled("NewFeature", true);
        service.IsEnabled("NewFeature").Should().BeTrue();

        service.SetEnabled("NewFeature", false);
        service.IsEnabled("NewFeature").Should().BeFalse();
    }

    [Fact]
    public void GetAll_ReturnsConfiguredFlags()
    {
        var all = CreateService().GetAll();

        all.Should().ContainKey("AdvancedSearch");
        all["AdvancedSearch"].Should().BeTrue();
        all.Should().ContainKey("BetaExport");
        all["BetaExport"].Should().BeFalse();
    }
}
