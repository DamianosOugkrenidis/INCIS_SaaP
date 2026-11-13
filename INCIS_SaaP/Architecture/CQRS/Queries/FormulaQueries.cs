// Author: Damianos Pythagoras Ougkrenidis
// CQRS - Formula Queries (Read operations)

using INCIS_SaaP.Domain.Common;

namespace INCIS_SaaP.Architecture.CQRS.Queries;

/// <summary>
/// Base interface for all queries (read operations)
/// </summary>
public interface IQuery<TResult>
{
    string? UserId { get; }
    string? TenantId { get; }
}

/// <summary>
/// Base query class with common properties
/// </summary>
public abstract class QueryBase<TResult> : IQuery<TResult>
{
    public string? UserId { get; init; }
    public string? TenantId { get; init; }
}

// ============================================================
// FORMULA QUERIES
// ============================================================

/// <summary>
/// Query to get formula details by ID
/// </summary>
public sealed class GetFormulaByIdQuery : QueryBase<FormulaDetailDto?>
{
    public int FormulaId { get; init; }
    public bool IncludeRawMaterials { get; init; } = true;
    public bool IncludeSafetyData { get; init; } = true;
    public bool IncludeProductionHistory { get; init; }
}

/// <summary>
/// Query to get paginated formula list
/// </summary>
public sealed class GetFormulasQuery : QueryBase<PaginatedResult<FormulaListDto>>
{
    public string? SearchTerm { get; init; }
    public string? ProductType { get; init; }
    public int? ClientId { get; init; }
    public bool? IsWIP { get; init; }
    public bool? IsValidated { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string SortBy { get; init; } = "CreationDate";
    public bool SortDescending { get; init; } = true;
}

/// <summary>
/// Query to search formulas by raw material
/// </summary>
public sealed class SearchFormulasByRawMaterialQuery : QueryBase<List<FormulaListDto>>
{
    public int RawMaterialId { get; init; }
}

/// <summary>
/// Query to get water percentage status for a formula
/// </summary>
public sealed class GetFormulaWaterStatusQuery : QueryBase<WaterStatusDto>
{
    public int FormulaId { get; init; }
}

/// <summary>
/// Query to get formula compatibility analysis
/// </summary>
public sealed class GetFormulaCompatibilityQuery : QueryBase<CompatibilityReportDto>
{
    public int FormulaId { get; init; }
}

// ============================================================
// DTOs FOR QUERY RESULTS
// ============================================================

public sealed class FormulaDetailDto
{
    public int Id { get; init; }
    public string FormulaCode { get; init; } = string.Empty;
    public string? ProductName { get; init; }
    public string? FormulaName { get; init; }
    public string? ProductType { get; init; }
    public string? ManufacturingSite { get; init; }
    public string? ManufacturingMethod { get; init; }
    public bool IsWIP { get; init; }
    public bool IsValidated { get; init; }
    public DateTime? ValidationDate { get; init; }
    public decimal TotalPercentage { get; init; }
    public decimal WaterPercentage { get; init; }
    public string? ClientName { get; init; }
    public List<FormulaRawMaterialDto> RawMaterials { get; init; } = new();
    public List<ProductionBatchDto> ProductionBatches { get; init; } = new();
}

public sealed class FormulaListDto
{
    public int Id { get; init; }
    public string FormulaCode { get; init; } = string.Empty;
    public string? ProductName { get; init; }
    public string? ProductType { get; init; }
    public string? ClientName { get; init; }
    public bool IsWIP { get; init; }
    public bool IsValidated { get; init; }
    public int RawMaterialCount { get; init; }
    public DateTime? CreationDate { get; init; }
    public DateTime? LastModified { get; init; }
}

public sealed class FormulaRawMaterialDto
{
    public int RawMaterialId { get; init; }
    public string RawMaterialName { get; init; } = string.Empty;
    public string? SupplierName { get; init; }
    public decimal? Percentage { get; init; }
    public string? Phase { get; init; }
    public string? Category { get; init; }
}

public sealed class ProductionBatchDto
{
    public int Id { get; init; }
    public string BatchCode { get; init; } = string.Empty;
    public decimal TotalQuantityKg { get; init; }
    public DateTime ProductionDate { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class WaterStatusDto
{
    public decimal TotalRawMaterialPercentage { get; init; }
    public decimal WaterPercentage { get; init; }
    public bool IsAnhydrous { get; init; }
    public bool IsValid { get; init; }
    public string StatusMessage { get; init; } = string.Empty;
}

public sealed class CompatibilityReportDto
{
    public int FormulaId { get; init; }
    public List<IncompatibilityWarning> Warnings { get; init; } = new();
    public double OverallCompatibilityScore { get; init; }
    public bool HasCriticalIssues { get; init; }
}

public sealed class IncompatibilityWarning
{
    public string RawMaterial1 { get; init; } = string.Empty;
    public string RawMaterial2 { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty; // Low, Medium, High, Critical
}

public sealed class PaginatedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
