// Author: Damianos Pythagoras Ougkrenidis
// Domain Events - Raw Material Bounded Context

using INCIS_SaaP.Domain.Common;

namespace INCIS_SaaP.Domain.Events;

/// <summary>
/// Raised when a raw material composition is changed
/// </summary>
public sealed class RawMaterialCompositionChangedEvent : DomainEvent
{
    public int RawMaterialId { get; init; }
    public string RawMaterialName { get; init; } = string.Empty;
    public List<CompositionChange> Changes { get; init; } = new();
    public string ChangedBy { get; init; } = string.Empty;
    
    /// <summary>
    /// List of affected formula IDs that use this raw material
    /// </summary>
    public List<int> AffectedFormulaIds { get; init; } = new();
}

public sealed class CompositionChange
{
    public int INCIId { get; init; }
    public string INCIName { get; init; } = string.Empty;
    public decimal? OldMinPercentage { get; init; }
    public decimal? NewMinPercentage { get; init; }
    public decimal? OldMaxPercentage { get; init; }
    public decimal? NewMaxPercentage { get; init; }
    public ChangeType Type { get; init; }
}

public enum ChangeType
{
    Added,
    Modified,
    Removed
}

/// <summary>
/// Raised when a raw material is fully registered and evaluated
/// </summary>
public sealed class RawMaterialEvaluatedEvent : DomainEvent
{
    public int RawMaterialId { get; init; }
    public string RawMaterialName { get; init; } = string.Empty;
    public string EvaluatedBy { get; init; } = string.Empty;
    public bool IsApproved { get; init; }
    public string? Comments { get; init; }
    public List<string> MissingDocuments { get; init; } = new();
}

/// <summary>
/// Raised when a supplier is changed for a raw material
/// </summary>
public sealed class RawMaterialSupplierChangedEvent : DomainEvent
{
    public int RawMaterialId { get; init; }
    public string RawMaterialName { get; init; } = string.Empty;
    public int? OldSupplierId { get; init; }
    public string? OldSupplierName { get; init; }
    public int NewSupplierId { get; init; }
    public string NewSupplierName { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Raised when a raw material sample expires
/// </summary>
public sealed class RawMaterialSampleExpiredEvent : DomainEvent
{
    public int RawMaterialId { get; init; }
    public string RawMaterialName { get; init; } = string.Empty;
    public int SampleId { get; init; }
    public string LotNumber { get; init; } = string.Empty;
    public DateTime ExpiryDate { get; init; }
}

/// <summary>
/// Raised when a new raw material sample is added
/// </summary>
public sealed class RawMaterialSampleAddedEvent : DomainEvent
{
    public int RawMaterialId { get; init; }
    public string RawMaterialName { get; init; } = string.Empty;
    public string LotNumber { get; init; } = string.Empty;
    public DateTime ReceivedDate { get; init; }
    public DateTime ExpiryDate { get; init; }
}

/// <summary>
/// Raised when a raw material price changes
/// </summary>
public sealed class RawMaterialPriceChangedEvent : DomainEvent
{
    public int RawMaterialId { get; init; }
    public string RawMaterialName { get; init; } = string.Empty;
    public decimal? OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public string Currency { get; init; } = "EUR";
    
    /// <summary>
    /// Formulas affected by price change (for cost recalculation)
    /// </summary>
    public List<int> AffectedFormulaIds { get; init; } = new();
}
