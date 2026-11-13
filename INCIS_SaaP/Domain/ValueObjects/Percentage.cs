// Author: Damianos Pythagoras Ougkrenidis
// Domain Value Object - Percentage (immutable, self-validating)

using INCIS_SaaP.Domain.Common;

namespace INCIS_SaaP.Domain.ValueObjects;

/// <summary>
/// Value Object representing a percentage value (0-100).
/// Enforces business rules for formula composition percentages.
/// </summary>
public sealed class Percentage : ValueObject
{
    public decimal Value { get; }

    private Percentage(decimal value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a Percentage value object with validation
    /// </summary>
    public static Result<Percentage> Create(decimal value)
    {
        if (value < 0)
            return Result<Percentage>.Failure("Percentage cannot be negative.");

        if (value > 100)
            return Result<Percentage>.Failure("Percentage cannot exceed 100%.");

        return Result<Percentage>.Success(new Percentage(Math.Round(value, 4)));
    }

    /// <summary>
    /// Creates a Percentage for composition ranges (min/max)
    /// </summary>
    public static Result<Percentage> CreateForComposition(decimal value)
    {
        if (value < 0)
            return Result<Percentage>.Failure("Composition percentage cannot be negative.");

        if (value > 100)
            return Result<Percentage>.Failure("Composition percentage cannot exceed 100%.");

        return Result<Percentage>.Success(new Percentage(Math.Round(value, 6)));
    }

    /// <summary>
    /// Creates a zero percentage (for anhydrous formulas)
    /// </summary>
    public static Percentage Zero => new(0);

    /// <summary>
    /// Creates 100% (full composition)
    /// </summary>
    public static Percentage Full => new(100);

    /// <summary>
    /// Calculates remaining percentage to reach 100%
    /// </summary>
    public Percentage RemainingToFull() => new(100 - Value);

    /// <summary>
    /// Adds two percentages
    /// </summary>
    public Result<Percentage> Add(Percentage other)
    {
        var sum = Value + other.Value;
        if (sum > 100)
            return Result<Percentage>.Failure($"Sum of percentages ({sum}%) exceeds 100%.");
        return Result<Percentage>.Success(new Percentage(sum));
    }

    public bool IsZero => Value == 0;
    public bool IsFull => Value == 100;
    public bool IsValid => Value >= 0 && Value <= 100;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"{Value:F2}%";

    // Implicit conversions
    public static implicit operator decimal(Percentage p) => p.Value;
}
