// Author: Damianos Pythagoras Ougkrenidis
// Real ML.NET binary-classification model for formula success prediction.
// Pipeline: OneHot(ProductType) + numeric features -> MinMax normalize -> SDCA Logistic Regression.
// Trains on real labeled formulas when available; otherwise augments with a synthetic
// rule-based dataset (clearly flagged) so the ML pipeline is functional from day one.

using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.ML.FormulaIntelligence;

/// <summary>ML.NET input schema (feature vector + label) for formula success.</summary>
public class FormulaMlFeatures
{
    public float RawMaterialCount { get; set; }
    public float WaterPercentage { get; set; }
    public float TotalPercentage { get; set; }
    public float PreservativeCount { get; set; }
    public float HasPreservatives { get; set; }   // 0/1
    public float IsAnhydrous { get; set; }         // 0/1
    public float IsExternalDevelopment { get; set; } // 0/1
    public string ProductType { get; set; } = string.Empty;

    public bool Label { get; set; }
}

/// <summary>ML.NET output schema.</summary>
public class FormulaMlPrediction
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    public float Probability { get; set; }
    public float Score { get; set; }
}

/// <summary>Metadata describing the currently trained model (for transparency in the UI).</summary>
public class FormulaModelMetadata
{
    public bool IsTrained { get; set; }
    public DateTime? TrainedAtUtc { get; set; }
    /// <summary>"Real", "Hybrid" (real + synthetic) or "Synthetic".</summary>
    public string TrainingSource { get; set; } = "None";
    public int SampleCount { get; set; }
    public int RealSampleCount { get; set; }
    public int SyntheticSampleCount { get; set; }
    public double Accuracy { get; set; }
    public double AreaUnderRocCurve { get; set; }
    public double F1Score { get; set; }
    public List<PredictionFactor> GlobalFactors { get; set; } = new();
}

public interface IFormulaSuccessModel
{
    /// <summary>Ensures a model exists (trains lazily on first use). Thread-safe.</summary>
    Task<FormulaModelMetadata> EnsureTrainedAsync(CancellationToken ct = default);

    /// <summary>Forces a retrain (e.g. scheduled Hangfire job after new data).</summary>
    Task<FormulaModelMetadata> RetrainAsync(CancellationToken ct = default);

    /// <summary>Runs inference for a single feature vector.</summary>
    FormulaMlPrediction Predict(FormulaMlFeatures features);

    FormulaModelMetadata Metadata { get; }
}

/// <summary>
/// Singleton ML.NET model service. Holds the trained transformer + a prediction engine
/// (guarded by a lock, since PredictionEngine is not thread-safe) and persists the model to disk.
/// </summary>
public class FormulaSuccessModel : IFormulaSuccessModel
{
    private const int MinRealSamples = 40;      // κάτω από αυτό -> augment με synthetic
    private const int SyntheticSamples = 600;

    private static readonly string[] FeatureColumns =
    {
        "RawMaterialCount", "WaterPercentage", "TotalPercentage", "PreservativeCount",
        "HasPreservatives", "IsAnhydrous", "IsExternalDevelopment", "ProductTypeEncoded"
    };

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FormulaSuccessModel> _logger;
    private readonly MLContext _ml;
    private readonly string _modelPath;
    private readonly SemaphoreSlim _trainLock = new(1, 1);
    private readonly object _engineLock = new();

    private ITransformer? _model;
    private PredictionEngine<FormulaMlFeatures, FormulaMlPrediction>? _engine;
    private FormulaModelMetadata _metadata = new();

    public FormulaSuccessModel(
        IServiceProvider serviceProvider,
        IWebHostEnvironment env,
        ILogger<FormulaSuccessModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _ml = new MLContext(seed: 42);

        var dir = Path.Combine(env.ContentRootPath, "ML", "models");
        Directory.CreateDirectory(dir);
        _modelPath = Path.Combine(dir, "formula_success.zip");
    }

    public FormulaModelMetadata Metadata => _metadata;

    public async Task<FormulaModelMetadata> EnsureTrainedAsync(CancellationToken ct = default)
    {
        if (_model != null) return _metadata;

        await _trainLock.WaitAsync(ct);
        try
        {
            if (_model != null) return _metadata;

            // Try to load a previously persisted model first
            if (File.Exists(_modelPath))
            {
                try
                {
                    using var fs = File.OpenRead(_modelPath);
                    _model = _ml.Model.Load(fs, out _);
                    RebuildEngine();
                    // metadata not persisted with model -> mark loaded
                    if (_metadata.TrainingSource == "None")
                        _metadata = new FormulaModelMetadata { IsTrained = true, TrainingSource = "Persisted", TrainedAtUtc = File.GetLastWriteTimeUtc(_modelPath) };
                    _logger.LogInformation("Loaded persisted formula-success model from {Path}", _modelPath);
                    return _metadata;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load persisted model; will retrain.");
                }
            }

            await TrainInternalAsync(ct);
            return _metadata;
        }
        finally
        {
            _trainLock.Release();
        }
    }

    public async Task<FormulaModelMetadata> RetrainAsync(CancellationToken ct = default)
    {
        await _trainLock.WaitAsync(ct);
        try
        {
            await TrainInternalAsync(ct);
            return _metadata;
        }
        finally
        {
            _trainLock.Release();
        }
    }

    public FormulaMlPrediction Predict(FormulaMlFeatures features)
    {
        if (_engine == null)
            throw new InvalidOperationException("Model not trained. Call EnsureTrainedAsync first.");

        lock (_engineLock)
        {
            return _engine.Predict(features);
        }
    }

    // ============================================================
    // TRAINING
    // ============================================================
    private async Task TrainInternalAsync(CancellationToken ct)
    {
        var (samples, realCount) = await BuildTrainingDataAsync(ct);
        var syntheticCount = samples.Count - realCount;

        var source = realCount == 0 ? "Synthetic"
            : realCount >= MinRealSamples ? "Real"
            : "Hybrid";

        var dataView = _ml.Data.LoadFromEnumerable(samples);

        var pipeline = _ml.Transforms.Categorical
                .OneHotEncoding("ProductTypeEncoded", nameof(FormulaMlFeatures.ProductType))
            .Append(_ml.Transforms.Concatenate("Features", FeatureColumns))
            .Append(_ml.Transforms.NormalizeMinMax("Features"))
            .Append(_ml.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: nameof(FormulaMlFeatures.Label),
                featureColumnName: "Features"));

        // Cross-validation for honest metrics
        double acc = 0, auc = 0, f1 = 0;
        try
        {
            var folds = samples.Count >= 50 ? 5 : 3;
            var cv = _ml.BinaryClassification.CrossValidate(
                dataView, pipeline, numberOfFolds: folds,
                labelColumnName: nameof(FormulaMlFeatures.Label));
            acc = cv.Average(f => f.Metrics.Accuracy);
            auc = cv.Average(f => f.Metrics.AreaUnderRocCurve);
            f1 = cv.Average(f => f.Metrics.F1Score);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cross-validation failed; continuing with full-fit metrics only.");
        }

        // Fit final model on all data
        _model = pipeline.Fit(dataView);
        RebuildEngine();

        // Persist
        try
        {
            using var fs = File.Create(_modelPath);
            _ml.Model.Save(_model, dataView.Schema, fs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist model to {Path}", _modelPath);
        }

        var factors = ComputeGlobalFactors(samples);

        _metadata = new FormulaModelMetadata
        {
            IsTrained = true,
            TrainedAtUtc = DateTime.UtcNow,
            TrainingSource = source,
            SampleCount = samples.Count,
            RealSampleCount = realCount,
            SyntheticSampleCount = syntheticCount,
            Accuracy = acc,
            AreaUnderRocCurve = auc,
            F1Score = f1,
            GlobalFactors = factors
        };

        _logger.LogInformation(
            "Trained formula-success model. Source={Source} Samples={Samples} (real={Real}) AUC={Auc:F3} Acc={Acc:F3}",
            source, samples.Count, realCount, auc, acc);
    }

    private void RebuildEngine()
    {
        lock (_engineLock)
        {
            _engine = _ml.Model.CreatePredictionEngine<FormulaMlFeatures, FormulaMlPrediction>(_model!);
        }
    }

    /// <summary>Real labeled formulas + (optionally) a synthetic augmentation set.</summary>
    private async Task<(List<FormulaMlFeatures> samples, int realCount)> BuildTrainingDataAsync(CancellationToken ct)
    {
        var samples = new List<FormulaMlFeatures>();

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCISDbContext>();

        // Known outcomes: explicit training records + combination logs
        var labelByFormula = new Dictionary<int, bool>();

        try
        {
            foreach (var t in await db.Set<FormulaTrainingRecord>().ToListAsync(ct))
                labelByFormula[t.FormulaId] = t.PassedOverall;

            foreach (var l in await db.Set<FormulaCombinationLog>()
                         .Where(l => l.PassedTests != null).ToListAsync(ct))
                labelByFormula[l.FormulaId] = l.PassedTests!.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read ML training tables; falling back to validated formulas / synthetic.");
        }

        var formulas = await db.Formulas
            .Include(f => f.FormulaRawMaterials)
            .ThenInclude(frm => frm.RawMaterial)
            .ToListAsync(ct);

        foreach (var f in formulas)
        {
            bool label;
            if (labelByFormula.TryGetValue(f.Id, out var known))
                label = known;
            else if (f.IsValidated)
                label = true;                 // validated formula = positive real sample (proxy)
            else
                continue;                     // unknown outcome -> skip (don't invent negatives)

            var feat = ExtractFeatures(f);
            feat.Label = label;
            samples.Add(feat);
        }

        int realCount = samples.Count;

        // Augment with synthetic data when real labeled data is scarce
        if (realCount < MinRealSamples)
        {
            samples.AddRange(GenerateSyntheticSamples(SyntheticSamples));
        }

        // Guarantee both classes present (SDCA needs both labels)
        if (samples.All(s => s.Label) || samples.All(s => !s.Label))
        {
            samples.AddRange(GenerateSyntheticSamples(SyntheticSamples));
        }

        return (samples, realCount);
    }

    /// <summary>Feature extraction shared by training and inference.</summary>
    public static FormulaMlFeatures ExtractFeatures(Formula formula)
    {
        var active = formula.FormulaRawMaterials?.Where(frm => frm.IsActive).ToList()
                     ?? new List<FormulaRawMaterial>();
        var preservatives = active.Count(frm =>
            string.Equals(frm.RawMaterial?.Category, "Preservatives", StringComparison.OrdinalIgnoreCase));
        var water = (float)formula.CalculateRequiredWaterPercentage();

        return new FormulaMlFeatures
        {
            RawMaterialCount = active.Count,
            WaterPercentage = water,
            TotalPercentage = (float)formula.CalculateTotalRawMaterialPercentage(),
            PreservativeCount = preservatives,
            HasPreservatives = preservatives > 0 ? 1f : 0f,
            IsAnhydrous = formula.CanBeAnhydrous() ? 1f : 0f,
            IsExternalDevelopment = formula.IsExternalDevelopment ? 1f : 0f,
            ProductType = string.IsNullOrWhiteSpace(formula.ProductType) ? "Unknown" : formula.ProductType!
        };
    }

    // ============================================================
    // SYNTHETIC DATA (rule-based label + noise) — clearly flagged in metadata
    // ============================================================
    private static List<FormulaMlFeatures> GenerateSyntheticSamples(int count)
    {
        var rnd = new Random(42);
        var list = new List<FormulaMlFeatures>(count);
        string[] types = { "Leave on", "Rinse off" };

        for (int i = 0; i < count; i++)
        {
            var rmCount = rnd.Next(3, 30);
            var water = (float)(rnd.NextDouble() * 90);
            var total = 95f + (float)(rnd.NextDouble() * 10); // ~100 ± noise
            var preservatives = rnd.Next(0, 3);
            var hasPres = preservatives > 0 ? 1f : 0f;
            var anhydrous = water < 5 ? 1f : 0f;
            var external = rnd.NextDouble() < 0.3 ? 1f : 0f;
            var type = types[rnd.Next(types.Length)];

            // Domain logit: preservatives in water-based good; too many RMs bad; extreme water bad
            double logit = 1.2;
            if (water > 10 && hasPres == 0) logit -= 3.0;         // unpreserved aqueous -> likely fail
            if (hasPres == 1) logit += 1.0;
            if (rmCount > 22) logit -= 1.0;
            if (water is > 0 and < 15) logit -= 0.6;
            if (total is < 98 or > 102) logit -= 0.8;
            if (external == 1) logit -= 0.3;
            logit += (rnd.NextDouble() - 0.5) * 1.5;              // noise

            var p = 1.0 / (1.0 + Math.Exp(-logit));
            var label = rnd.NextDouble() < p;

            list.Add(new FormulaMlFeatures
            {
                RawMaterialCount = rmCount,
                WaterPercentage = water,
                TotalPercentage = total,
                PreservativeCount = preservatives,
                HasPreservatives = hasPres,
                IsAnhydrous = anhydrous,
                IsExternalDevelopment = external,
                ProductType = type,
                Label = label
            });
        }
        return list;
    }

    // ============================================================
    // EXPLAINABILITY — sensitivity analysis on the trained model
    // (perturb each feature across ±1σ and measure the change in predicted
    //  probability using the actual prediction engine → real, model-derived importance)
    // ============================================================
    private List<PredictionFactor> ComputeGlobalFactors(List<FormulaMlFeatures> samples)
    {
        var factors = new List<PredictionFactor>();
        if (_engine == null || samples.Count == 0) return factors;

        try
        {
            var subset = samples.Count > 200
                ? samples.OrderBy(_ => Guid.NewGuid()).Take(200).ToList()
                : samples;

            var numericGetters = new (string Name, Func<FormulaMlFeatures, float> Get, Action<FormulaMlFeatures, float> Set)[]
            {
                ("RawMaterialCount", f => f.RawMaterialCount, (f, v) => f.RawMaterialCount = v),
                ("WaterPercentage", f => f.WaterPercentage, (f, v) => f.WaterPercentage = v),
                ("TotalPercentage", f => f.TotalPercentage, (f, v) => f.TotalPercentage = v),
                ("PreservativeCount", f => f.PreservativeCount, (f, v) => f.PreservativeCount = v),
                ("HasPreservatives", f => f.HasPreservatives, (f, v) => f.HasPreservatives = v),
                ("IsAnhydrous", f => f.IsAnhydrous, (f, v) => f.IsAnhydrous = v),
                ("IsExternalDevelopment", f => f.IsExternalDevelopment, (f, v) => f.IsExternalDevelopment = v),
            };

            foreach (var (name, get, set) in numericGetters)
            {
                var values = subset.Select(get).ToList();
                var mean = values.Average();
                var std = (float)Math.Sqrt(values.Select(v => (v - mean) * (v - mean)).Average());
                if (std < 1e-4f) std = Math.Max(1f, Math.Abs(mean) * 0.1f); // avoid zero range

                var low = mean - std;
                var high = mean + std;

                double sumSigned = 0;
                foreach (var s in subset)
                {
                    var probHigh = PredictPerturbed(s, set, high);
                    var probLow = PredictPerturbed(s, set, low);
                    sumSigned += probHigh - probLow;
                }
                var avgSigned = sumSigned / subset.Count;

                factors.Add(new PredictionFactor
                {
                    FactorName = Prettify(name),
                    Importance = Math.Round(Math.Abs(avgSigned), 4),
                    Direction = avgSigned >= 0 ? "positive" : "negative",
                    Description = $"Ευαισθησία μοντέλου: μεταβολή πιθανότητας {avgSigned:+0.000;-0.000} ανά +1σ"
                });
            }

            factors = factors.OrderByDescending(f => f.Importance).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sensitivity analysis failed; global factors unavailable.");
            factors.Clear();
        }
        return factors;
    }

    private double PredictPerturbed(FormulaMlFeatures original, Action<FormulaMlFeatures, float> set, float value)
    {
        var clone = new FormulaMlFeatures
        {
            RawMaterialCount = original.RawMaterialCount,
            WaterPercentage = original.WaterPercentage,
            TotalPercentage = original.TotalPercentage,
            PreservativeCount = original.PreservativeCount,
            HasPreservatives = original.HasPreservatives,
            IsAnhydrous = original.IsAnhydrous,
            IsExternalDevelopment = original.IsExternalDevelopment,
            ProductType = original.ProductType
        };
        set(clone, value);
        lock (_engineLock)
        {
            return _engine!.Predict(clone).Probability;
        }
    }

    private static string Prettify(string raw) => raw switch
    {
        "RawMaterialCount" => "Αριθμός συστατικών",
        "WaterPercentage" => "Ποσοστό νερού",
        "TotalPercentage" => "Συνολικό ποσοστό",
        "PreservativeCount" => "Αριθμός συντηρητικών",
        "HasPreservatives" => "Ύπαρξη συντηρητικού",
        "IsAnhydrous" => "Άνυδρη φόρμουλα",
        "IsExternalDevelopment" => "Εξωτερική ανάπτυξη",
        _ => raw.StartsWith("ProductTypeEncoded") ? "Τύπος προϊόντος" : raw
    };
}
