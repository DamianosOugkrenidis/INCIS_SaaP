using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// Branch/edge-case tests για το κρίσιμο business logic τιμολόγησης (PriceCalculationService):
/// εύρεση φόρμουλας, κόστος παραγωγής με ελάχιστο, ανάλυση κόστους, και όλες οι public μέθοδοι.
/// </summary>
public class PriceCalculationServiceBranchTests : IDisposable
{
    private readonly Data.INCISDbContext _context;
    private readonly PriceCalculationService _service;

    public PriceCalculationServiceBranchTests()
    {
        _context = TestDbContextFactory.Create();
        _service = new PriceCalculationService(_context);
    }

    private async Task<Formula> SeedFormulaAsync(decimal price = 10m, decimal? pct = 50m, string? category = "EMOLLIENTS")
    {
        var supplier = new Supplier { Name = "S" };
        _context.Suppliers.Add(supplier);
        var rm = new RawMaterial { Code = "RM-PC", ProductName = "Mat", SupplierId = null, Price = price, Category = category };
        _context.RawMaterials.Add(rm);
        var client = new Client { Name = "C" };
        _context.Clients.Add(client);
        var formula = new Formula { FormulaCode = "PC-0001-T01", ClientId = client.Id };
        _context.Formulas.Add(formula);
        await _context.SaveChangesAsync();

        _context.FormulaRawMaterials.Add(new FormulaRawMaterial
        {
            FormulaId = formula.Id,
            RawMaterialId = rm.Id,
            Percentage = pct,
            Phase = "A",
            IsActive = true
        });
        await _context.SaveChangesAsync();
        return formula;
    }

    [Fact]
    public async Task CalculatePrices_ExistingFormula_ComputesCostsAndAllPricePoints()
    {
        var formula = await SeedFormulaAsync(price: 10m, pct: 50m);

        var result = await _service.CalculatePrices(formula.Id);

        result.ErrorMessage.Should().BeNull();
        result.TotalMaterialCost.Should().Be(5m);                       // 10 * 50%
        result.ProductionCost.Should().Be(0.75m);                       // max(5*0.15, 0.5)
        result.TotalCost.Should().Be(5.75m);
        result.Prices.Should().ContainKeys("Economy", "Standard", "Premium", "Luxury", "Professional", "Default");
        var def = result.Prices["Default"];
        def.MarginPercentage.Should().Be(30m);
        def.RetailPriceWithVAT.Should().BeGreaterThan(def.RetailPrice); // ΦΠΑ προστίθεται
        result.CostBreakdown.Should().ContainSingle();
        result.CostBreakdown[0].Percentage.Should().Be(100m);
    }

    [Fact]
    public async Task CalculatePrices_MissingFormula_ReturnsErrorMessage()
    {
        var result = await _service.CalculatePrices(999999);
        result.ErrorMessage.Should().Be("Formula not found");
        result.Prices.Should().BeEmpty();
    }

    [Fact]
    public void CalculatePricesFromFormulaMaterials_ZeroCost_ProductionMinimumApplied_AndZeroPercentages()
    {
        var list = new List<FormulaRawMaterial>
        {
            new() { RawMaterial = new RawMaterial { Price = null, Category = "X" }, Percentage = 0m }
        };

        var result = _service.CalculatePricesFromFormulaMaterials(list);

        result.TotalMaterialCost.Should().Be(0m);
        result.ProductionCost.Should().Be(0.50m);            // ελάχιστο κόστος παραγωγής
        result.CostBreakdown.Should().ContainSingle();
        result.CostBreakdown[0].Percentage.Should().Be(0m);  // no division by zero
    }

    [Fact]
    public void CalculatePricesFromFormulaMaterials_GroupsByCategory_OrderedDesc()
    {
        var list = new List<FormulaRawMaterial>
        {
            new() { RawMaterial = new RawMaterial { Price = 10m, Category = "A" }, Percentage = 100m }, // 10
            new() { RawMaterial = new RawMaterial { Price = 5m, Category = "B" }, Percentage = 100m },  // 5
            new() { RawMaterial = null, Percentage = 100m }                                             // category "Other", cost 0
        };

        var result = _service.CalculatePricesFromFormulaMaterials(list);

        result.TotalMaterialCost.Should().Be(15m);
        result.CostBreakdown.Select(c => c.Category).First().Should().Be("A");
        result.CostBreakdown.Should().Contain(c => c.Category == "Other");
        result.CostBreakdown.Sum(c => c.Percentage).Should().BeApproximately(100m, 0.01m);
    }

    [Fact]
    public void CalculatePricesFromMaterials_Overload_TreatsAsFullPercentage()
    {
        var result = _service.CalculatePricesFromMaterials(new List<RawMaterial>
        {
            new() { Price = 20m, Category = "A" }
        });

        result.TotalMaterialCost.Should().Be(20m); // 100%
    }

    [Theory]
    [InlineData(100, 0.30, 142.85714285714285714285714286)]
    [InlineData(0, 0.30, 0)]
    public void CalculateRecommendedRetailPrice_Computes(decimal cost, decimal margin, decimal expected)
    {
        _service.CalculateRecommendedRetailPrice(cost, margin).Should().BeApproximately(expected, 0.0001m);
    }

    [Theory]
    [InlineData(80, 100, 20)]   // (100-80)/100*100
    [InlineData(100, 100, 0)]
    [InlineData(50, 0, 0)]      // sellingPrice<=0 -> 0
    public void CalculateProfitMargin_Computes(decimal cost, decimal selling, decimal expected)
    {
        _service.CalculateProfitMargin(cost, selling).Should().Be(expected);
    }

    [Theory]
    [InlineData(100, 4, 25)]
    [InlineData(100, 0, 0)]     // units<=0 -> 0
    public void CalculateCostPerUnit_Computes(decimal total, int units, decimal expected)
    {
        _service.CalculateCostPerUnit(total, units).Should().Be(expected);
    }

    [Fact]
    public void CalculatePriceWithVAT_UnknownCountry_UsesEuRate()
    {
        var value = _service.CalculatePriceWithVAT(100m, "XX");
        value.Should().Be(121m); // EU 21%
    }

    public void Dispose() => _context.Dispose();
}
