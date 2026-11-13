// Author: Damianos Pythagoras Ougkrenidis
// Domain Value Object - INCIName (immutable, self-validating)

using INCIS_SaaP.Domain.Common;
using System.Text.RegularExpressions;

namespace INCIS_SaaP.Domain.ValueObjects;

/// <summary>
/// Value Object representing an INCI (International Nomenclature of Cosmetic Ingredients) name.
/// INCI names follow specific conventions: uppercase Latin characters, specific allowed characters.
/// Based on EU Regulation 1223/2009 and CosIng database.
/// </summary>
public sealed class INCIName : ValueObject
{
    public string Value { get; }
    public string NormalizedValue { get; }

    // INCI names use uppercase and specific characters (hyphens, spaces, numbers, parentheses)
    private static readonly Regex ValidINCIPattern = new(
        @"^[A-Z0-9\s\-/\(\)\[\],\.'+]+$",
        RegexOptions.Compiled);

    private INCIName(string value, string normalizedValue)
    {
        Value = value;
        NormalizedValue = normalizedValue;
    }

    /// <summary>
    /// Creates a new INCIName value object with validation
    /// </summary>
    public static Result<INCIName> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<INCIName>.Failure("INCI name cannot be empty.");

        var normalized = name.Trim().ToUpperInvariant();

        // Remove multiple spaces
        normalized = Regex.Replace(normalized, @"\s+", " ");

        if (normalized.Length < 2)
            return Result<INCIName>.Failure("INCI name must be at least 2 characters.");

        if (normalized.Length > 500)
            return Result<INCIName>.Failure("INCI name cannot exceed 500 characters.");

        // Validate characters (INCI names are typically uppercase with specific characters)
        if (!ValidINCIPattern.IsMatch(normalized))
        {
            // Allow mixed case for flexibility but normalize
            var upperCheck = normalized.ToUpperInvariant();
            if (!ValidINCIPattern.IsMatch(upperCheck))
            {
                return Result<INCIName>.Failure(
                    "INCI name contains invalid characters. Only uppercase Latin letters, numbers, hyphens, spaces, and parentheses are allowed.");
            }
        }

        return Result<INCIName>.Success(new INCIName(name.Trim(), normalized));
    }

    /// <summary>
    /// Creates an INCIName without strict validation (for importing existing data)
    /// </summary>
    public static INCIName CreateUnsafe(string name)
    {
        var normalized = (name ?? "").Trim().ToUpperInvariant();
        return new INCIName(name?.Trim() ?? "", normalized);
    }

    /// <summary>
    /// Checks if this INCI name matches another (case-insensitive, normalized)
    /// </summary>
    public bool Matches(INCIName other)
    {
        return NormalizedValue == other.NormalizedValue;
    }

    /// <summary>
    /// Checks if this INCI name contains a search term
    /// </summary>
    public bool Contains(string searchTerm)
    {
        return NormalizedValue.Contains(searchTerm.ToUpperInvariant());
    }

    /// <summary>
    /// Calculates similarity score with another INCI name (for fuzzy matching)
    /// Uses Levenshtein distance normalized to 0-1 range
    /// </summary>
    public double SimilarityTo(INCIName other)
    {
        var maxLen = Math.Max(NormalizedValue.Length, other.NormalizedValue.Length);
        if (maxLen == 0) return 1.0;

        var distance = LevenshteinDistance(NormalizedValue, other.NormalizedValue);
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
        {
            for (int j = 1; j <= m; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(
                    d[i - 1, j] + 1,
                    d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return NormalizedValue;
    }

    public override string ToString() => Value;

    public static implicit operator string(INCIName name) => name.Value;
}
