using FluentAssertions;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>Unit tests for calculation/compliance services (pure/list-based methods).</summary>
public class CalculationServicesTests : IDisposable
{
    private readonly Data.INCISDbContext _context;

    public CalculationServicesTests()
    {
        _context = TestDbContextFactory.Create();
    }

    // ---------------- PriceCalculationService ----------------
    [Fact]
    public void Price_RecommendedRetail_AppliesMargin()
    {
        var svc = new PriceCalculationService(_context);
        var price = svc.CalculateRecommendedRetailPrice(100m, 0.30m);
        price.Should().BeGreaterThan(100m);
    }

    [Fact]
    public void Price_ProfitMargin_And_CostPerUnit_And_Discount()
    {
        var svc = new PriceCalculationService(_context);
        svc.CalculateProfitMargin(100m, 150m).Should().BeGreaterThan(0);
        svc.CalculateCostPerUnit(200m, 10).Should().Be(20m);
        svc.ApplyDiscount(100m, 10m).Should().Be(90m);
    }

    [Fact]
    public void Price_WithVAT_GreeceAddsVat()
    {
        var svc = new PriceCalculationService(_context);
        var withVat = svc.CalculatePriceWithVAT(100m, "GR");
        withVat.Should().BeGreaterThan(100m);
    }

    // ---------------- HeavyMetalComplianceService ----------------
    [Fact]
    public void HeavyMetal_Limits_AreDefined()
    {
        HeavyMetalComplianceService.GetAllLimits().Should().NotBeEmpty();
        HeavyMetalComplianceService.HasLimit("Lead").Should().BeTrue();
        HeavyMetalComplianceService.HasLimit("Unobtainium").Should().BeFalse();
    }

    [Fact]
    public void HeavyMetal_CheckCompliance_List_Works()
    {
        var svc = new HeavyMetalComplianceService(_context);
        var metals = new List<HeavyMetal>
        {
            new() { MetalName = "Lead", LimitValue = 10m, DetectedValue = 2m, Unit = "ppm" },
            new() { MetalName = "Mercury", LimitValue = 1m, DetectedValue = 5m, Unit = "ppm" }
        };
        var result = svc.CheckCompliance(metals);
        result.Should().NotBeNull();

        svc.CalculateTotalHeavyMetalBurden(metals).Should().BeGreaterThan(0);
        svc.CheckMetalCompliance(metals[0]).Should().NotBeNull();
        svc.RequiresHeavyMetalTesting("Leave on"); // just exercise the method
    }

    // ---------------- AllergenService ----------------
    [Fact]
    public void Allergen_EuList_IsPopulated()
    {
        AllergenService.GetEUDeclaredAllergens().Should().NotBeEmpty();
    }

    [Fact]
    public void Allergen_CheckAllergens_List_Works()
    {
        var svc = new AllergenService(_context);
        var allergens = new List<Allergen>
        {
            new() { AllergenName = "Limonene", Concentration = 0.02m, Unit = "%", IsDeclared = true },
            new() { AllergenName = "Linalool", Concentration = 0.005m, Unit = "%", IsDeclared = false }
        };
        svc.CheckAllergens(allergens).Should().NotBeNull();
        svc.CheckAllergen(allergens[0]).Should().NotBeNull();
        svc.CalculateTotalAllergenConcentration(allergens).Should().BeGreaterThan(0);
    }

    public void Dispose() => _context.Dispose();
}
