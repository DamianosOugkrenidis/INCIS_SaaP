using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using INCIS_SaaP.Data;
using INCIS_SaaP.ML.FormulaIntelligence;
using INCIS_SaaP.Models;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>Tests for the ML data-loop (RecordOutcome) and prediction wiring.</summary>
public class FormulaIntelligenceServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ServiceProvider _provider;
    private readonly FormulaIntelligenceService _service;

    public FormulaIntelligenceServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "incis-fi-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(_tempDir);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(accessor.Object);
        services.AddSingleton(env.Object);
        var dbName = "fi-" + Guid.NewGuid();
        services.AddDbContext<INCISDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddSingleton<IFormulaSuccessModel, FormulaSuccessModel>();
        _provider = services.BuildServiceProvider();

        _service = new FormulaIntelligenceService(_provider, NullLogger<FormulaIntelligenceService>.Instance);
    }

    private async Task<int> SeedFormulaAsync()
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCISDbContext>();
        var f = new Formula { FormulaCode = "TEST-0001-T01", ProductType = "Leave on" };
        db.Formulas.Add(f);
        await db.SaveChangesAsync();
        return f.Id;
    }

    [Fact]
    public async Task RecordOutcome_CreatesTrainingRecord_AndReturnsCount()
    {
        var id = await SeedFormulaAsync();

        var total = await _service.RecordOutcomeAsync(id, passedOverall: true, failureReason: null);

        total.Should().Be(1);

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCISDbContext>();
        var rec = db.FormulaTrainingRecords.Single();
        rec.FormulaId.Should().Be(id);
        rec.PassedOverall.Should().BeTrue();
        rec.FormulaCode.Should().Be("TEST-0001-T01");
    }

    [Fact]
    public async Task RecordOutcome_Twice_UpsertsSameRecord()
    {
        var id = await SeedFormulaAsync();

        await _service.RecordOutcomeAsync(id, true);
        var total = await _service.RecordOutcomeAsync(id, false, failureReason: "phase separation");

        total.Should().Be(1); // upsert, not duplicate

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCISDbContext>();
        var rec = db.FormulaTrainingRecords.Single();
        rec.PassedOverall.Should().BeFalse();
        rec.FailureReason.Should().Be("phase separation");
    }

    [Fact]
    public async Task RecordOutcome_UnknownFormula_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RecordOutcomeAsync(999999, true));
    }

    [Fact]
    public async Task PredictFormulaSuccess_ReturnsPredictionWithModelMetadata()
    {
        var id = await SeedFormulaAsync();

        var p = await _service.PredictFormulaSuccessAsync(id);

        p.SuccessProbability.Should().BeInRange(0.0, 1.0);
        p.RiskLevel.Should().BeOneOf("Low", "Medium", "High");
        p.ModelSource.Should().NotBe("None"); // real ML model was trained
    }

    public void Dispose()
    {
        _provider.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { /* ignore */ }
    }
}
