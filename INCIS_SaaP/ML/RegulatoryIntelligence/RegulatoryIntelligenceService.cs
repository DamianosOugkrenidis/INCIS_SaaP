// Author: Damianos Pythagoras Ougkrenidis
// ML - Regulatory Intelligence (Regulation change detection, restriction impact, allergen risk)

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace INCIS_SaaP.ML.RegulatoryIntelligence;

/// <summary>
/// Detected regulatory change record
/// </summary>
public class RegulatoryChange
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Regulation { get; set; } = string.Empty; // "EC 1223/2009"
    
    [MaxLength(500)]
    public string? SourceUrl { get; set; }
    
    /// <summary>
    /// Severity: "Info", "Warning", "Critical"
    /// </summary>
    [MaxLength(20)]
    public string Severity { get; set; } = "Info";
    
    /// <summary>
    /// Affected INCI substance names (JSON array)
    /// </summary>
    public string AffectedINCIsJson { get; set; } = "[]";
    
    /// <summary>
    /// Affected formula IDs (JSON array)
    /// </summary>
    public string AffectedFormulasJson { get; set; } = "[]";
    
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveDate { get; set; }
    
    /// <summary>
    /// Status: "New", "UnderReview", "ActionRequired", "Resolved"
    /// </summary>
    [MaxLength(30)]
    public string Status { get; set; } = "New";
    
    [MaxLength(100)]
    public string? ReviewedBy { get; set; }
    
    [MaxLength(1000)]
    public string? ActionTaken { get; set; }
}

/// <summary>
/// Allergen risk assessment per formula
/// </summary>
public class AllergenRiskAssessment
{
    [Key]
    public int Id { get; set; }
    
    public int FormulaId { get; set; }
    
    [MaxLength(100)]
    public string FormulaCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Overall risk score (0-100)
    /// </summary>
    public double OverallRiskScore { get; set; }
    
    /// <summary>
    /// Risk level: "Low", "Medium", "High", "Critical"
    /// </summary>
    [MaxLength(20)]
    public string RiskLevel { get; set; } = "Low";
    
    /// <summary>
    /// Detailed breakdown per allergen (JSON)
    /// </summary>
    public string DetailedBreakdownJson { get; set; } = "[]";
    
    /// <summary>
    /// Whether formula complies with EU allergen labeling requirements
    /// </summary>
    public bool IsCompliant { get; set; } = true;
    
    /// <summary>
    /// Allergens that exceed regulatory threshold and must be declared
    /// </summary>
    public string MustDeclareJson { get; set; } = "[]";
    
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string? AssessedBy { get; set; }
}

/// <summary>
/// INCI synonym mapping (for fuzzy matching commercial names to INCI names)
/// </summary>
public class INCISynonym
{
    [Key]
    public int Id { get; set; }
    
    public int INCIId { get; set; }
    
    [Required]
    [MaxLength(300)]
    public string INCIName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(300)]
    public string Synonym { get; set; } = string.Empty;
    
    /// <summary>
    /// Type: "CommercialName", "Abbreviation", "ChemicalName", "CommonName"
    /// </summary>
    [MaxLength(30)]
    public string SynonymType { get; set; } = "CommercialName";
    
    /// <summary>
    /// Confidence of the mapping (0-1)
    /// </summary>
    public double Confidence { get; set; } = 1.0;
    
    /// <summary>
    /// Source: "Manual", "CosIng", "MLPredicted"
    /// </summary>
    [MaxLength(20)]
    public string Source { get; set; } = "Manual";
}

/// <summary>
/// Restriction impact analysis result
/// </summary>
public class RestrictionImpactAnalysis
{
    public int RestrictionId { get; init; }
    public string INCIName { get; init; } = string.Empty;
    public string RegulationReference { get; init; } = string.Empty;
    public decimal? NewMaxPercentage { get; init; }
    public List<ImpactedFormula> ImpactedFormulas { get; init; } = new();
    public List<AlternativeSuggestion> Alternatives { get; init; } = new();
    public string Severity { get; init; } = string.Empty;
    public string ActionRequired { get; init; } = string.Empty;
}

public class ImpactedFormula
{
    public int FormulaId { get; init; }
    public string FormulaCode { get; init; } = string.Empty;
    public decimal CurrentConcentration { get; init; }
    public decimal AllowedConcentration { get; init; }
    public bool IsNonCompliant { get; init; }
    public string RequiredAction { get; init; } = string.Empty;
}

public class AlternativeSuggestion
{
    public int RawMaterialId { get; init; }
    public string RawMaterialName { get; init; } = string.Empty;
    public string INCIName { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public double SimilarityScore { get; init; }
}

/// <summary>
/// Regulatory Intelligence Service interface
/// </summary>
public interface IRegulatoryIntelligenceService
{
    /// <summary>
    /// Analyzes impact of a new/modified restriction on existing formulas
    /// </summary>
    Task<RestrictionImpactAnalysis> AnalyzeRestrictionImpactAsync(int restrictionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calculates allergen risk score for a formula
    /// </summary>
    Task<AllergenRiskAssessment> AssessAllergenRiskAsync(int formulaId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resolves INCI name from commercial/synonym name (fuzzy matching)
    /// </summary>
    Task<List<INCISynonymMatch>> ResolveINCINameAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all pending regulatory changes
    /// </summary>
    Task<List<RegulatoryChange>> GetPendingChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Scans for regulatory changes (called by background job)
    /// </summary>
    Task<int> ScanForChangesAsync(CancellationToken cancellationToken = default);
}

public class INCISynonymMatch
{
    public int INCIId { get; init; }
    public string INCIName { get; init; } = string.Empty;
    public string MatchedSynonym { get; init; } = string.Empty;
    public double MatchScore { get; init; }
    public string MatchType { get; init; } = string.Empty;
}

/// <summary>
/// Regulatory Intelligence Service implementation
/// </summary>
public class RegulatoryIntelligenceService : IRegulatoryIntelligenceService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RegulatoryIntelligenceService> _logger;

    // EU allergen threshold values (percentage) for declaration
    private static readonly Dictionary<string, decimal> AllergenThresholds = new()
    {
        ["LIMONENE"] = 0.001m,
        ["LINALOOL"] = 0.001m,
        ["CITRONELLOL"] = 0.001m,
        ["GERANIOL"] = 0.001m,
        ["CITRAL"] = 0.001m,
        ["EUGENOL"] = 0.001m,
        ["COUMARIN"] = 0.001m,
        ["CINNAMAL"] = 0.001m,
        ["HYDROXYCITRONELLAL"] = 0.001m,
        ["BENZYL ALCOHOL"] = 0.001m,
        ["BENZYL SALICYLATE"] = 0.001m,
        ["CINNAMYL ALCOHOL"] = 0.001m,
        ["FARNESOL"] = 0.001m,
        ["BUTYLPHENYL METHYLPROPIONAL"] = 0.001m,
        ["ALPHA-ISOMETHYL IONONE"] = 0.001m,
        ["HEXYL CINNAMAL"] = 0.001m,
        ["ISOEUGENOL"] = 0.001m,
        ["AMYL CINNAMAL"] = 0.001m,
        ["BENZYL BENZOATE"] = 0.001m,
        ["BENZYL CINNAMATE"] = 0.001m,
        ["ANISE ALCOHOL"] = 0.001m,
        ["EVERNIA PRUNASTRI EXTRACT"] = 0.001m,
        ["EVERNIA FURFURACEA EXTRACT"] = 0.001m,
        ["METHYL 2-OCTYNOATE"] = 0.001m,
        ["HYDROXYISOHEXYL 3-CYCLOHEXENE CARBOXALDEHYDE"] = 0.001m
    };

    // Leave-on thresholds are 10x lower than rinse-off
    private const decimal LeaveOnMultiplier = 0.001m; // 0.001%
    private const decimal RinseOffMultiplier = 0.01m;  // 0.01%

    public RegulatoryIntelligenceService(IServiceProvider serviceProvider, ILogger<RegulatoryIntelligenceService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<RestrictionImpactAnalysis> AnalyzeRestrictionImpactAsync(int restrictionId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var restriction = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                dbContext.Set<Models.Restriction>()
                    .Include(r => r.INCI)
                    .Where(r => r.Id == restrictionId),
                cancellationToken);

        if (restriction == null)
            return new RestrictionImpactAnalysis { Severity = "Unknown", ActionRequired = "Restriction not found" };

        // Find all formulas that contain this INCI
        var affectedFormulaRMs = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<Models.FormulaRawMaterial>()
                    .Include(frm => frm.Formula)
                    .Include(frm => frm.RawMaterial)
                    .ThenInclude(rm => rm!.Composition)
                    .Where(frm => frm.IsActive && frm.RawMaterial != null
                        && frm.RawMaterial.Composition.Any(c => c.INCIId == restriction.INCIId)),
                cancellationToken);

        var impacted = new List<ImpactedFormula>();
        foreach (var frm in affectedFormulaRMs)
        {
            if (frm.Formula == null || frm.RawMaterial == null) continue;

            var comp = frm.RawMaterial.Composition.FirstOrDefault(c => c.INCIId == restriction.INCIId);
            var maxConcentration = (frm.Percentage ?? 0) * (comp?.MaxPercentage ?? 100) / 100;

            var isNonCompliant = restriction.MaxPercentage.HasValue && maxConcentration > restriction.MaxPercentage.Value;

            impacted.Add(new ImpactedFormula
            {
                FormulaId = frm.FormulaId,
                FormulaCode = frm.Formula.FormulaCode,
                CurrentConcentration = maxConcentration,
                AllowedConcentration = restriction.MaxPercentage ?? 100,
                IsNonCompliant = isNonCompliant,
                RequiredAction = isNonCompliant ? "Reformulation required" : "Monitoring"
            });
        }

        // Find alternatives (raw materials without the restricted INCI)
        var alternatives = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<Models.RawMaterial>()
                    .Where(rm => rm.IsActive && rm.IsEvaluated
                        && !rm.Composition.Any(c => c.INCIId == restriction.INCIId))
                    .Take(10),
                cancellationToken);

        var severity = impacted.Any(f => f.IsNonCompliant) ? "Critical" : "Warning";

        return new RestrictionImpactAnalysis
        {
            RestrictionId = restrictionId,
            INCIName = restriction.INCI?.Name ?? "",
            RegulationReference = restriction.Regulation ?? "",
            NewMaxPercentage = restriction.MaxPercentage,
            ImpactedFormulas = impacted,
            Alternatives = alternatives.Select(a => new AlternativeSuggestion
            {
                RawMaterialId = a.Id,
                RawMaterialName = a.ProductName,
                INCIName = a.MainINCI?.Name ?? "",
                Reason = "Does not contain restricted substance",
                SimilarityScore = 0.5
            }).ToList(),
            Severity = severity,
            ActionRequired = impacted.Any(f => f.IsNonCompliant)
                ? $"{impacted.Count(f => f.IsNonCompliant)} formulas require immediate reformulation"
                : "Monitor - no immediate action required"
        };
    }

    public async Task<AllergenRiskAssessment> AssessAllergenRiskAsync(int formulaId, CancellationToken cancellationToken = default)
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
            return new AllergenRiskAssessment { FormulaId = formulaId, RiskLevel = "Unknown" };

        var isLeaveOn = formula.ProductType?.Contains("Leave") == true;
        var threshold = isLeaveOn ? LeaveOnMultiplier : RinseOffMultiplier;

        double totalRisk = 0;
        var mustDeclare = new List<string>();
        var breakdown = new List<object>();

        foreach (var frm in formula.FormulaRawMaterials.Where(f => f.IsActive))
        {
            if (frm.RawMaterial?.Allergens == null) continue;

            // Parse allergens from the raw material
            var allergens = frm.RawMaterial.Allergens.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim().ToUpperInvariant());

            foreach (var allergen in allergens)
            {
                if (AllergenThresholds.TryGetValue(allergen, out var regulatoryThreshold))
                {
                    // Estimate concentration in final formula
                    var estimatedConcentration = (frm.Percentage ?? 0) / 100; // Simplified

                    if (estimatedConcentration > threshold)
                    {
                        mustDeclare.Add(allergen);
                        totalRisk += 10;
                    }
                    else
                    {
                        totalRisk += 2;
                    }

                    breakdown.Add(new
                    {
                        Allergen = allergen,
                        Source = frm.RawMaterial.ProductName,
                        EstimatedConcentration = estimatedConcentration,
                        Threshold = threshold,
                        MustDeclare = estimatedConcentration > threshold
                    });
                }
            }
        }

        var riskLevel = totalRisk switch
        {
            <= 10 => "Low",
            <= 30 => "Medium",
            <= 60 => "High",
            _ => "Critical"
        };

        var assessment = new AllergenRiskAssessment
        {
            FormulaId = formulaId,
            FormulaCode = formula.FormulaCode,
            OverallRiskScore = Math.Min(100, totalRisk),
            RiskLevel = riskLevel,
            DetailedBreakdownJson = JsonSerializer.Serialize(breakdown),
            IsCompliant = true, // Declaration is required but formula is still compliant if declared
            MustDeclareJson = JsonSerializer.Serialize(mustDeclare.Distinct()),
            AssessedAt = DateTime.UtcNow
        };

        dbContext.Set<AllergenRiskAssessment>().Add(assessment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return assessment;
    }

    public async Task<List<INCISynonymMatch>> ResolveINCINameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<INCISynonymMatch>();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var normalized = searchTerm.Trim().ToUpperInvariant();

        // Exact match first
        var exactMatches = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<INCISynonym>()
                    .Where(s => s.Synonym.ToUpper() == normalized || s.INCIName.ToUpper() == normalized),
                cancellationToken);

        var results = exactMatches.Select(m => new INCISynonymMatch
        {
            INCIId = m.INCIId,
            INCIName = m.INCIName,
            MatchedSynonym = m.Synonym,
            MatchScore = 1.0,
            MatchType = "Exact"
        }).ToList();

        // Fuzzy match if no exact results
        if (!results.Any())
        {
            var allSynonyms = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .ToListAsync(
                    dbContext.Set<INCISynonym>().Take(10000),
                    cancellationToken);

            results = allSynonyms
                .Select(s => new { Synonym = s, Score = CalculateSimilarity(normalized, s.Synonym.ToUpperInvariant()) })
                .Where(x => x.Score > 0.6)
                .OrderByDescending(x => x.Score)
                .Take(10)
                .Select(x => new INCISynonymMatch
                {
                    INCIId = x.Synonym.INCIId,
                    INCIName = x.Synonym.INCIName,
                    MatchedSynonym = x.Synonym.Synonym,
                    MatchScore = x.Score,
                    MatchType = "Fuzzy"
                })
                .ToList();
        }

        return results;
    }

    public async Task<List<RegulatoryChange>> GetPendingChangesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<RegulatoryChange>()
                    .Where(c => c.Status == "New" || c.Status == "UnderReview")
                    .OrderByDescending(c => c.Severity == "Critical")
                    .ThenByDescending(c => c.DetectedAt),
                cancellationToken);
    }

    public async Task<int> ScanForChangesAsync(CancellationToken cancellationToken = default)
    {
        // In production, this would scrape EU Official Journal, SCCS opinions, etc.
        // For now, it's a placeholder that checks CosIng data for changes
        _logger.LogInformation("Scanning for regulatory changes...");
        return 0;
    }

    private static double CalculateSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0;
        
        var maxLen = Math.Max(s1.Length, s2.Length);
        if (maxLen == 0) return 1.0;

        // Contains check
        if (s1.Contains(s2) || s2.Contains(s1))
            return 0.85;

        // Levenshtein distance
        var distance = LevenshteinDistance(s1, s2);
        return 1.0 - (double)distance / maxLen;
    }

    private static int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;
        for (int i = 1; i <= n; i++)
            for (int j = 1; j <= m; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        return d[n, m];
    }
}

