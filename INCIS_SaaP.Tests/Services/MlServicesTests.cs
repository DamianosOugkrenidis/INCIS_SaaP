using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using INCIS_SaaP.Data;
using INCIS_SaaP.ML.RegulatoryIntelligence;
using INCIS_SaaP.ML.SemanticSearch;
using INCIS_SaaP.Models;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>Unit tests for the regulatory-intelligence and semantic-search services.</summary>
public class MlServicesTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public MlServicesTests()
    {
        _provider = TestServiceProvider.Create(out _);
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCISDbContext>();
        db.INCIs.AddRange(
            new INCI { Name = "Aqua", CASNumber = "7732-18-5", Synonyms = "Water" },
            new INCI { Name = "Glycerin", CASNumber = "56-81-5" });
        var f = new Formula { FormulaCode = "ML-0001-T01", ProductType = "Leave on" };
        db.Formulas.Add(f);
        db.SaveChanges();
    }

    [Fact]
    public async Task Regulatory_ResolveInciName_ReturnsMatches()
    {
        var svc = new RegulatoryIntelligenceService(_provider, NullLogger<RegulatoryIntelligenceService>.Instance);
        var matches = await svc.ResolveINCINameAsync("aqua");
        matches.Should().NotBeNull();
    }

    [Fact]
    public async Task Regulatory_PendingChanges_And_Scan_Run()
    {
        var svc = new RegulatoryIntelligenceService(_provider, NullLogger<RegulatoryIntelligenceService>.Instance);
        (await svc.GetPendingChangesAsync()).Should().NotBeNull();
        (await svc.ScanForChangesAsync()).Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task SemanticSearch_RebuildAndSearch_Run()
    {
        var svc = new SemanticSearchService(_provider, NullLogger<SemanticSearchService>.Instance);
        await svc.RebuildIndexAsync();
        var results = await svc.SearchAsync("water", maxResults: 5);
        results.Should().NotBeNull();
    }

    public void Dispose() => _provider.Dispose();
}
