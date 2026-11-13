using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Models;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Regression;

/// <summary>
/// Regression tests για τα δεδομένα/λειτουργίες CosIng (Annex II–VI) — επιβεβαιώνουν ότι
/// οι πρόσφατες αλλαγές (INCI catalog) ΔΕΝ επηρέασαν τη λειτουργικότητα CosIng.
/// Ασκούν το ApiCosIngController με seeded CosIng δεδομένα και ελέγχουν πραγματικά αποτελέσματα.
/// </summary>
[Trait("Category", "Regression")]
public class CosIngRegressionTests : IDisposable
{
    private readonly Data.INCISDbContext _context;
    private readonly ApiCosIngController _controller;

    public CosIngRegressionTests()
    {
        _context = TestDbContextFactory.Create();
        _controller = new ApiCosIngController(
            _context,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<ApiCosIngController>.Instance);

        _context.CosIngIngredients.AddRange(
            new CosIngIngredient { InciName = "AQUA", CASNumber = "7732-18-5", ECNumber = "231-791-2", Function = "SOLVENT", DataSource = "github_csv_2016" },
            new CosIngIngredient { InciName = "GLYCERIN", CASNumber = "56-81-5", Function = "HUMECTANT, SOLVENT", DataSource = "github_csv_2016" });
        _context.CosIngEntries.AddRange(
            new CosIngEntry { InciName = "PROHIBITED SUBSTANCE X", Annex = "II", EntryNumber = "1", DataSource = "cosingchecker_api_2026" },
            new CosIngEntry { InciName = "RESTRICTED SUBSTANCE Y", Annex = "III", EntryNumber = "2", MaxConcentration = "1%", DataSource = "cosingchecker_api_2026" });
        _context.SaveChanges();
    }

    private static string Json(object? value) => JsonSerializer.Serialize(value);

    [Fact]
    public async Task SearchIngredients_FindsSeededByName_AndByCas()
    {
        // Σημ.: ο InMemory provider είναι case-sensitive· η case-insensitivity στο SQL
        // καλύπτεται από τα SQL integration tests.
        var byName = Json(((OkObjectResult)await _controller.SearchIngredients("AQUA")).Value);
        byName.Should().Contain("AQUA");

        var byCas = Json(((OkObjectResult)await _controller.SearchIngredients("56-81-5")).Value);
        byCas.Should().Contain("GLYCERIN");
    }

    [Fact]
    public async Task SearchIngredients_ShortQuery_ReturnsEmpty()
    {
        var res = await _controller.SearchIngredients("a");
        var json = Json(((OkObjectResult)res).Value);
        json.Should().Be("[]");
    }

    [Fact]
    public async Task GetByFunction_FiltersByFunction()
    {
        var json = Json(((OkObjectResult)await _controller.GetByFunction("HUMECTANT")).Value);
        json.Should().Contain("GLYCERIN");
        json.Should().NotContain("AQUA");
    }

    [Fact]
    public async Task GetFunctions_ReturnsDistinctFunctions()
    {
        var json = Json(((OkObjectResult)await _controller.GetFunctions()).Value);
        json.Should().Contain("SOLVENT");
        json.Should().Contain("HUMECTANT");
    }

    [Fact]
    public async Task CheckProhibited_DetectsAnnexII()
    {
        var json = Json(((OkObjectResult)await _controller.CheckProhibited("PROHIBITED SUBSTANCE X")).Value);
        json.Should().Contain("II");
    }

    [Fact]
    public async Task CheckRestricted_DetectsAnnexIII()
    {
        var json = Json(((OkObjectResult)await _controller.CheckRestricted("RESTRICTED SUBSTANCE Y")).Value);
        json.Should().Contain("III");
    }

    [Fact]
    public async Task GetStats_ReturnsCounts()
    {
        var res = await _controller.GetStats();
        var ok = res.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    public void Dispose() => _context.Dispose();
}
