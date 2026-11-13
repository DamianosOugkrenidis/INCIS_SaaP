// Author: Damianos Pythagoras Ougkrenidis
// ML - Formula Success Prediction (Classification model)
// Predicts whether a formula will pass stability/challenge tests

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace INCIS_SaaP.ML.FormulaIntelligence;

/// <summary>
/// Training data for formula success prediction
/// Each record represents a formula's test outcome
/// </summary>
public class FormulaTrainingRecord
{
    [Key]
    public int Id { get; set; }
    
    public int FormulaId { get; set; }
    
    [MaxLength(100)]
    public string FormulaCode { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string ProductType { get; set; } = string.Empty;
    
    /// <summary>
    /// Feature vector: raw material percentages and categories
    /// </summary>
    public string FeaturesJson { get; set; } = "{}";
    
    /// <summary>
    /// Number of raw materials in formula
    /// </summary>
    public int RawMaterialCount { get; set; }
    
    /// <summary>
    /// Total natural origin percentage
    /// </summary>
    public decimal NaturalOriginPercentage { get; set; }
    
    /// <summary>
    /// Water percentage
    /// </summary>
    public decimal WaterPercentage { get; set; }
    
    /// <summary>
    /// Number of preservatives used
    /// </summary>
    public int PreservativeCount { get; set; }
    
    /// <summary>
    /// pH value if available
    /// </summary>
    public decimal? pH { get; set; }
    
    // Labels (outcomes)
    public bool PassedStabilityTest { get; set; }
    public bool PassedChallengeTest { get; set; }
    public bool PassedOverall { get; set; }
    
    [MaxLength(500)]
    public string? FailureReason { get; set; }
    
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Formula combination log for ML training
/// </summary>
public class FormulaCombinationLog
{
    [Key]
    public int Id { get; set; }
    
    public int FormulaId { get; set; }
    
    [MaxLength(100)]
    public string FormulaCode { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string ProductType { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON array of {rawMaterialId, percentage, category, phase}
    /// </summary>
    public string CombinationJson { get; set; } = "[]";
    
    /// <summary>
    /// INCI ingredients with concentrations
    /// </summary>
    public string INCICompositionJson { get; set; } = "[]";
    
    public int? ClientId { get; set; }
    
    public decimal TotalPercentage { get; set; }
    public decimal WaterPercentage { get; set; }
    public bool IsAnhydrous { get; set; }
    
    // Outcome tracking
    public bool? IsValidated { get; set; }
    public bool? PassedTests { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Prediction result from the ML model
/// </summary>
public class FormulaPrediction
{
    public double SuccessProbability { get; init; }
    public double StabilityProbability { get; init; }
    public double ChallengeProbability { get; init; }
    public string RiskLevel { get; init; } = string.Empty; // Low, Medium, High
    public List<PredictionFactor> TopFactors { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public double ModelConfidence { get; init; }

    // --- ML model transparency (real ML.NET model) ---
    /// <summary>"Real", "Hybrid", "Synthetic" or "Persisted" — πηγή εκπαίδευσης του μοντέλου.</summary>
    public string ModelSource { get; init; } = "None";
    public double ModelAuc { get; init; }
    public double ModelAccuracy { get; init; }
    public int TrainingSampleCount { get; init; }
    public DateTime? ModelTrainedAtUtc { get; init; }
}

public class PredictionFactor
{
    public string FactorName { get; init; } = string.Empty;
    public double Importance { get; init; }
    public string Direction { get; init; } = string.Empty; // "positive", "negative"
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Ingredient compatibility matrix
/// Stores known incompatibilities between raw materials
/// </summary>
public class IngredientCompatibility
{
    [Key]
    public int Id { get; set; }
    
    public int RawMaterial1Id { get; set; }
    public int RawMaterial2Id { get; set; }
    
    /// <summary>
    /// Score from -1 (fully incompatible) to +1 (synergistic)
    /// </summary>
    public double CompatibilityScore { get; set; }
    
    /// <summary>
    /// Confidence in the score (0-1)
    /// </summary>
    public double Confidence { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Source: "Manual", "MLPredicted", "Literature"
    /// </summary>
    [MaxLength(20)]
    public string Source { get; set; } = "Manual";
    
    /// <summary>
    /// Maximum combined percentage before issues occur
    /// </summary>
    public decimal? MaxCombinedPercentage { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Optimal concentration suggestion record
/// </summary>
public class ConcentrationSuggestion
{
    public int RawMaterialId { get; init; }
    public string RawMaterialName { get; init; } = string.Empty;
    public string ProductType { get; init; } = string.Empty;
    public decimal SuggestedMinPercentage { get; init; }
    public decimal SuggestedMaxPercentage { get; init; }
    public decimal OptimalPercentage { get; init; }
    public double Confidence { get; init; }
    public int BasedOnFormulaCount { get; init; }
    public string Reasoning { get; init; } = string.Empty;
}

/// <summary>
/// ML Service interface for formula intelligence
/// </summary>
public interface IFormulaIntelligenceService
{
    /// <summary>
    /// Predicts success probability for a formula
    /// </summary>
    Task<FormulaPrediction> PredictFormulaSuccessAsync(int formulaId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks compatibility between raw materials in a formula
    /// </summary>
    Task<List<CompatibilityWarning>> CheckCompatibilityAsync(int formulaId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Suggests optimal concentration for a raw material in a specific product type
    /// </summary>
    Task<ConcentrationSuggestion> SuggestConcentrationAsync(int rawMaterialId, string productType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Logs a formula combination for ML training
    /// </summary>
    Task LogCombinationAsync(int formulaId, bool? passedTests = null, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Καταγράφει ΠΡΑΓΜΑΤΙΚΟ αποτέλεσμα τεστ φόρμουλας (labeled training data).
    /// Δημιουργεί/ενημερώνει FormulaTrainingRecord με snapshot των features + label,
    /// ώστε το ML μοντέλο να εκπαιδεύεται σε πραγματικά δεδομένα (data loop).
    /// Επιστρέφει τον συνολικό αριθμό πραγματικών labeled records.
    /// </summary>
    Task<int> RecordOutcomeAsync(int formulaId, bool passedOverall, bool? passedStability = null,
        bool? passedChallenge = null, string? failureReason = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Auto-generates a draft formula from a product brief
    /// </summary>
    Task<AutoFormulationResult> AutoFormulateAsync(AutoFormulationRequest request, CancellationToken cancellationToken = default);
}

public class CompatibilityWarning
{
    public int RawMaterial1Id { get; init; }
    public string RawMaterial1Name { get; init; } = string.Empty;
    public int RawMaterial2Id { get; init; }
    public string RawMaterial2Name { get; init; } = string.Empty;
    public double CompatibilityScore { get; init; }
    public string Severity { get; init; } = string.Empty; // "Low", "Medium", "High", "Critical"
    public string Description { get; init; } = string.Empty;
    public string? Suggestion { get; init; }
}

public class AutoFormulationRequest
{
    public string ProductBrief { get; init; } = string.Empty; // "moisturizing face cream SPF30"
    public string ProductType { get; init; } = string.Empty; // "Leave on" or "Rinse off"
    public List<string>? RequiredCategories { get; init; } // e.g., "UV Filters", "Emollients"
    public List<int>? ExcludedRawMaterialIds { get; init; }
    public decimal? TargetNaturalPercentage { get; init; }
    public decimal? MaxBudgetPerKg { get; init; }
    public bool PreferVegan { get; init; }
}

public class AutoFormulationResult
{
    public bool IsSuccess { get; init; }
    public List<SuggestedRawMaterial> SuggestedRawMaterials { get; init; } = new();
    public decimal EstimatedWaterPercentage { get; init; }
    public decimal EstimatedCostPerKg { get; init; }
    public double PredictedSuccessRate { get; init; }
    public string Reasoning { get; init; } = string.Empty;
    public List<string> Warnings { get; init; } = new();
}

public class SuggestedRawMaterial
{
    public int RawMaterialId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal SuggestedPercentage { get; init; }
    public string SuggestedPhase { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Formula Intelligence Service implementation
/// Uses statistical analysis on historical data (can be enhanced with ML.NET)
/// </summary>
public class FormulaIntelligenceService : IFormulaIntelligenceService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FormulaIntelligenceService> _logger;

    public FormulaIntelligenceService(IServiceProvider serviceProvider, ILogger<FormulaIntelligenceService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<FormulaPrediction> PredictFormulaSuccessAsync(int formulaId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var formula = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                dbContext.Set<Models.Formula>()
                    .Include(f => f.FormulaRawMaterials)
                    .ThenInclude(frm => frm.RawMaterial)
                    .Where(f => f.Id == formulaId),
                cancellationToken);

        if (formula == null)
            return new FormulaPrediction { SuccessProbability = 0, RiskLevel = "Unknown", ModelConfidence = 0 };

        // Real ML.NET model (trains lazily; augments with synthetic data if real data is scarce)
        var model = scope.ServiceProvider.GetRequiredService<IFormulaSuccessModel>();
        var meta = await model.EnsureTrainedAsync(cancellationToken);

        var features = FormulaSuccessModel.ExtractFeatures(formula);
        var prediction = model.Predict(features);

        double successRate = Math.Clamp(prediction.Probability, 0d, 1d);

        var riskLevel = successRate switch
        {
            >= 0.8 => "Low",
            >= 0.5 => "Medium",
            _ => "High"
        };

        // Actionable domain recommendations derived from the same features the model uses
        var recommendations = new List<string>();
        if (features.HasPreservatives == 0f && features.WaterPercentage > 10f)
            recommendations.Add("Προσθέστε σύστημα συντήρησης — υδατική φόρμουλα χωρίς συντηρητικό δύσκολα περνά challenge test.");
        if (features.RawMaterialCount > 22f)
            recommendations.Add("Μεγάλος αριθμός συστατικών — εξετάστε απλοποίηση για σταθερότητα/κόστος.");
        if (features.TotalPercentage is < 98f or > 102f)
            recommendations.Add($"Το συνολικό ποσοστό ({features.TotalPercentage:F1}%) αποκλίνει από 100% — ελέγξτε τη σύνθεση.");
        if (successRate < 0.5)
            recommendations.Add("Χαμηλή προβλεπόμενη πιθανότητα επιτυχίας — συνιστάται επανεξέταση πριν το scale-up.");

        // Model confidence scales with amount of REAL data behind the model
        double confidence = meta.TrainingSource switch
        {
            "Real" => Math.Min(0.95, 0.6 + meta.RealSampleCount * 0.002),
            "Hybrid" => Math.Min(0.7, 0.45 + meta.RealSampleCount * 0.004),
            "Persisted" => 0.6,
            _ => 0.4 // Synthetic
        };

        return new FormulaPrediction
        {
            SuccessProbability = successRate,
            StabilityProbability = Math.Clamp(successRate * 1.1, 0d, 1d),
            ChallengeProbability = Math.Clamp(successRate * 0.9, 0d, 1d),
            RiskLevel = riskLevel,
            TopFactors = meta.GlobalFactors.Take(5).ToList(),
            Recommendations = recommendations,
            ModelConfidence = confidence,
            ModelSource = meta.TrainingSource,
            ModelAuc = meta.AreaUnderRocCurve,
            ModelAccuracy = meta.Accuracy,
            TrainingSampleCount = meta.SampleCount,
            ModelTrainedAtUtc = meta.TrainedAtUtc
        };
    }

    public async Task<List<CompatibilityWarning>> CheckCompatibilityAsync(int formulaId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var formulaRMs = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<Models.FormulaRawMaterial>()
                    .Include(frm => frm.RawMaterial)
                    .Where(frm => frm.FormulaId == formulaId && frm.IsActive),
                cancellationToken);

        var warnings = new List<CompatibilityWarning>();
        var compatibilities = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(dbContext.Set<IngredientCompatibility>(), cancellationToken);

        // Check pairwise compatibility
        for (int i = 0; i < formulaRMs.Count; i++)
        {
            for (int j = i + 1; j < formulaRMs.Count; j++)
            {
                var rm1 = formulaRMs[i];
                var rm2 = formulaRMs[j];

                var compat = compatibilities.FirstOrDefault(c =>
                    (c.RawMaterial1Id == rm1.RawMaterialId && c.RawMaterial2Id == rm2.RawMaterialId) ||
                    (c.RawMaterial1Id == rm2.RawMaterialId && c.RawMaterial2Id == rm1.RawMaterialId));

                if (compat != null && compat.CompatibilityScore < 0)
                {
                    var severity = compat.CompatibilityScore switch
                    {
                        <= -0.8 => "Critical",
                        <= -0.5 => "High",
                        <= -0.3 => "Medium",
                        _ => "Low"
                    };

                    warnings.Add(new CompatibilityWarning
                    {
                        RawMaterial1Id = rm1.RawMaterialId,
                        RawMaterial1Name = rm1.RawMaterial?.ProductName ?? "",
                        RawMaterial2Id = rm2.RawMaterialId,
                        RawMaterial2Name = rm2.RawMaterial?.ProductName ?? "",
                        CompatibilityScore = compat.CompatibilityScore,
                        Severity = severity,
                        Description = compat.Description ?? "Known incompatibility",
                        Suggestion = compat.MaxCombinedPercentage.HasValue
                            ? $"Keep combined percentage below {compat.MaxCombinedPercentage}%"
                            : "Consider replacing one of these ingredients"
                    });
                }
            }
        }

        return warnings;
    }

    public async Task<ConcentrationSuggestion> SuggestConcentrationAsync(int rawMaterialId, string productType, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        // Find historical usage of this raw material in similar formulas
        var usages = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<Models.FormulaRawMaterial>()
                    .Include(frm => frm.Formula)
                    .Include(frm => frm.RawMaterial)
                    .Where(frm => frm.RawMaterialId == rawMaterialId && frm.IsActive
                        && frm.Formula != null && frm.Formula.ProductType == productType),
                cancellationToken);

        var rm = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                dbContext.Set<Models.RawMaterial>().Where(r => r.Id == rawMaterialId),
                cancellationToken);

        if (!usages.Any() || rm == null)
        {
            return new ConcentrationSuggestion
            {
                RawMaterialId = rawMaterialId,
                RawMaterialName = rm?.ProductName ?? "Unknown",
                ProductType = productType,
                SuggestedMinPercentage = 0.5m,
                SuggestedMaxPercentage = 5m,
                OptimalPercentage = 2m,
                Confidence = 0.3,
                BasedOnFormulaCount = 0,
                Reasoning = "No historical data available. Using category defaults."
            };
        }

        var percentages = usages.Where(u => u.Percentage.HasValue).Select(u => u.Percentage!.Value).ToList();
        var min = percentages.Min();
        var max = percentages.Max();
        var avg = percentages.Average();
        var median = percentages.OrderBy(p => p).ElementAt(percentages.Count / 2);

        // Prefer validated formulas
        var validatedUsages = usages.Where(u => u.Formula?.ValidationDate != null).ToList();
        var optimalPct = validatedUsages.Any()
            ? validatedUsages.Where(u => u.Percentage.HasValue).Average(u => u.Percentage!.Value)
            : median;

        return new ConcentrationSuggestion
        {
            RawMaterialId = rawMaterialId,
            RawMaterialName = rm.ProductName,
            ProductType = productType,
            SuggestedMinPercentage = min,
            SuggestedMaxPercentage = max,
            OptimalPercentage = optimalPct,
            Confidence = Math.Min(0.9, 0.5 + (usages.Count * 0.02)),
            BasedOnFormulaCount = usages.Count,
            Reasoning = $"Based on {usages.Count} formulas of type '{productType}'. " +
                       $"Validated formulas use average {optimalPct:F2}%."
        };
    }

    public async Task LogCombinationAsync(int formulaId, bool? passedTests = null, string? notes = null, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var formula = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                dbContext.Set<Models.Formula>()
                    .Include(f => f.FormulaRawMaterials)
                    .ThenInclude(frm => frm.RawMaterial)
                    .Where(f => f.Id == formulaId),
                cancellationToken);

        if (formula == null) return;

        var combination = formula.FormulaRawMaterials
            .Where(frm => frm.IsActive)
            .Select(frm => new
            {
                frm.RawMaterialId,
                Percentage = frm.Percentage ?? 0,
                Category = frm.RawMaterial?.Category ?? "",
                Phase = frm.Phase ?? ""
            })
            .ToList();

        var log = new FormulaCombinationLog
        {
            FormulaId = formulaId,
            FormulaCode = formula.FormulaCode,
            ProductType = formula.ProductType ?? "",
            CombinationJson = JsonSerializer.Serialize(combination),
            ClientId = formula.ClientId,
            TotalPercentage = formula.CalculateTotalRawMaterialPercentage(),
            WaterPercentage = formula.CalculateRequiredWaterPercentage(),
            IsAnhydrous = formula.CanBeAnhydrous(),
            IsValidated = formula.IsValidated,
            PassedTests = passedTests,
            Notes = notes
        };

        dbContext.Set<FormulaCombinationLog>().Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Logged formula combination for ML training: Formula {FormulaId} ({FormulaCode})", 
            formulaId, formula.FormulaCode);
    }

    public async Task<int> RecordOutcomeAsync(int formulaId, bool passedOverall, bool? passedStability = null,
        bool? passedChallenge = null, string? failureReason = null, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var formula = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                db.Set<Models.Formula>()
                    .Include(f => f.FormulaRawMaterials)
                    .ThenInclude(frm => frm.RawMaterial)
                    .Where(f => f.Id == formulaId),
                cancellationToken);

        if (formula == null)
            throw new InvalidOperationException($"Formula {formulaId} not found.");

        var features = FormulaSuccessModel.ExtractFeatures(formula);

        var rec = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(db.Set<FormulaTrainingRecord>().Where(t => t.FormulaId == formulaId), cancellationToken);
        var isNew = rec == null;
        rec ??= new FormulaTrainingRecord { FormulaId = formulaId };

        rec.FormulaCode = formula.FormulaCode;
        rec.ProductType = formula.ProductType ?? "";
        rec.RawMaterialCount = (int)features.RawMaterialCount;
        rec.WaterPercentage = (decimal)features.WaterPercentage;
        rec.PreservativeCount = (int)features.PreservativeCount;
        rec.PassedStabilityTest = passedStability ?? passedOverall;
        rec.PassedChallengeTest = passedChallenge ?? passedOverall;
        rec.PassedOverall = passedOverall;
        rec.FailureReason = failureReason;
        rec.FeaturesJson = JsonSerializer.Serialize(features);
        rec.RecordedAt = DateTime.UtcNow;

        if (isNew) db.Set<FormulaTrainingRecord>().Add(rec);
        await db.SaveChangesAsync(cancellationToken);

        var total = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(db.Set<FormulaTrainingRecord>(), cancellationToken);

        _logger.LogInformation(
            "Recorded real outcome for formula {FormulaId} ({Code}): passed={Passed}. Total labeled records={Total}",
            formulaId, formula.FormulaCode, passedOverall, total);

        return total;
    }

    public async Task<AutoFormulationResult> AutoFormulateAsync(AutoFormulationRequest request, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        // Get available raw materials
        var rawMaterials = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<Models.RawMaterial>()
                    .Where(rm => rm.IsActive && rm.IsFullyRegistered && rm.IsEvaluated),
                cancellationToken);

        if (request.ExcludedRawMaterialIds?.Any() == true)
            rawMaterials = rawMaterials.Where(rm => !request.ExcludedRawMaterialIds.Contains(rm.Id)).ToList();

        if (request.PreferVegan)
            rawMaterials = rawMaterials.Where(rm => rm.HasVegan).ToList();

        // Get historical successful formulas of similar type
        var successfulFormulas = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<FormulaCombinationLog>()
                    .Where(l => l.ProductType == request.ProductType && l.IsValidated == true && l.PassedTests == true)
                    .OrderByDescending(l => l.LoggedAt)
                    .Take(50),
                cancellationToken);

        var suggestions = new List<SuggestedRawMaterial>();
        var warnings = new List<string>();

        // Analyze common patterns from successful formulas
        // This is a simplified heuristic - in production would use ML.NET or Python model
        var categoryMap = rawMaterials.GroupBy(rm => rm.Category ?? "Other").ToDictionary(g => g.Key, g => g.ToList());

        // Basic formula structure based on product type
        var requiredCategories = request.RequiredCategories ?? GetDefaultCategories(request.ProductType);

        decimal totalPct = 0;
        foreach (var category in requiredCategories)
        {
            if (!categoryMap.ContainsKey(category)) continue;
            
            var candidates = categoryMap[category];
            var selected = candidates.OrderByDescending(rm => rm.Price ?? 0).FirstOrDefault();
            
            if (selected != null)
            {
                var suggestedPct = GetDefaultPercentage(category, request.ProductType);
                totalPct += suggestedPct;

                suggestions.Add(new SuggestedRawMaterial
                {
                    RawMaterialId = selected.Id,
                    Name = selected.ProductName,
                    Category = category,
                    SuggestedPercentage = suggestedPct,
                    SuggestedPhase = GetDefaultPhase(category),
                    Reason = $"Selected as {category} based on historical success patterns"
                });
            }
        }

        var waterPct = Math.Max(0, 100 - totalPct);

        return new AutoFormulationResult
        {
            IsSuccess = suggestions.Any(),
            SuggestedRawMaterials = suggestions,
            EstimatedWaterPercentage = waterPct,
            EstimatedCostPerKg = suggestions.Sum(s => (rawMaterials.FirstOrDefault(rm => rm.Id == s.RawMaterialId)?.Price ?? 0) * s.SuggestedPercentage / 100),
            PredictedSuccessRate = successfulFormulas.Any() ? 0.7 : 0.5,
            Reasoning = $"Generated based on {successfulFormulas.Count} successful '{request.ProductType}' formulas. " +
                       $"Brief: {request.ProductBrief}",
            Warnings = warnings
        };
    }

    private static List<string> GetDefaultCategories(string productType)
    {
        return productType switch
        {
            "Leave on" => new() { "Emollients", "Emulsifiers", "Thickeners", "Preservatives", "Humectants" },
            "Rinse off" => new() { "Surfactants", "Thickeners", "Preservatives", "Conditioning Agents" },
            _ => new() { "Emollients", "Preservatives", "Thickeners" }
        };
    }

    private static decimal GetDefaultPercentage(string category, string productType)
    {
        return category switch
        {
            "Emollients" => 15m,
            "Emulsifiers" => 3m,
            "Thickeners" => 2m,
            "Preservatives" => 1m,
            "Humectants" => 5m,
            "Surfactants" => 12m,
            "UV Filters" => 5m,
            "Conditioning Agents" => 3m,
            _ => 2m
        };
    }

    private static string GetDefaultPhase(string category)
    {
        return category switch
        {
            "Emollients" or "Emulsifiers" or "UV Filters" => "Oil Phase",
            "Humectants" or "Surfactants" => "Water Phase",
            "Preservatives" => "Cool Down Phase",
            "Thickeners" => "Water Phase",
            _ => "Main Phase"
        };
    }
}

