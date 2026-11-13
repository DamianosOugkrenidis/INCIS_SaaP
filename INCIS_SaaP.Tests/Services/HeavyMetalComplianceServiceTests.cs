using FluentAssertions;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// Unit tests for HeavyMetalComplianceService (EU 1223/2009 heavy-metal limits).
/// </summary>
public class HeavyMetalComplianceServiceTests : IDisposable
{
    private readonly Data.INCISDbContext _context;
    private readonly HeavyMetalComplianceService _service;

    public HeavyMetalComplianceServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _service = new HeavyMetalComplianceService(_context);
    }

    // ---------- CheckMetalCompliance (pure logic) ----------

    [Fact]
    public void CheckMetalCompliance_WithinLimit_IsCompliant()
    {
        var metal = new HeavyMetal { MetalName = "Lead", DetectedValue = 1.0m };

        var result = _service.CheckMetalCompliance(metal);

        result.StandardLimit.Should().Be(2.0m);
        result.ApplicableLimit.Should().Be(2.0m);
        result.IsDetected.Should().BeTrue();
        result.IsCompliant.Should().BeTrue();
        result.ComplianceMessage.Should().Be("Εντός ορίων");
        result.Regulation.Should().Be("EU 1223/2009");
    }

    [Fact]
    public void CheckMetalCompliance_ExceedsLimit_IsNonCompliant_WithExceedance()
    {
        var metal = new HeavyMetal { MetalName = "Lead", DetectedValue = 3.0m };

        var result = _service.CheckMetalCompliance(metal);

        result.IsCompliant.Should().BeFalse();
        result.ExceedanceAmount.Should().Be(1.0m);
        result.ExceedancePercentage.Should().Be(50m);
        result.ComplianceMessage.Should().Contain("Υπέρβαση");
    }

    [Fact]
    public void CheckMetalCompliance_NotDetected_IsCompliant()
    {
        var metal = new HeavyMetal { MetalName = "Mercury", DetectedValue = 0m };

        var result = _service.CheckMetalCompliance(metal);

        result.IsDetected.Should().BeFalse();
        result.IsCompliant.Should().BeTrue();
        result.ComplianceMessage.Should().Be("Δεν ανιχνεύτηκε");
    }

    [Fact]
    public void CheckMetalCompliance_NullDetectedValue_IsCompliant()
    {
        var metal = new HeavyMetal { MetalName = "Cadmium", DetectedValue = null };

        var result = _service.CheckMetalCompliance(metal);

        result.IsDetected.Should().BeFalse();
        result.IsCompliant.Should().BeTrue();
    }

    [Fact]
    public void CheckMetalCompliance_IsCaseInsensitive()
    {
        var result = _service.CheckMetalCompliance(new HeavyMetal { MetalName = "MERCURY", DetectedValue = 0.5m });

        result.StandardLimit.Should().Be(1.0m);
        result.Regulation.Should().Be("EU 1223/2009");
    }

    [Fact]
    public void CheckMetalCompliance_UnknownMetal_UsesUserDefinedLimit()
    {
        var metal = new HeavyMetal { MetalName = "Gold", DetectedValue = 5m, LimitValue = 8m };

        var result = _service.CheckMetalCompliance(metal);

        result.Regulation.Should().Be("User Defined");
        result.ApplicableLimit.Should().Be(8m);
        result.IsCompliant.Should().BeTrue();
    }

    [Fact]
    public void CheckMetalCompliance_UnknownMetal_NoLimit_UsesDefaultTen()
    {
        var metal = new HeavyMetal { MetalName = "Gold", DetectedValue = 5m, LimitValue = null };

        var result = _service.CheckMetalCompliance(metal);

        result.ApplicableLimit.Should().Be(10.0m);
        result.IsCompliant.Should().BeTrue();
    }

    [Fact]
    public void CheckMetalCompliance_KnownMetal_UsesLessStrictOfProvidedAndStandard()
    {
        // ApplicableLimit = Math.Max(metal.LimitValue ?? standard, standard)
        var metal = new HeavyMetal { MetalName = "Lead", DetectedValue = 4m, LimitValue = 5m };

        var result = _service.CheckMetalCompliance(metal);

        result.ApplicableLimit.Should().Be(5m);
        result.IsCompliant.Should().BeTrue();
    }

    // ---------- CheckCompliance (aggregation) ----------

    [Fact]
    public void CheckCompliance_List_AggregatesCorrectly()
    {
        var metals = new List<HeavyMetal>
        {
            new() { MetalName = "Lead", DetectedValue = 1.0m },     // compliant, detected
            new() { MetalName = "Arsenic", DetectedValue = 5.0m },  // non-compliant (limit 2)
            new() { MetalName = "Cadmium", DetectedValue = 0m }     // not detected, compliant
        };

        var result = _service.CheckCompliance(metals);

        result.TotalMetalsTested.Should().Be(3);
        result.CompliantMetals.Should().Be(2);
        result.NonCompliantMetals.Should().Be(1);
        result.MetalsDetected.Should().Be(2);
        result.IsOverallCompliant.Should().BeFalse();
        result.CompliancePercentage.Should().BeApproximately(66.67m, 0.01m);
        result.MetalResults.Should().HaveCount(3);
    }

    [Fact]
    public void CheckCompliance_AllCompliant_HundredPercent()
    {
        var metals = new List<HeavyMetal>
        {
            new() { MetalName = "Lead", DetectedValue = 0.5m },
            new() { MetalName = "Mercury", DetectedValue = 0m }
        };

        var result = _service.CheckCompliance(metals);

        result.IsOverallCompliant.Should().BeTrue();
        result.CompliancePercentage.Should().Be(100m);
    }

    [Fact]
    public void CheckCompliance_EmptyList_HundredPercentCompliant()
    {
        var result = _service.CheckCompliance(new List<HeavyMetal>());

        result.TotalMetalsTested.Should().Be(0);
        result.IsOverallCompliant.Should().BeTrue();
        result.CompliancePercentage.Should().Be(100m);
    }

    // ---------- RequiresHeavyMetalTesting ----------

    [Theory]
    [InlineData("cream", true)]
    [InlineData("Face Cream", true)]
    [InlineData("foundation", true)]
    [InlineData("body lotion", true)]
    [InlineData("shampoo", false)]
    [InlineData("", false)]
    public void RequiresHeavyMetalTesting_ReturnsExpected(string productType, bool expected)
    {
        _service.RequiresHeavyMetalTesting(productType).Should().Be(expected);
    }

    [Fact]
    public void RequiresHeavyMetalTesting_Null_ReturnsFalse()
    {
        _service.RequiresHeavyMetalTesting(null!).Should().BeFalse();
    }

    // ---------- CalculateTotalHeavyMetalBurden ----------

    [Fact]
    public void CalculateTotalHeavyMetalBurden_SumsDetectedValues()
    {
        var metals = new List<HeavyMetal>
        {
            new() { DetectedValue = 1.5m },
            new() { DetectedValue = 2.5m },
            new() { DetectedValue = null }
        };

        _service.CalculateTotalHeavyMetalBurden(metals).Should().Be(4.0m);
    }

    // ---------- Static helpers ----------

    [Theory]
    [InlineData("lead", true)]
    [InlineData("LEAD", true)]
    [InlineData("gold", false)]
    public void HasLimit_ReturnsExpected(string metal, bool expected)
    {
        HeavyMetalComplianceService.HasLimit(metal).Should().Be(expected);
    }

    [Fact]
    public void HasLimit_Null_ReturnsFalse()
    {
        HeavyMetalComplianceService.HasLimit(null!).Should().BeFalse();
    }

    [Fact]
    public void GetAllLimits_ContainsKnownMetals()
    {
        var limits = HeavyMetalComplianceService.GetAllLimits();

        limits.Should().ContainKey("lead");
        limits["lead"].Limit.Should().Be(2.0m);
    }

    // ---------- DB-backed ----------

    [Fact]
    public async Task CheckCompliance_ByFormulaId_ReadsOnlyMatchingFormula()
    {
        var f1 = new Formula { FormulaCode = "F-M1", TenantId = null };
        var f2 = new Formula { FormulaCode = "F-M2", TenantId = null };
        _context.Formulas.AddRange(f1, f2);
        await _context.SaveChangesAsync();

        _context.HeavyMetals.AddRange(
            new HeavyMetal { FormulaId = f1.Id, MetalName = "Lead", DetectedValue = 1.0m },
            new HeavyMetal { FormulaId = f1.Id, MetalName = "Arsenic", DetectedValue = 1.0m },
            new HeavyMetal { FormulaId = f2.Id, MetalName = "Mercury", DetectedValue = 0.5m });
        await _context.SaveChangesAsync();

        var result = await _service.CheckCompliance(f1.Id);

        result.TotalMetalsTested.Should().Be(2);
    }

    public void Dispose() => _context.Dispose();
}
