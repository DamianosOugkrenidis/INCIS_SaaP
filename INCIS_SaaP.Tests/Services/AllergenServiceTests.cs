using FluentAssertions;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// Unit tests for AllergenService (EU 1223/2009 allergen compliance logic).
/// </summary>
public class AllergenServiceTests : IDisposable
{
    private readonly Data.INCISDbContext _context;
    private readonly AllergenService _service;

    public AllergenServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _service = new AllergenService(_context);
    }

    // ---------- CheckAllergen (pure logic) ----------

    [Fact]
    public void CheckAllergen_KnownAllergen_AboveThreshold_NotDeclared_IsNonCompliant()
    {
        var allergen = new Allergen { AllergenName = "Limonene", Concentration = 0.005m, IsDeclared = false };

        var result = _service.CheckAllergen(allergen);

        result.Threshold.Should().Be(0.001m);
        result.IsAboveThreshold.Should().BeTrue();
        result.RequiresDeclaration.Should().BeTrue();
        result.IsCompliant.Should().BeFalse();
        result.ComplianceMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CheckAllergen_KnownAllergen_AboveThreshold_Declared_IsCompliant()
    {
        var allergen = new Allergen { AllergenName = "Limonene", Concentration = 0.005m, IsDeclared = true };

        var result = _service.CheckAllergen(allergen);

        result.RequiresDeclaration.Should().BeTrue();
        result.IsDeclared.Should().BeTrue();
        result.IsCompliant.Should().BeTrue();
    }

    [Fact]
    public void CheckAllergen_BelowThreshold_DoesNotRequireDeclaration_IsCompliant()
    {
        var allergen = new Allergen { AllergenName = "Limonene", Concentration = 0.0005m, IsDeclared = false };

        var result = _service.CheckAllergen(allergen);

        result.IsAboveThreshold.Should().BeFalse();
        result.RequiresDeclaration.Should().BeFalse();
        result.IsCompliant.Should().BeTrue();
    }

    [Fact]
    public void CheckAllergen_ExactlyAtThreshold_RequiresDeclaration_ButNotAboveThreshold()
    {
        var allergen = new Allergen { AllergenName = "Coumarin", Concentration = 0.001m, IsDeclared = false };

        var result = _service.CheckAllergen(allergen);

        // RequiresDeclaration uses >= threshold, IsAboveThreshold uses > threshold
        result.RequiresDeclaration.Should().BeTrue();
        result.IsAboveThreshold.Should().BeFalse();
        result.IsCompliant.Should().BeFalse();
    }

    [Fact]
    public void CheckAllergen_IsCaseInsensitiveForName()
    {
        var lower = _service.CheckAllergen(new Allergen { AllergenName = "citral", Concentration = 0.002m });
        var upper = _service.CheckAllergen(new Allergen { AllergenName = "CITRAL", Concentration = 0.002m });

        lower.Threshold.Should().Be(upper.Threshold);
        lower.RequiresDeclaration.Should().Be(upper.RequiresDeclaration);
    }

    [Fact]
    public void CheckAllergen_UnknownAllergen_UsesDefaultThreshold()
    {
        var allergen = new Allergen { AllergenName = "SomeCustomAllergen", Concentration = 0.005m, IsDeclared = false };

        var result = _service.CheckAllergen(allergen);

        result.Threshold.Should().Be(0.001m);
        result.IsAboveThreshold.Should().BeTrue();
        result.RequiresDeclaration.Should().BeTrue();
    }

    [Fact]
    public void CheckAllergen_NullConcentration_TreatedAsZero_IsCompliant()
    {
        var allergen = new Allergen { AllergenName = "Geraniol", Concentration = null, IsDeclared = false };

        var result = _service.CheckAllergen(allergen);

        result.RequiresDeclaration.Should().BeFalse();
        result.IsCompliant.Should().BeTrue();
    }

    [Fact]
    public void CheckAllergen_NullName_DoesNotThrow_UsesUnknown()
    {
        var allergen = new Allergen { AllergenName = null, Concentration = 0.005m };

        var result = _service.CheckAllergen(allergen);

        result.AllergenName.Should().Be("Unknown");
    }

    // ---------- CheckAllergens (aggregation) ----------

    [Fact]
    public void CheckAllergens_AggregatesCountsCorrectly()
    {
        var allergens = new List<Allergen>
        {
            new() { AllergenName = "Limonene", Concentration = 0.005m, IsDeclared = true },   // compliant, requires declaration
            new() { AllergenName = "Linalool", Concentration = 0.005m, IsDeclared = false },  // non-compliant
            new() { AllergenName = "Citral", Concentration = 0.0001m, IsDeclared = false }    // below threshold, compliant
        };

        var result = _service.CheckAllergens(allergens);

        result.TotalAllergens.Should().Be(3);
        result.AllergensRequiringDeclaration.Should().Be(2);
        result.AllergensAboveThreshold.Should().Be(2);
        result.NonCompliantAllergens.Should().Be(1);
        result.IsCompliant.Should().BeFalse();
        result.AllergenResults.Should().HaveCount(3);
        result.Summary.Should().Contain("Μη συμμορφούμενο");
    }

    [Fact]
    public void CheckAllergens_AllCompliant_ReturnsCompliant()
    {
        var allergens = new List<Allergen>
        {
            new() { AllergenName = "Limonene", Concentration = 0.005m, IsDeclared = true },
            new() { AllergenName = "Citral", Concentration = 0.00001m, IsDeclared = false }
        };

        var result = _service.CheckAllergens(allergens);

        result.NonCompliantAllergens.Should().Be(0);
        result.IsCompliant.Should().BeTrue();
        result.Summary.Should().Contain("Συμμορφούμενο");
    }

    [Fact]
    public void CheckAllergens_EmptyList_IsCompliant()
    {
        var result = _service.CheckAllergens(new List<Allergen>());

        result.TotalAllergens.Should().Be(0);
        result.IsCompliant.Should().BeTrue();
    }

    // ---------- CalculateTotalAllergenConcentration ----------

    [Fact]
    public void CalculateTotalAllergenConcentration_SumsConcentrations()
    {
        var allergens = new List<Allergen>
        {
            new() { Concentration = 0.01m },
            new() { Concentration = 0.02m },
            new() { Concentration = null }
        };

        var total = _service.CalculateTotalAllergenConcentration(allergens);

        total.Should().Be(0.03m);
    }

    [Fact]
    public void CalculateTotalAllergenConcentration_EmptyList_ReturnsZero()
    {
        _service.CalculateTotalAllergenConcentration(new List<Allergen>()).Should().Be(0m);
    }

    // ---------- GetEUDeclaredAllergens (static) ----------

    [Fact]
    public void GetEUDeclaredAllergens_ReturnsKnownAllergens()
    {
        var list = AllergenService.GetEUDeclaredAllergens();

        list.Should().NotBeEmpty();
        list.Should().Contain("limonene");
        list.Should().Contain("coumarin");
    }

    // ---------- DB-backed methods ----------

    private async Task<int> AddFormulaAsync(string code)
    {
        var formula = new Formula { FormulaCode = code, TenantId = null };
        _context.Formulas.Add(formula);
        await _context.SaveChangesAsync();
        return formula.Id;
    }

    [Fact]
    public async Task CheckFormulaAllergens_ReadsOnlyMatchingFormula()
    {
        var f1 = await AddFormulaAsync("F-A1");
        var f2 = await AddFormulaAsync("F-A2");
        _context.Allergens.AddRange(
            new Allergen { FormulaId = f1, AllergenName = "Limonene", Concentration = 0.005m, IsDeclared = false },
            new Allergen { FormulaId = f1, AllergenName = "Citral", Concentration = 0.005m, IsDeclared = true },
            new Allergen { FormulaId = f2, AllergenName = "Linalool", Concentration = 0.005m, IsDeclared = false });
        await _context.SaveChangesAsync();

        var result = await _service.CheckFormulaAllergens(f1);

        result.TotalAllergens.Should().Be(2);
    }

    [Fact]
    public async Task GetUndeclaredAllergens_ReturnsOnlyUndeclared()
    {
        var f = await AddFormulaAsync("F-A3");
        _context.Allergens.AddRange(
            new Allergen { FormulaId = f, AllergenName = "Limonene", IsDeclared = false },
            new Allergen { FormulaId = f, AllergenName = "Citral", IsDeclared = true },
            new Allergen { FormulaId = f, AllergenName = "Geraniol", IsDeclared = null });
        await _context.SaveChangesAsync();

        var result = await _service.GetUndeclaredAllergens(f);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.IsDeclared != true);
    }

    public void Dispose() => _context.Dispose();
}
