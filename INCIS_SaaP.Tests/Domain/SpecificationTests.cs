using System.Linq.Expressions;
using FluentAssertions;
using INCIS_SaaP.Domain.Specifications;
using INCIS_SaaP.Models;
using Xunit;

namespace INCIS_SaaP.Tests.Domain;

/// <summary>
/// Tests for the Specification pattern (combinators + concrete business rules).
/// </summary>
public class SpecificationTests
{
    // Helper concrete spec for combinator tests
    private sealed class GreaterThanSpec : Specification<int>
    {
        private readonly int _value;
        public GreaterThanSpec(int value) => _value = value;
        public override Expression<Func<int, bool>> ToExpression() => x => x > _value;
    }

    private sealed class EvenSpec : Specification<int>
    {
        public override Expression<Func<int, bool>> ToExpression() => x => x % 2 == 0;
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsExpectedResult()
    {
        var spec = new GreaterThanSpec(5);
        spec.IsSatisfiedBy(10).Should().BeTrue();
        spec.IsSatisfiedBy(3).Should().BeFalse();
    }

    [Fact]
    public void And_RequiresBothConditions()
    {
        var spec = new GreaterThanSpec(5).And(new EvenSpec());

        spec.IsSatisfiedBy(8).Should().BeTrue();   // > 5 and even
        spec.IsSatisfiedBy(7).Should().BeFalse();  // > 5 but odd
        spec.IsSatisfiedBy(4).Should().BeFalse();  // even but not > 5
    }

    [Fact]
    public void Or_RequiresEitherCondition()
    {
        var spec = new GreaterThanSpec(5).Or(new EvenSpec());

        spec.IsSatisfiedBy(8).Should().BeTrue();   // both
        spec.IsSatisfiedBy(7).Should().BeTrue();   // > 5
        spec.IsSatisfiedBy(2).Should().BeTrue();   // even
        spec.IsSatisfiedBy(1).Should().BeFalse();  // neither
    }

    [Fact]
    public void Not_InvertsResult()
    {
        var spec = new EvenSpec().Not();

        spec.IsSatisfiedBy(3).Should().BeTrue();
        spec.IsSatisfiedBy(4).Should().BeFalse();
    }

    [Fact]
    public void CombinedSpec_ToExpression_Compiles()
    {
        var spec = new GreaterThanSpec(5).And(new EvenSpec()).Or(new GreaterThanSpec(100));
        var predicate = spec.ToExpression().Compile();

        predicate(8).Should().BeTrue();
        predicate(101).Should().BeTrue();
        predicate(3).Should().BeFalse();
    }

    // ---- Formula specifications ----

    [Fact]
    public void FormulaReadyForValidationSpec_TrueWhenNotWipHasMaterialsNotValidated()
    {
        var formula = new Formula
        {
            FormulaCode = "F-1",
            IsWIP = false,
            ValidationDate = null,
            FormulaRawMaterials = new List<FormulaRawMaterial>
            {
                new() { Percentage = 50, IsActive = true }
            }
        };

        new FormulaReadyForValidationSpec().IsSatisfiedBy(formula).Should().BeTrue();
    }

    [Fact]
    public void FormulaReadyForValidationSpec_FalseWhenWip()
    {
        var formula = new Formula
        {
            FormulaCode = "F-2",
            IsWIP = true,
            FormulaRawMaterials = new List<FormulaRawMaterial> { new() { Percentage = 50, IsActive = true } }
        };

        new FormulaReadyForValidationSpec().IsSatisfiedBy(formula).Should().BeFalse();
    }

    [Fact]
    public void FormulaValidatedSpec_TrueWhenValidatedAndNotWip()
    {
        var formula = new Formula { FormulaCode = "F-3", IsWIP = false, ValidationDate = DateTime.UtcNow };
        new FormulaValidatedSpec().IsSatisfiedBy(formula).Should().BeTrue();
    }

    [Fact]
    public void FormulaIsWIPSpec_ReflectsWipFlag()
    {
        new FormulaIsWIPSpec().IsSatisfiedBy(new Formula { FormulaCode = "F", IsWIP = true }).Should().BeTrue();
        new FormulaIsWIPSpec().IsSatisfiedBy(new Formula { FormulaCode = "F", IsWIP = false }).Should().BeFalse();
    }

    [Fact]
    public void FormulaPercentageValidSpec_TrueWhenSumUnder100()
    {
        var formula = new Formula
        {
            FormulaCode = "F",
            FormulaRawMaterials = new List<FormulaRawMaterial>
            {
                new() { Percentage = 40, IsActive = true },
                new() { Percentage = 30, IsActive = true },
                new() { Percentage = 999, IsActive = false } // ignored (inactive)
            }
        };

        new FormulaPercentageValidSpec().IsSatisfiedBy(formula).Should().BeTrue();
    }

    [Fact]
    public void FormulaIsAnhydrousSpec_TrueWhenSumIs100()
    {
        var formula = new Formula
        {
            FormulaCode = "F",
            FormulaRawMaterials = new List<FormulaRawMaterial>
            {
                new() { Percentage = 60, IsActive = true },
                new() { Percentage = 40, IsActive = true }
            }
        };

        new FormulaIsAnhydrousSpec().IsSatisfiedBy(formula).Should().BeTrue();
    }

    [Fact]
    public void FormulaBelongsToClientSpec_MatchesClientId()
    {
        var spec = new FormulaBelongsToClientSpec(42);
        spec.IsSatisfiedBy(new Formula { FormulaCode = "F", ClientId = 42 }).Should().BeTrue();
        spec.IsSatisfiedBy(new Formula { FormulaCode = "F", ClientId = 1 }).Should().BeFalse();
    }

    // ---- RawMaterial specifications ----

    [Fact]
    public void RawMaterialCompleteSpec_TrueWhenFullyDocumented()
    {
        var rm = new RawMaterial
        {
            Code = "RM1",
            ProductName = "X",
            IsFullyRegistered = true,
            IsEvaluated = true,
            HasTDS = true,
            HasMSDS = true,
            Composition = new List<RawMaterialComposition> { new() { INCIId = 1 } }
        };

        new RawMaterialCompleteSpec().IsSatisfiedBy(rm).Should().BeTrue();
        new RawMaterialIncompleteSpec().IsSatisfiedBy(rm).Should().BeFalse();
    }

    [Fact]
    public void RawMaterialIncompleteSpec_TrueWhenMissingDocs()
    {
        var rm = new RawMaterial { Code = "RM2", ProductName = "Y", IsFullyRegistered = false, IsEvaluated = true };
        new RawMaterialIncompleteSpec().IsSatisfiedBy(rm).Should().BeTrue();
    }

    [Fact]
    public void RawMaterialContainsINCISpec_MatchesNonImpurity()
    {
        var rm = new RawMaterial
        {
            Code = "RM3",
            ProductName = "Z",
            Composition = new List<RawMaterialComposition>
            {
                new() { INCIId = 5, IsImpurity = false },
                new() { INCIId = 9, IsImpurity = true }
            }
        };

        new RawMaterialContainsINCISpec(5).IsSatisfiedBy(rm).Should().BeTrue();
        new RawMaterialContainsINCISpec(9).IsSatisfiedBy(rm).Should().BeFalse(); // impurity excluded
        new RawMaterialContainsINCISpec(99).IsSatisfiedBy(rm).Should().BeFalse();
    }
}
