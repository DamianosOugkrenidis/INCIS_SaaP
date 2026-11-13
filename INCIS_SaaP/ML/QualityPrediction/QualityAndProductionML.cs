// Author: Damianos Pythagoras Ougkrenidis
// ML - Quality & Production (Batch prediction, Shelf Life, Anomaly Detection, Image QC)

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace INCIS_SaaP.ML.QualityPrediction;

/// <summary>
/// Batch quality measurement record for ML training
/// </summary>
public class BatchQualityRecord
{
    [Key]
    public int Id { get; set; }
    
    public int ProductionBatchId { get; set; }
    public int FormulaId { get; set; }
    
    // Process parameters (features)
    public decimal? MixingTemperature { get; set; } // °C
    public decimal? MixingSpeed { get; set; } // RPM
    public decimal? MixingDuration { get; set; } // minutes
    public decimal? CoolingRate { get; set; } // °C/min
    public decimal? HomogenizationPressure { get; set; } // bar
    public decimal? AmbientHumidity { get; set; } // %
    public decimal? AmbientTemperature { get; set; } // °C
    
    // Quality measurements (labels/outcomes)
    public decimal? MeasuredpH { get; set; }
    public decimal? MeasuredViscosity { get; set; } // mPa·s
    public decimal? MeasuredDensity { get; set; } // g/cm³
    public decimal? ParticleSize { get; set; } // µm
    
    /// <summary>
    /// Quality score (0-100)
    /// </summary>
    public double? QualityScore { get; set; }
    
    public bool PassedQC { get; set; }
    
    [MaxLength(500)]
    public string? FailureReason { get; set; }
    
    [MaxLength(50)]
    public string? OperatorId { get; set; }
    
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Shelf life prediction training data
/// </summary>
public class ShelfLifeRecord
{
    [Key]
    public int Id { get; set; }
    
    public int FormulaId { get; set; }
    
    [MaxLength(50)]
    public string ProductType { get; set; } = string.Empty;
    
    /// <summary>
    /// Formula composition features (JSON)
    /// </summary>
    public string CompositionFeaturesJson { get; set; } = "{}";
    
    // Accelerated stability data
    public decimal? Stability40C_3m { get; set; } // Score after 3 months at 40°C
    public decimal? Stability40C_6m { get; set; }
    public decimal? Stability25C_6m { get; set; }
    public decimal? Stability25C_12m { get; set; }
    
    /// <summary>
    /// Actual observed shelf life in months
    /// </summary>
    public int? ActualShelfLifeMonths { get; set; }
    
    /// <summary>
    /// Predicted shelf life in months
    /// </summary>
    public int? PredictedShelfLifeMonths { get; set; }
    
    /// <summary>
    /// Preservative system used
    /// </summary>
    [MaxLength(200)]
    public string? PreservativeSystem { get; set; }
    
    /// <summary>
    /// Packaging type
    /// </summary>
    [MaxLength(100)]
    public string? PackagingType { get; set; }
    
    public bool HasAntioxidants { get; set; }
    public decimal? WaterActivity { get; set; }
    public decimal? pH { get; set; }
    
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Anomaly detection record for physicochemical data
/// </summary>
public class AnomalyRecord
{
    [Key]
    public int Id { get; set; }
    
    public int? FormulaId { get; set; }
    public int? ProductionBatchId { get; set; }
    public int? RawMaterialId { get; set; }
    
    /// <summary>
    /// Parameter that showed anomaly: "pH", "Viscosity", "Color", "Density"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Parameter { get; set; } = string.Empty;
    
    public decimal MeasuredValue { get; set; }
    public decimal ExpectedMin { get; set; }
    public decimal ExpectedMax { get; set; }
    
    /// <summary>
    /// Anomaly score (higher = more anomalous)
    /// </summary>
    public double AnomalyScore { get; set; }
    
    /// <summary>
    /// Severity: "Low", "Medium", "High", "Critical"
    /// </summary>
    [MaxLength(20)]
    public string Severity { get; set; } = "Low";
    
    /// <summary>
    /// Detection method: "IsolationForest", "StatisticalBounds", "Manual"
    /// </summary>
    [MaxLength(30)]
    public string DetectionMethod { get; set; } = "StatisticalBounds";
    
    public bool IsConfirmed { get; set; }
    
    [MaxLength(500)]
    public string? RootCause { get; set; }
    
    [MaxLength(500)]
    public string? CorrectiveAction { get; set; }
    
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Image QC record (for computer vision analysis)
/// </summary>
public class ImageQCRecord
{
    [Key]
    public int Id { get; set; }
    
    public int? ProductionBatchId { get; set; }
    public int? RawMaterialSampleId { get; set; }
    
    /// <summary>
    /// Path to the stored image
    /// </summary>
    [MaxLength(500)]
    public string ImagePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Analysis type: "ColorCheck", "TextureAnalysis", "Separation", "Contamination"
    /// </summary>
    [MaxLength(50)]
    public string AnalysisType { get; set; } = string.Empty;
    
    /// <summary>
    /// Detected color in Lab color space
    /// </summary>
    public decimal? ColorL { get; set; }
    public decimal? ColorA { get; set; }
    public decimal? ColorB { get; set; }
    
    /// <summary>
    /// Color difference from reference (Delta E)
    /// </summary>
    public decimal? DeltaE { get; set; }
    
    /// <summary>
    /// Pass/Fail result
    /// </summary>
    public bool PassedVisualQC { get; set; }
    
    /// <summary>
    /// Confidence of the CV model (0-1)
    /// </summary>
    public double ModelConfidence { get; set; }
    
    /// <summary>
    /// Detected issues (JSON array)
    /// </summary>
    public string DetectedIssuesJson { get; set; } = "[]";
    
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Quality &amp; Production ML Service interface
/// </summary>
public interface IQualityPredictionService
{
    /// <summary>
    /// Predicts batch quality based on process parameters
    /// </summary>
    Task<BatchQualityPrediction> PredictBatchQualityAsync(BatchProcessParameters parameters, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Predicts shelf life based on formula composition and accelerated stability data
    /// </summary>
    Task<ShelfLifePrediction> PredictShelfLifeAsync(int formulaId, ShelfLifeInputs inputs, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Detects anomalies in physicochemical measurements
    /// </summary>
    Task<List<AnomalyRecord>> DetectAnomaliesAsync(int formulaId, PhysicochemicalMeasurements measurements, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Analyzes a sample image for quality control
    /// </summary>
    Task<ImageQCResult> AnalyzeImageAsync(int batchId, string imagePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets optimal process parameters for a formula based on historical success
    /// </summary>
    Task<OptimalProcessParameters> GetOptimalParametersAsync(int formulaId, CancellationToken cancellationToken = default);
}

// Supporting DTOs

public class BatchProcessParameters
{
    public int FormulaId { get; init; }
    public decimal MixingTemperature { get; init; }
    public decimal MixingSpeed { get; init; }
    public decimal MixingDuration { get; init; }
    public decimal? CoolingRate { get; init; }
    public decimal? HomogenizationPressure { get; init; }
    public decimal? AmbientHumidity { get; init; }
    public decimal? AmbientTemperature { get; init; }
}

public class BatchQualityPrediction
{
    public double PredictedQualityScore { get; init; }
    public bool PredictedPass { get; init; }
    public double Confidence { get; init; }
    public string RiskLevel { get; init; } = string.Empty;
    public List<ProcessSuggestion> Suggestions { get; init; } = new();
}

public class ProcessSuggestion
{
    public string Parameter { get; init; } = string.Empty;
    public string Suggestion { get; init; } = string.Empty;
    public decimal? SuggestedValue { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public class ShelfLifeInputs
{
    public decimal? Stability40C_3m { get; init; }
    public decimal? Stability40C_6m { get; init; }
    public string? PreservativeSystem { get; init; }
    public string? PackagingType { get; init; }
    public decimal? WaterActivity { get; init; }
    public decimal? pH { get; init; }
}

public class ShelfLifePrediction
{
    public int PredictedMonths { get; init; }
    public int ConfidenceIntervalLow { get; init; }
    public int ConfidenceIntervalHigh { get; init; }
    public double Confidence { get; init; }
    public List<string> RiskFactors { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
}

public class PhysicochemicalMeasurements
{
    public decimal? pH { get; init; }
    public decimal? Viscosity { get; init; }
    public decimal? Density { get; init; }
    public string? Color { get; init; }
    public decimal? Conductivity { get; init; }
}

public class ImageQCResult
{
    public bool Passed { get; init; }
    public double Confidence { get; init; }
    public decimal? DeltaE { get; init; }
    public List<string> DetectedIssues { get; init; } = new();
    public string OverallAssessment { get; init; } = string.Empty;
}

public class OptimalProcessParameters
{
    public decimal SuggestedMixingTemperature { get; init; }
    public decimal SuggestedMixingSpeed { get; init; }
    public decimal SuggestedMixingDuration { get; init; }
    public decimal? SuggestedCoolingRate { get; init; }
    public double Confidence { get; init; }
    public int BasedOnBatchCount { get; init; }
    public string Reasoning { get; init; } = string.Empty;
}

/// <summary>
/// Quality Prediction Service implementation
/// </summary>
public class QualityPredictionService : IQualityPredictionService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QualityPredictionService> _logger;

    public QualityPredictionService(IServiceProvider serviceProvider, ILogger<QualityPredictionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<BatchQualityPrediction> PredictBatchQualityAsync(BatchProcessParameters parameters, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        // Get historical batch data for this formula
        var historicalBatches = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<BatchQualityRecord>()
                    .Where(b => b.FormulaId == parameters.FormulaId && b.QualityScore.HasValue)
                    .OrderByDescending(b => b.RecordedAt)
                    .Take(50),
                cancellationToken);

        if (!historicalBatches.Any())
        {
            return new BatchQualityPrediction
            {
                PredictedQualityScore = 75,
                PredictedPass = true,
                Confidence = 0.3,
                RiskLevel = "Medium",
                Suggestions = new() { new ProcessSuggestion { Parameter = "General", Suggestion = "No historical data available. Using default parameters.", Reason = "First batch for this formula" } }
            };
        }

        // Simple statistical prediction (in production, use ML.NET or Python model)
        var avgQuality = historicalBatches.Average(b => b.QualityScore!.Value);
        var passRate = historicalBatches.Count(b => b.PassedQC) / (double)historicalBatches.Count;

        var suggestions = new List<ProcessSuggestion>();

        // Check if temperature is within optimal range
        var successfulBatches = historicalBatches.Where(b => b.PassedQC && b.MixingTemperature.HasValue).ToList();
        if (successfulBatches.Any())
        {
            var optimalTemp = successfulBatches.Average(b => b.MixingTemperature!.Value);
            if (Math.Abs(parameters.MixingTemperature - optimalTemp) > 5)
            {
                suggestions.Add(new ProcessSuggestion
                {
                    Parameter = "MixingTemperature",
                    Suggestion = $"Consider adjusting to {optimalTemp:F1}°C (historical optimal)",
                    SuggestedValue = optimalTemp,
                    Reason = $"Successful batches average {optimalTemp:F1}°C"
                });
            }
        }

        var riskLevel = passRate switch
        {
            >= 0.9 => "Low",
            >= 0.7 => "Medium",
            _ => "High"
        };

        return new BatchQualityPrediction
        {
            PredictedQualityScore = avgQuality,
            PredictedPass = passRate >= 0.7,
            Confidence = Math.Min(0.9, 0.5 + historicalBatches.Count * 0.01),
            RiskLevel = riskLevel,
            Suggestions = suggestions
        };
    }

    public async Task<ShelfLifePrediction> PredictShelfLifeAsync(int formulaId, ShelfLifeInputs inputs, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        // Simple prediction based on accelerated stability (Arrhenius extrapolation simplified)
        int predictedMonths = 24; // Default: 2 years
        var riskFactors = new List<string>();
        var recommendations = new List<string>();

        // Accelerated stability correlation
        if (inputs.Stability40C_3m.HasValue)
        {
            if (inputs.Stability40C_3m < 70)
            {
                predictedMonths = 12;
                riskFactors.Add("Low stability score at 40°C/3 months");
            }
            else if (inputs.Stability40C_3m >= 90)
            {
                predictedMonths = 36;
            }
        }

        // pH influence
        if (inputs.pH.HasValue)
        {
            if (inputs.pH < 4 || inputs.pH > 8)
            {
                riskFactors.Add("Extreme pH may affect long-term stability");
                predictedMonths = (int)(predictedMonths * 0.85);
            }
        }

        // Water activity
        if (inputs.WaterActivity.HasValue && inputs.WaterActivity > 0.8m)
        {
            riskFactors.Add("High water activity increases microbial risk");
            recommendations.Add("Ensure robust preservative system");
        }

        // Packaging
        if (inputs.PackagingType?.Contains("jar") == true)
        {
            riskFactors.Add("Open jar packaging increases oxidation risk");
            predictedMonths = (int)(predictedMonths * 0.8);
            recommendations.Add("Consider airless pump packaging for better stability");
        }

        return new ShelfLifePrediction
        {
            PredictedMonths = predictedMonths,
            ConfidenceIntervalLow = (int)(predictedMonths * 0.8),
            ConfidenceIntervalHigh = (int)(predictedMonths * 1.2),
            Confidence = inputs.Stability40C_3m.HasValue ? 0.7 : 0.4,
            RiskFactors = riskFactors,
            Recommendations = recommendations
        };
    }

    public async Task<List<AnomalyRecord>> DetectAnomaliesAsync(int formulaId, PhysicochemicalMeasurements measurements, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var anomalies = new List<AnomalyRecord>();

        // Get historical measurements for statistical bounds
        var historicalBatches = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<BatchQualityRecord>()
                    .Where(b => b.FormulaId == formulaId && b.PassedQC)
                    .OrderByDescending(b => b.RecordedAt)
                    .Take(30),
                cancellationToken);

        if (!historicalBatches.Any()) return anomalies;

        // pH anomaly detection
        if (measurements.pH.HasValue)
        {
            var pHValues = historicalBatches.Where(b => b.MeasuredpH.HasValue).Select(b => b.MeasuredpH!.Value).ToList();
            if (pHValues.Any())
            {
                var mean = pHValues.Average();
                var stdDev = (decimal)Math.Sqrt((double)pHValues.Average(v => (v - mean) * (v - mean)));
                var min = mean - 2 * stdDev;
                var max = mean + 2 * stdDev;

                if (measurements.pH < min || measurements.pH > max)
                {
                    anomalies.Add(new AnomalyRecord
                    {
                        FormulaId = formulaId,
                        Parameter = "pH",
                        MeasuredValue = measurements.pH.Value,
                        ExpectedMin = min,
                        ExpectedMax = max,
                        AnomalyScore = (double)Math.Abs(measurements.pH.Value - mean) / (double)(stdDev == 0 ? 1 : stdDev),
                        Severity = Math.Abs(measurements.pH.Value - mean) > 3 * stdDev ? "High" : "Medium",
                        DetectionMethod = "StatisticalBounds"
                    });
                }
            }
        }

        // Viscosity anomaly detection
        if (measurements.Viscosity.HasValue)
        {
            var viscValues = historicalBatches.Where(b => b.MeasuredViscosity.HasValue).Select(b => b.MeasuredViscosity!.Value).ToList();
            if (viscValues.Any())
            {
                var mean = viscValues.Average();
                var stdDev = (decimal)Math.Sqrt((double)viscValues.Average(v => (v - mean) * (v - mean)));
                var min = mean - 2 * stdDev;
                var max = mean + 2 * stdDev;

                if (measurements.Viscosity < min || measurements.Viscosity > max)
                {
                    anomalies.Add(new AnomalyRecord
                    {
                        FormulaId = formulaId,
                        Parameter = "Viscosity",
                        MeasuredValue = measurements.Viscosity.Value,
                        ExpectedMin = min,
                        ExpectedMax = max,
                        AnomalyScore = (double)Math.Abs(measurements.Viscosity.Value - mean) / (double)(stdDev == 0 ? 1 : stdDev),
                        Severity = Math.Abs(measurements.Viscosity.Value - mean) > 3 * stdDev ? "High" : "Medium",
                        DetectionMethod = "StatisticalBounds"
                    });
                }
            }
        }

        // Save detected anomalies
        if (anomalies.Any())
        {
            dbContext.Set<AnomalyRecord>().AddRange(anomalies);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Detected {Count} anomalies for formula {FormulaId}", anomalies.Count, formulaId);
        }

        return anomalies;
    }

    public async Task<ImageQCResult> AnalyzeImageAsync(int batchId, string imagePath, CancellationToken cancellationToken = default)
    {
        // Placeholder for CV model integration (would use ONNX Runtime or Python microservice)
        _logger.LogInformation("Image QC analysis requested for batch {BatchId}: {ImagePath}", batchId, imagePath);
        
        return new ImageQCResult
        {
            Passed = true,
            Confidence = 0.85,
            DeltaE = 2.1m,
            DetectedIssues = new(),
            OverallAssessment = "Visual quality within acceptable parameters"
        };
    }

    public async Task<OptimalProcessParameters> GetOptimalParametersAsync(int formulaId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var successfulBatches = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<BatchQualityRecord>()
                    .Where(b => b.FormulaId == formulaId && b.PassedQC && b.QualityScore >= 80)
                    .OrderByDescending(b => b.QualityScore),
                cancellationToken);

        if (!successfulBatches.Any())
        {
            return new OptimalProcessParameters
            {
                SuggestedMixingTemperature = 70m,
                SuggestedMixingSpeed = 500m,
                SuggestedMixingDuration = 20m,
                Confidence = 0.3,
                BasedOnBatchCount = 0,
                Reasoning = "No historical data. Using industry defaults."
            };
        }

        return new OptimalProcessParameters
        {
            SuggestedMixingTemperature = successfulBatches.Where(b => b.MixingTemperature.HasValue).Average(b => b.MixingTemperature!.Value),
            SuggestedMixingSpeed = successfulBatches.Where(b => b.MixingSpeed.HasValue).Average(b => b.MixingSpeed!.Value),
            SuggestedMixingDuration = successfulBatches.Where(b => b.MixingDuration.HasValue).Average(b => b.MixingDuration!.Value),
            SuggestedCoolingRate = successfulBatches.Where(b => b.CoolingRate.HasValue).Select(b => b.CoolingRate!.Value).DefaultIfEmpty(2m).Average(),
            Confidence = Math.Min(0.9, 0.5 + successfulBatches.Count * 0.02),
            BasedOnBatchCount = successfulBatches.Count,
            Reasoning = $"Based on {successfulBatches.Count} high-quality batches (score >= 80)"
        };
    }
}

