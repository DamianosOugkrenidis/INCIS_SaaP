using FluentAssertions;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// Tests for the pure pricing calculations of PriceCalculationService.
/// </summary>
public class PriceCalculationServiceTests
{
    private static PriceCalculationService CreateService()
        => new(TestDbContextFactory.Create());

    [Fact]
    public void CalculateRecommendedRetailPrice_AppliesMargin()
    {
        var service = CreateService();
        // cost 70, margin 30% => 70 / 0.7 = 100
        service.CalculateRecommendedRetailPrice(70m, 0.30m).Should().BeApproximately(100m, 0.001m);
    }

    [Fact]
    public void CalculateProfitMargin_ComputesPercentage()
    {
        var service = CreateService();
        // (100 - 60) / 100 * 100 = 40%
        service.CalculateProfitMargin(60m, 100m).Should().Be(40m);
    }

    [Fact]
    public void CalculateProfitMargin_ZeroSellingPrice_ReturnsZero()
    {
        CreateService().CalculateProfitMargin(50m, 0m).Should().Be(0m);
    }

    [Fact]
    public void CalculateCostPerUnit_DividesTotal()
    {
        CreateService().CalculateCostPerUnit(100m, 4).Should().Be(25m);
    }

    [Fact]
    public void CalculateCostPerUnit_ZeroUnits_ReturnsZero()
    {
        CreateService().CalculateCostPerUnit(100m, 0).Should().Be(0m);
    }

    [Theory]
    [InlineData(100, 10, 90)]
    [InlineData(200, 0, 200)]
    [InlineData(100, -5, 100)]   // invalid discount ignored
    [InlineData(100, 150, 100)]  // invalid discount ignored
    public void ApplyDiscount_HandlesValidAndInvalidPercentages(double price, double discount, double expected)
    {
        CreateService().ApplyDiscount((decimal)price, (decimal)discount)
            .Should().BeApproximately((decimal)expected, 0.001m);
    }

    [Fact]
    public void CalculatePriceWithVAT_UsesCountryRate()
    {
        var service = CreateService();
        var greek = service.CalculatePriceWithVAT(100m, "GR");
        greek.Should().BeGreaterThan(100m);

        // Unknown country falls back to EU default rate (still adds VAT)
        var fallback = service.CalculatePriceWithVAT(100m, "ZZ");
        fallback.Should().BeGreaterThan(100m);
    }
}
