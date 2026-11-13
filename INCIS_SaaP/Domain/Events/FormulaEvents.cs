// Author: Damianos Pythagoras Ougkrenidis
// Domain Events - Formula Bounded Context

using INCIS_SaaP.Domain.Common;

namespace INCIS_SaaP.Domain.Events;

/// <summary>
/// Raised when a formula is validated by the RA Head
/// </summary>
public sealed class FormulaValidatedEvent : DomainEvent
{
    public int FormulaId { get; init; }
    public string FormulaCode { get; init; } = string.Empty;
    public string ValidatedBy { get; init; } = string.Empty;
    public DateTime ValidationDate { get; init; }
    public string? Comments { get; init; }
}

/// <summary>
/// Raised when a formula's WIP status is removed (after stability/challenge tests)
/// </summary>
public sealed class FormulaWIPRemovedEvent : DomainEvent
{
    public int FormulaId { get; init; }
    public string FormulaCode { get; init; } = string.Empty;
    public string RemovedBy { get; init; } = string.Empty;
    public string? StabilityTestReference { get; init; }
    public string? ChallengeTestReference { get; init; }
}

/// <summary>
/// Raised when a raw material is added to a formula
/// </summary>
public sealed class FormulaRawMaterialAddedEvent : DomainEvent
{
    public int FormulaId { get; init; }
    public int RawMaterialId { get; init; }
    public string RawMaterialName { get; init; } = string.Empty;
    public decimal Percentage { get; init; }
    public string? Phase { get; init; }
    public decimal TotalPercentageAfter { get; init; }
}

/// <summary>
/// Raised when a raw material is removed from a formula
/// </summary>
public sealed class FormulaRawMaterialRemovedEvent : DomainEvent
{
    public int FormulaId { get; init; }
    public int RawMaterialId { get; init; }
    public string RawMaterialName { get; init; } = string.Empty;
    public string? Reason { get; init; }
}

/// <summary>
/// Raised when a formula's water percentage is recalculated
/// </summary>
public sealed class FormulaWaterCalculatedEvent : DomainEvent
{
    public int FormulaId { get; init; }
    public decimal WaterPercentage { get; init; }
    public decimal TotalRawMaterialPercentage { get; init; }
    public bool IsAnhydrous { get; init; }
}

/// <summary>
/// Raised when a formula is produced (production batch created)
/// </summary>
public sealed class FormulaProducedEvent : DomainEvent
{
    public int FormulaId { get; init; }
    public int ProductionBatchId { get; init; }
    public string BatchCode { get; init; } = string.Empty;
    public decimal TotalQuantityKg { get; init; }
    public DateTime ProductionDate { get; init; }
}

/// <summary>
/// Raised when a formula is cloned
/// </summary>
public sealed class FormulaClonedEvent : DomainEvent
{
    public int OriginalFormulaId { get; init; }
    public int NewFormulaId { get; init; }
    public string NewFormulaCode { get; init; } = string.Empty;
    public string? Reason { get; init; }
}

/// <summary>
/// Raised when a formula version is created
/// </summary>
public sealed class FormulaVersionCreatedEvent : DomainEvent
{
    public int FormulaId { get; init; }
    public int VersionId { get; init; }
    public string VersionNumber { get; init; } = string.Empty;
    public string ChangeDescription { get; init; } = string.Empty;
}

/// <summary>
/// Raised when formula combination is logged (for ML training)
/// </summary>
public sealed class FormulaCombinationLoggedEvent : DomainEvent
{
    public int FormulaId { get; init; }
    public string FormulaCode { get; init; } = string.Empty;
    public string ProductType { get; init; } = string.Empty;
    public Dictionary<string, decimal> RawMaterialPercentages { get; init; } = new();
    public List<string> INCIIngredients { get; init; } = new();
    public bool PassedStabilityTest { get; init; }
    public bool PassedChallengeTest { get; init; }
    public string? FailureReason { get; init; }
}
