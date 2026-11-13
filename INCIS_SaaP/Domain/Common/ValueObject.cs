// Author: Damianos Pythagoras Ougkrenidis
// Domain-Driven Design - Base Value Object

namespace INCIS_SaaP.Domain.Common;

/// <summary>
/// Base class for Value Objects in DDD.
/// Value Objects are immutable and compared by their structural equality.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Returns the atomic values that define equality for this value object
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public bool Equals(ValueObject? other)
    {
        return Equals((object?)other);
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Generic Result type for domain operations (instead of exceptions for business logic)
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public IReadOnlyList<string> Errors { get; }

    private Result(bool isSuccess, T? value, string? error, IReadOnlyList<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Errors = errors ?? Array.Empty<string>();
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
    public static Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors.FirstOrDefault(), errors.ToList());

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error ?? "Unknown error");
    }

    public async Task<TResult> MatchAsync<TResult>(Func<T, Task<TResult>> onSuccess, Func<string, Task<TResult>> onFailure)
    {
        return IsSuccess ? await onSuccess(Value!) : await onFailure(Error ?? "Unknown error");
    }
}

/// <summary>
/// Non-generic Result for operations that don't return a value
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public IReadOnlyList<string> Errors { get; }

    private Result(bool isSuccess, string? error, IReadOnlyList<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? Array.Empty<string>();
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result Failure(IEnumerable<string> errors) => new(false, errors.FirstOrDefault(), errors.ToList());
}
