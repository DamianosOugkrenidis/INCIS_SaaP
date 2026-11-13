using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using INCIS_SaaP.Data;
using INCIS_SaaP.ML.FormulaIntelligence;
using INCIS_SaaP.Models;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>Tests for the real ML.NET formula-success model.</summary>
public class FormulaSuccessModelTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ServiceProvider _provider;
    private readonly FormulaSuccessModel _model;

    public FormulaSuccessModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "incis-ml-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var services = new ServiceCollection();
        services.AddSingleton(accessor.Object);
        var dbName = "mltest-" + Guid.NewGuid();
        services.AddDbContext<INCISDbContext>(o => o.UseInMemoryDatabase(dbName));
        _provider = services.BuildServiceProvider();

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(_tempDir);

        _model = new FormulaSuccessModel(_provider, env.Object, NullLogger<FormulaSuccessModel>.Instance);
    }

    [Fact]
    public async Task EnsureTrained_EmptyDatabase_TrainsOnSyntheticData()
    {
        var meta = await _model.EnsureTrainedAsync();

        meta.IsTrained.Should().BeTrue();
        meta.TrainingSource.Should().Be("Synthetic");
        meta.RealSampleCount.Should().Be(0);
        meta.SampleCount.Should().BeGreaterThan(100);
        meta.AreaUnderRocCurve.Should().BeInRange(0.0, 1.0);
        meta.GlobalFactors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Predict_ReturnsProbabilityInRange()
    {
        await _model.EnsureTrainedAsync();

        var pred = _model.Predict(new FormulaMlFeatures
        {
            RawMaterialCount = 10,
            WaterPercentage = 55,
            TotalPercentage = 100,
            PreservativeCount = 1,
            HasPreservatives = 1,
            IsAnhydrous = 0,
            IsExternalDevelopment = 0,
            ProductType = "Leave on"
        });

        pred.Probability.Should().BeInRange(0f, 1f);
    }

    [Fact]
    public async Task Predict_LearnsPreservativePattern_UnpreservedAqueousScoresLower()
    {
        await _model.EnsureTrainedAsync();

        FormulaMlFeatures Base() => new()
        {
            RawMaterialCount = 12,
            WaterPercentage = 60,
            TotalPercentage = 100,
            PreservativeCount = 1,
            HasPreservatives = 1,
            IsAnhydrous = 0,
            IsExternalDevelopment = 0,
            ProductType = "Leave on"
        };

        var good = _model.Predict(Base());

        var badFeat = Base();
        badFeat.HasPreservatives = 0;
        badFeat.PreservativeCount = 0;
        var bad = _model.Predict(badFeat);

        // The synthetic training signal makes unpreserved aqueous formulas riskier.
        bad.Probability.Should().BeLessThan(good.Probability);
    }

    [Fact]
    public async Task Retrain_WithValidatedFormulas_CountsRealSamples()
    {
        using (var scope = _provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<INCISDbContext>();
            for (int i = 0; i < 5; i++)
                db.Formulas.Add(new Formula
                {
                    FormulaCode = $"T-{i:0000}-T01",
                    ProductType = "Leave on",
                    ValidationDate = DateTime.UtcNow
                });
            await db.SaveChangesAsync();
        }

        var meta = await _model.RetrainAsync();

        meta.RealSampleCount.Should().Be(5);
        meta.TrainingSource.Should().Be("Hybrid"); // few real -> augmented
        meta.IsTrained.Should().BeTrue();
    }

    [Fact]
    public void ExtractFeatures_MapsFormulaCorrectly()
    {
        var formula = new Formula
        {
            ProductType = "Rinse off",
            IsExternalDevelopment = true,
            FormulaRawMaterials = new List<FormulaRawMaterial>
            {
                new() { IsActive = true, Percentage = 5, RawMaterial = new RawMaterial { ProductName = "P", Category = "Preservatives" } },
                new() { IsActive = true, Percentage = 3, RawMaterial = new RawMaterial { ProductName = "E", Category = "Emollients" } },
                new() { IsActive = false, Percentage = 9, RawMaterial = new RawMaterial { ProductName = "X", Category = "Other" } }
            }
        };

        var f = FormulaSuccessModel.ExtractFeatures(formula);

        f.RawMaterialCount.Should().Be(2); // only active
        f.PreservativeCount.Should().Be(1);
        f.HasPreservatives.Should().Be(1f);
        f.IsExternalDevelopment.Should().Be(1f);
        f.ProductType.Should().Be("Rinse off");
    }

    public void Dispose()
    {
        _provider.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { /* ignore */ }
    }
}
