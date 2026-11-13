using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using INCIS_SaaP.Data;
using INCIS_SaaP.ML.FormulaIntelligence;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// ML/AI verification (H-2 item 4): φόρτωση αποθηκευμένου μοντέλου, πραγματικές προβλέψεις,
/// fallback όταν λείπει/είναι κατεστραμμένο το artifact, retrain, μετρικές, χειρισμός άκρων.
/// Διαχωρίζει ρητά το πραγματικό ML από heuristics (βλ. docs/ML_AI_INVENTORY.md).
/// </summary>
[Trait("Category", "ML")]
public class MLVerificationTests : IDisposable
{
    private readonly string _tempDir;

    public MLVerificationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "incis-mlverify-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
    }

    private FormulaSuccessModel CreateModel(string? contentRoot = null)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var services = new ServiceCollection();
        services.AddSingleton(accessor.Object);
        services.AddDbContext<INCISDbContext>(o => o.UseInMemoryDatabase("mlverify-" + Guid.NewGuid()));
        var provider = services.BuildServiceProvider();

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(contentRoot ?? _tempDir);
        return new FormulaSuccessModel(provider, env.Object, NullLogger<FormulaSuccessModel>.Instance);
    }

    private static FormulaMlFeatures Sample() => new()
    {
        RawMaterialCount = 12,
        WaterPercentage = 70,
        TotalPercentage = 100,
        PreservativeCount = 2,
        HasPreservatives = 1,
        IsAnhydrous = 0,
        IsExternalDevelopment = 0,
        ProductType = "Leave on"
    };

    private static string? FindRepoModelFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "INCIS_SaaP", "ML", "models", "formula_success.zip");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }

    // ---------------- 1. Φόρτωση αποθηκευμένου μοντέλου + πραγματική πρόβλεψη ----------------
    [Fact]
    public async Task PersistedModel_IsLoaded_AndProducesRealPrediction()
    {
        var repoModel = FindRepoModelFile();
        if (repoModel == null) return; // soft-skip αν λείπει το artifact

        var modelsDir = Path.Combine(_tempDir, "ML", "models");
        Directory.CreateDirectory(modelsDir);
        File.Copy(repoModel, Path.Combine(modelsDir, "formula_success.zip"), overwrite: true);

        var model = CreateModel();
        var meta = await model.EnsureTrainedAsync();

        meta.IsTrained.Should().BeTrue();
        meta.TrainingSource.Should().Be("Persisted", "το αποθηκευμένο μοντέλο πρέπει να φορτώνεται από τον δίσκο");

        var pred = model.Predict(Sample());
        float.IsFinite(pred.Probability).Should().BeTrue();
        pred.Probability.Should().BeInRange(0f, 1f);
        float.IsFinite(pred.Score).Should().BeTrue();
    }

    // ---------------- 2. Fallback όταν λείπει το artifact ----------------
    [Fact]
    public async Task MissingArtifact_TrainsFallback_AndPredicts()
    {
        var model = CreateModel(); // κενός φάκελος -> χωρίς artifact
        var meta = await model.EnsureTrainedAsync();

        meta.IsTrained.Should().BeTrue();
        meta.TrainingSource.Should().Be("Synthetic");
        meta.SampleCount.Should().BeGreaterThan(0);

        var pred = model.Predict(Sample());
        float.IsFinite(pred.Probability).Should().BeTrue();
    }

    // ---------------- 3. Predict πριν την εκπαίδευση -> σαφές σφάλμα ----------------
    [Fact]
    public void Predict_BeforeTraining_Throws()
    {
        var model = CreateModel();

        FluentActions.Invoking(() => model.Predict(Sample()))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*not trained*");
    }

    // ---------------- 4. Κατεστραμμένο artifact -> ανάκαμψη με retrain (χωρίς crash) ----------------
    [Fact]
    public async Task CorruptModelFile_RecoversByRetraining()
    {
        var modelsDir = Path.Combine(_tempDir, "ML", "models");
        Directory.CreateDirectory(modelsDir);
        await File.WriteAllBytesAsync(Path.Combine(modelsDir, "formula_success.zip"), new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        var model = CreateModel();
        var meta = await model.EnsureTrainedAsync();

        meta.IsTrained.Should().BeTrue("μετά από αποτυχία φόρτωσης πρέπει να γίνεται retrain, όχι crash");
        meta.TrainingSource.Should().Be("Synthetic");
        float.IsFinite(model.Predict(Sample()).Probability).Should().BeTrue();
    }

    // ---------------- 5. Retrain -> μετρικές/μέγεθος δείγματος ----------------
    [Fact]
    public async Task Retrain_ProvidesMetricsAndSampleCount()
    {
        var model = CreateModel();
        var meta = await model.RetrainAsync();

        meta.IsTrained.Should().BeTrue();
        meta.SampleCount.Should().BeGreaterThan(0);
        meta.AreaUnderRocCurve.Should().BeInRange(0.0, 1.0);
        meta.Accuracy.Should().BeInRange(0.0, 1.0);
        meta.F1Score.Should().BeGreaterThanOrEqualTo(0.0);
        meta.GlobalFactors.Should().NotBeEmpty();
    }

    // ---------------- 6. Ακραία δεδομένα -> χωρίς crash ----------------
    [Fact]
    public async Task ExtremeFeatures_DoNotCrash()
    {
        var model = CreateModel();
        await model.EnsureTrainedAsync();

        var extreme = new FormulaMlFeatures
        {
            RawMaterialCount = 1_000_000,
            WaterPercentage = -50,
            TotalPercentage = 100_000,
            PreservativeCount = 9_999,
            HasPreservatives = 5,
            IsAnhydrous = 7,
            IsExternalDevelopment = 1,
            ProductType = ""
        };

        var pred = model.Predict(extreme);
        float.IsFinite(pred.Probability).Should().BeTrue();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
