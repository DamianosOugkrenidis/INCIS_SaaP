using FluentAssertions;
using INCIS_SaaP.Domain.ValueObjects;
using Xunit;

namespace INCIS_SaaP.Tests.Models;

/// <summary>Unit tests for Domain Value Objects (pure logic).</summary>
public class DomainValueObjectsTests
{
    // ---------------- FormulaCode ----------------

    [Fact]
    public void FormulaCode_Generate_Standard_Succeeds()
    {
        var r = FormulaCode.Generate("CUST001", "0001", 1, isExternalDevelopment: false, isWIP: false);
        r.IsSuccess.Should().BeTrue();
        r.Value!.Value.Should().Be("CUST001-0001-T01");
        r.Value.IsWIP.Should().BeFalse();
        r.Value.TestNumber.Should().Be(1);
    }

    [Fact]
    public void FormulaCode_Generate_External_UsesT00()
    {
        var r = FormulaCode.Generate("C", "0001", 5, isExternalDevelopment: true, isWIP: true);
        r.IsSuccess.Should().BeTrue();
        r.Value!.Value.Should().Be("C-0001-T00?");
        r.Value.IsWIP.Should().BeTrue();
        r.Value.TestPart.Should().Be("T00");
    }

    [Theory]
    [InlineData("", "0001", 1)]
    [InlineData("C", "", 1)]
    [InlineData("C", "0001", 100)]
    [InlineData("C", "0001", -1)]
    public void FormulaCode_Generate_Invalid_Fails(string cust, string abcd, int test)
    {
        FormulaCode.Generate(cust, abcd, test, false, false).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void FormulaCode_Parse_Standard_ExtractsComponents()
    {
        var r = FormulaCode.Parse("CUST001-0002-T03");
        r.IsSuccess.Should().BeTrue();
        r.Value!.CustomerCode.Should().Be("CUST001");
        r.Value.AbcdCode.Should().Be("0002");
        r.Value.TestNumber.Should().Be(3);
        r.Value.IsWIP.Should().BeFalse();
    }

    [Fact]
    public void FormulaCode_Parse_Wip_DetectsQuestionMark()
    {
        var r = FormulaCode.Parse("CUST001-0002-T00?");
        r.Value!.IsWIP.Should().BeTrue();
        r.Value.IsExternalDevelopment.Should().BeTrue(); // T00
    }

    [Fact]
    public void FormulaCode_Parse_Legacy_NonStandard_Succeeds()
    {
        var r = FormulaCode.Parse("LEGACY_CODE_123");
        r.IsSuccess.Should().BeTrue();
        r.Value!.Value.Should().Be("LEGACY_CODE_123");
    }

    [Fact]
    public void FormulaCode_Parse_Empty_Fails() =>
        FormulaCode.Parse("  ").IsSuccess.Should().BeFalse();

    [Fact]
    public void FormulaCode_RemoveWip_And_SetWip_Work()
    {
        var wip = FormulaCode.Parse("C-0001-T01?").Value!;
        var removed = wip.RemoveWIP();
        removed.IsSuccess.Should().BeTrue();
        removed.Value!.Value.Should().Be("C-0001-T01");
        removed.Value.IsWIP.Should().BeFalse();

        // Removing WIP when not WIP fails
        removed.Value.RemoveWIP().IsSuccess.Should().BeFalse();

        // SetWIP adds '?'
        var set = removed.Value.SetWIP();
        set.Value.Should().EndWith("?");
        set.SetWIP().Should().BeSameAs(set); // idempotent
    }

    [Fact]
    public void FormulaCode_Equality_And_ImplicitString()
    {
        var a = FormulaCode.Parse("C-0001-T01").Value!;
        var b = FormulaCode.Parse("C-0001-T01").Value!;
        (a == b).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
        string s = a; // implicit
        s.Should().Be("C-0001-T01");
        a.ToString().Should().Be("C-0001-T01");
    }

    // ---------------- INCIName ----------------

    [Fact]
    public void INCIName_Create_NormalizesAndUppercases()
    {
        var r = INCIName.Create("  aqua   purified ");
        r.IsSuccess.Should().BeTrue();
        r.Value!.NormalizedValue.Should().Be("AQUA PURIFIED");
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void INCIName_Create_TooShortOrEmpty_Fails(string name) =>
        INCIName.Create(name).IsSuccess.Should().BeFalse();

    [Fact]
    public void INCIName_Create_InvalidChars_Fails() =>
        INCIName.Create("水 ингредиент").IsSuccess.Should().BeFalse();

    [Fact]
    public void INCIName_Matches_And_Contains()
    {
        var a = INCIName.Create("Glycerin").Value!;
        var b = INCIName.CreateUnsafe("GLYCERIN");
        a.Matches(b).Should().BeTrue();
        a.Contains("glyc").Should().BeTrue();
    }

    [Fact]
    public void INCIName_SimilarityTo_IdenticalIsOne_DifferentIsLower()
    {
        var a = INCIName.Create("Glycerin").Value!;
        a.SimilarityTo(INCIName.Create("Glycerin").Value!).Should().Be(1.0);
        a.SimilarityTo(INCIName.Create("Glycerol").Value!).Should().BeLessThan(1.0).And.BeGreaterThan(0.0);
    }

    // ---------------- Percentage ----------------

    [Fact]
    public void Percentage_Create_ValidatesRange()
    {
        Percentage.Create(50).IsSuccess.Should().BeTrue();
        Percentage.Create(-1).IsSuccess.Should().BeFalse();
        Percentage.Create(101).IsSuccess.Should().BeFalse();
        Percentage.CreateForComposition(0.0001m).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Percentage_ZeroFull_RemainingAndFlags()
    {
        Percentage.Zero.IsZero.Should().BeTrue();
        Percentage.Full.IsFull.Should().BeTrue();
        Percentage.Create(30).Value!.RemainingToFull().Value.Should().Be(70);
        Percentage.Create(50).Value!.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Percentage_Add_RespectsUpperBound()
    {
        var a = Percentage.Create(60).Value!;
        a.Add(Percentage.Create(30).Value!).IsSuccess.Should().BeTrue();
        a.Add(Percentage.Create(50).Value!).IsSuccess.Should().BeFalse(); // 110 > 100
    }

    [Fact]
    public void Percentage_ImplicitDecimal_And_ToString()
    {
        var p = Percentage.Create(12.5m).Value!;
        decimal d = p;
        d.Should().Be(12.5m);
        // ToString χρησιμοποιεί το τρέχον culture (τελεία ή κόμμα ως δεκαδικό)
        p.ToString().Should().BeOneOf("12.50%", "12,50%");
    }
}
