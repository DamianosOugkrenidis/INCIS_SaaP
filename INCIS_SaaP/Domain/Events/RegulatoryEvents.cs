// Author: Damianos Pythagoras Ougkrenidis
// Domain Events - Regulatory Bounded Context

using INCIS_SaaP.Domain.Common;

namespace INCIS_SaaP.Domain.Events;

/// <summary>
/// Raised when a new INCI restriction is added
/// </summary>
public sealed class INCIRestrictionAddedEvent : DomainEvent
{
    public int RestrictionId { get; init; }
    public int INCIId { get; init; }
    public string INCIName { get; init; } = string.Empty;
    public string Regulation { get; init; } = string.Empty;
    public decimal? MaxPercentage { get; init; }
    public bool IsRinseOffOnly { get; init; }
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Formulas that may be affected by this new restriction
    /// </summary>
    public List<int> PotentiallyAffectedFormulaIds { get; init; } = new();
}

/// <summary>
/// Raised when a restriction is modified
/// </summary>
public sealed class INCIRestrictionModifiedEvent : DomainEvent
{
    public int RestrictionId { get; init; }
    public int INCIId { get; init; }
    public string INCIName { get; init; } = string.Empty;
    public decimal? OldMaxPercentage { get; init; }
    public decimal? NewMaxPercentage { get; init; }
    public string ModifiedBy { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Raised when a regulation change is detected (by ML scraper)
/// </summary>
public sealed class RegulationChangeDetectedEvent : DomainEvent
{
    public string RegulationName { get; init; } = string.Empty;
    public string ChangeDescription { get; init; } = string.Empty;
    public string SourceUrl { get; init; } = string.Empty;
    public DateTime DetectedDate { get; init; }
    public string Severity { get; init; } = "Info"; // Info, Warning, Critical
    public List<string> AffectedINCINames { get; init; } = new();
    public List<int> AffectedFormulaIds { get; init; } = new();
}

/// <summary>
/// Raised when allergen risk score is calculated for a formula
/// </summary>
public sealed class AllergenRiskCalculatedEvent : DomainEvent
{
    public int FormulaId { get; init; }
    public string FormulaCode { get; init; } = string.Empty;
    public double RiskScore { get; init; }
    public string RiskLevel { get; init; } = string.Empty; // Low, Medium, High, Critical
    public List<AllergenRiskDetail> Details { get; init; } = new();
}

public sealed class AllergenRiskDetail
{
    public string AllergenName { get; init; } = string.Empty;
    public decimal ConcentrationPercent { get; init; }
    public decimal RegulatoryLimit { get; init; }
    public double ContributionToRisk { get; init; }
}
