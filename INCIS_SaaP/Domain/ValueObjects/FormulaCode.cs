// Author: Damianos Pythagoras Ougkrenidis
// Domain Value Object - FormulaCode (immutable, self-validating)

using INCIS_SaaP.Domain.Common;
using System.Text.RegularExpressions;

namespace INCIS_SaaP.Domain.ValueObjects;

/// <summary>
/// Value Object representing a Formula Code.
/// Format: CustomerCode-abcd-T## (optionally followed by ? for WIP)
/// Examples: CUST001-0001-T01, CUST001-0001-T00?, CUST001-0002-T03
/// </summary>
public sealed class FormulaCode : ValueObject
{
    public string Value { get; }
    public string CustomerCode { get; }
    public string AbcdCode { get; }
    public string TestPart { get; }
    public bool IsWIP { get; }
    public int TestNumber { get; }
    public bool IsExternalDevelopment { get; }

    private static readonly Regex CodePattern = new(
        @"^([A-Za-z0-9]+)-([A-Za-z0-9]+)-T(\d{2})(\?)?$",
        RegexOptions.Compiled);

    private FormulaCode(string value, string customerCode, string abcdCode, string testPart, bool isWIP, int testNumber, bool isExternalDev)
    {
        Value = value;
        CustomerCode = customerCode;
        AbcdCode = abcdCode;
        TestPart = testPart;
        IsWIP = isWIP;
        TestNumber = testNumber;
        IsExternalDevelopment = isExternalDev;
    }

    /// <summary>
    /// Generates a new formula code from components
    /// </summary>
    public static Result<FormulaCode> Generate(string customerCode, string abcdCode, int testNumber, bool isExternalDevelopment, bool isWIP)
    {
        if (string.IsNullOrWhiteSpace(customerCode))
            return Result<FormulaCode>.Failure("Customer code is required.");

        if (string.IsNullOrWhiteSpace(abcdCode))
            return Result<FormulaCode>.Failure("ABCD code is required.");

        if (testNumber < 0 || testNumber > 99)
            return Result<FormulaCode>.Failure("Test number must be between 0 and 99.");

        var testPart = isExternalDevelopment ? "T00" : $"T{testNumber:D2}";
        var baseCode = $"{customerCode}-{abcdCode}-{testPart}";
        var fullCode = isWIP ? baseCode + "?" : baseCode;

        return Result<FormulaCode>.Success(new FormulaCode(
            fullCode, customerCode, abcdCode, testPart, isWIP, testNumber, isExternalDevelopment));
    }

    /// <summary>
    /// Parses an existing formula code string
    /// </summary>
    public static Result<FormulaCode> Parse(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result<FormulaCode>.Failure("Formula code cannot be empty.");

        var match = CodePattern.Match(code);
        if (!match.Success)
        {
            // Allow non-standard codes (legacy data)
            return Result<FormulaCode>.Success(new FormulaCode(
                code, code, "", "", code.EndsWith("?"), 0, false));
        }

        var customerCode = match.Groups[1].Value;
        var abcdCode = match.Groups[2].Value;
        var testNumber = int.Parse(match.Groups[3].Value);
        var isWIP = match.Groups[4].Success;
        var isExternalDev = testNumber == 0;
        var testPart = $"T{testNumber:D2}";

        return Result<FormulaCode>.Success(new FormulaCode(
            code, customerCode, abcdCode, testPart, isWIP, testNumber, isExternalDev));
    }

    /// <summary>
    /// Removes WIP status (called by R&amp;D scientist after stability tests)
    /// </summary>
    public Result<FormulaCode> RemoveWIP()
    {
        if (!IsWIP)
            return Result<FormulaCode>.Failure("Formula is not in WIP status.");

        var newValue = Value.TrimEnd('?');
        return Result<FormulaCode>.Success(new FormulaCode(
            newValue, CustomerCode, AbcdCode, TestPart, false, TestNumber, IsExternalDevelopment));
    }

    /// <summary>
    /// Sets WIP status
    /// </summary>
    public FormulaCode SetWIP()
    {
        if (IsWIP) return this;
        return new FormulaCode(
            Value + "?", CustomerCode, AbcdCode, TestPart, true, TestNumber, IsExternalDevelopment);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(FormulaCode code) => code.Value;
}
