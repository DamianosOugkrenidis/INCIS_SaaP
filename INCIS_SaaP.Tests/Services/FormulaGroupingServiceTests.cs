using FluentAssertions;
using INCIS_SaaP.Dtos;
using INCIS_SaaP.Services;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// Unit tests for FormulaGroupingService (grouping formulas by type, project, date).
/// </summary>
public class FormulaGroupingServiceTests
{
    private readonly FormulaGroupingService _service = new();

    private static List<FormulaDto> Sample() => new()
    {
        new FormulaDto { Id = 1, ProductType = "Cream",  CreationDate = new DateTime(2026, 1, 10) },
        new FormulaDto { Id = 2, ProductType = "Cream",  CreationDate = new DateTime(2026, 1, 20) },
        new FormulaDto { Id = 3, ProductType = "Serum",  CreationDate = new DateTime(2026, 2, 5)  },
        new FormulaDto { Id = 4, ProductType = null,     CreationDate = new DateTime(2025, 12, 1) }
    };

    // ---------- GroupByFormulaType ----------

    [Fact]
    public void GroupByFormulaType_GroupsByProductType()
    {
        var groups = _service.GroupByFormulaType(Sample()).ToList();

        groups.Should().HaveCount(3); // Cream, Serum, Unknown
        groups.Single(g => g.Key == "Cream").Count.Should().Be(2);
        groups.Single(g => g.Key == "Serum").Count.Should().Be(1);
    }

    [Fact]
    public void GroupByFormulaType_NullProductType_BecomesUnknown()
    {
        var groups = _service.GroupByFormulaType(Sample()).ToList();

        groups.Should().Contain(g => g.Key == "Unknown");
        groups.Single(g => g.Key == "Unknown").Count.Should().Be(1);
    }

    // ---------- GroupByProjectId ----------

    [Fact]
    public void GroupByProjectId_GroupsByIdPlaceholder()
    {
        var groups = _service.GroupByProjectId(Sample()).ToList();

        groups.Should().HaveCount(4); // distinct ids
        groups.Should().OnlyContain(g => g.Count == 1);
    }

    // ---------- GroupByCreatedDate ----------

    [Fact]
    public void GroupByCreatedDate_ByMonth_GroupsMonths()
    {
        var groups = _service.GroupByCreatedDate(Sample(), DateTimeUnit.Month).ToList();

        // 2026-01, 2026-02, 2025-12
        groups.Should().HaveCount(3);
        groups.Single(g => g.Key == new DateTime(2026, 1, 1)).Count.Should().Be(2);
    }

    [Fact]
    public void GroupByCreatedDate_ByYear_GroupsYears()
    {
        var groups = _service.GroupByCreatedDate(Sample(), DateTimeUnit.Year).ToList();

        groups.Should().HaveCount(2); // 2026, 2025
        groups.Single(g => g.Key == new DateTime(2026, 1, 1)).Count.Should().Be(3);
        groups.Single(g => g.Key == new DateTime(2025, 1, 1)).Count.Should().Be(1);
    }

    [Fact]
    public void GroupByCreatedDate_ByDay_GroupsDistinctDays()
    {
        var groups = _service.GroupByCreatedDate(Sample(), DateTimeUnit.Day).ToList();

        groups.Should().HaveCount(4); // all distinct days
    }

    [Fact]
    public void GroupByCreatedDate_NullCreationDate_UsesMinValue()
    {
        var formulas = new List<FormulaDto>
        {
            new() { Id = 1, CreationDate = null },
            new() { Id = 2, CreationDate = null }
        };

        var groups = _service.GroupByCreatedDate(formulas, DateTimeUnit.Day).ToList();

        groups.Should().HaveCount(1);
        groups[0].Key.Should().Be(DateTime.MinValue.Date);
        groups[0].Count.Should().Be(2);
    }

    // ---------- GroupBy (generic) + guards ----------

    [Fact]
    public void GroupBy_CustomSelector_Works()
    {
        var groups = _service.GroupBy(Sample(), f => f.Id % 2 == 0).ToList();

        groups.Should().HaveCount(2); // odd / even ids
    }

    [Fact]
    public void GroupBy_NullFormulas_Throws()
    {
        Action act = () => _service.GroupBy<int>(null!, f => f.Id).ToList();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GroupBy_NullSelector_Throws()
    {
        Action act = () => _service.GroupBy<int>(Sample(), null!).ToList();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GroupByFormulaType_EmptyInput_ReturnsEmpty()
    {
        _service.GroupByFormulaType(new List<FormulaDto>()).Should().BeEmpty();
    }
}
