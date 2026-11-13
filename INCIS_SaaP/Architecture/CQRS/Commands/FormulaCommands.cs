// Author: Damianos Pythagoras Ougkrenidis
// CQRS - Formula Commands (Write operations)

using INCIS_SaaP.Domain.Common;

namespace INCIS_SaaP.Architecture.CQRS.Commands;

/// <summary>
/// Base interface for all commands (write operations)
/// </summary>
public interface ICommand<TResult>
{
    string? UserId { get; }
    string? TenantId { get; }
    string? CorrelationId { get; }
}

/// <summary>
/// Base command class with common properties
/// </summary>
public abstract class CommandBase<TResult> : ICommand<TResult>
{
    public string? UserId { get; init; }
    public string? TenantId { get; init; }
    public string? CorrelationId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

// ============================================================
// FORMULA COMMANDS
// ============================================================

/// <summary>
/// Command to create a new formula
/// </summary>
public sealed class CreateFormulaCommand : CommandBase<Result<int>>
{
    public string FormulaCode { get; init; } = string.Empty;
    public string? ProductName { get; init; }
    public string? FormulaName { get; init; }
    public string? ProductType { get; init; }
    public string? ManufacturingSite { get; init; }
    public string? ManufacturingMethod { get; init; }
    public string? PPIdCode { get; init; }
    public string? PackagingInfo { get; init; }
    public string? PhysicochemicalProperties { get; init; }
    public string? MicrobiologicalProperties { get; init; }
    public string? Comments { get; init; }
    public int? ClientId { get; init; }
    public int? TestNumber { get; init; }
    public bool IsExternalDevelopment { get; init; }
    public bool IsWIP { get; init; } = true;
}

/// <summary>
/// Command to add a raw material to a formula
/// </summary>
public sealed class AddRawMaterialToFormulaCommand : CommandBase<Result>
{
    public int FormulaId { get; init; }
    public int RawMaterialId { get; init; }
    public decimal Percentage { get; init; }
    public string? Phase { get; init; }
}

/// <summary>
/// Command to remove a raw material from a formula
/// </summary>
public sealed class RemoveRawMaterialFromFormulaCommand : CommandBase<Result>
{
    public int FormulaId { get; init; }
    public int RawMaterialId { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Command to validate a formula (by RA Head)
/// </summary>
public sealed class ValidateFormulaCommand : CommandBase<Result>
{
    public int FormulaId { get; init; }
    public string? Comments { get; init; }
}

/// <summary>
/// Command to remove WIP status from a formula (by R&amp;D Scientist)
/// </summary>
public sealed class RemoveFormulaWIPCommand : CommandBase<Result>
{
    public int FormulaId { get; init; }
    public string? StabilityTestReference { get; init; }
    public string? ChallengeTestReference { get; init; }
}

/// <summary>
/// Command to produce a formula (create a production batch)
/// </summary>
public sealed class ProduceFormulaCommand : CommandBase<Result<int>>
{
    public int FormulaId { get; init; }
    public decimal TotalQuantityKg { get; init; }
    public string? BatchCode { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Command to clone a formula
/// </summary>
public sealed class CloneFormulaCommand : CommandBase<Result<int>>
{
    public int OriginalFormulaId { get; init; }
    public string NewFormulaCode { get; init; } = string.Empty;
    public int? NewClientId { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Command to calculate water percentage for a formula
/// </summary>
public sealed class CalculateWaterCommand : CommandBase<Result<decimal>>
{
    public int FormulaId { get; init; }
    public bool IsAnhydrous { get; init; }
}

/// <summary>
/// Command to log a formula combination for ML training
/// </summary>
public sealed class LogFormulaCombinationCommand : CommandBase<Result>
{
    public int FormulaId { get; init; }
    public bool PassedStabilityTest { get; init; }
    public bool PassedChallengeTest { get; init; }
    public string? FailureReason { get; init; }
}
