using FluentAssertions;
using INCIS_SaaP.Models;
using Xunit;

namespace INCIS_SaaP.Tests.Models;

/// <summary>
/// Unit tests for the Formula model
/// </summary>
public class FormulaTests
{
    [Fact]
    public void GenerateFormulaCode_WithTestNumber_ShouldGenerateCorrectCode()
    {
        // Arrange
        var formula = new Formula { TestNumber = 5, IsExternalDevelopment = false, IsWIP = false };

        // Act
        var code = formula.GenerateFormulaCode("CL001", "0001");

        // Assert
        code.Should().Be("CL001-0001-T05");
    }

    [Fact]
    public void GenerateFormulaCode_ExternalDevelopment_ShouldUseT00()
    {
        var formula = new Formula { TestNumber = 5, IsExternalDevelopment = true, IsWIP = false };

        var code = formula.GenerateFormulaCode("CL001", "0001");

        code.Should().Be("CL001-0001-T00");
    }

    [Fact]
    public void GenerateFormulaCode_WIP_ShouldAppendQuestionMark()
    {
        var formula = new Formula { TestNumber = 3, IsExternalDevelopment = false, IsWIP = true };

        var code = formula.GenerateFormulaCode("CL001", "0001");

        code.Should().Be("CL001-0001-T03?");
    }

    [Fact]
    public void CalculateTotalRawMaterialPercentage_ShouldSumActivePercentages()
    {
        var formula = new Formula();
        formula.FormulaRawMaterials = new List<FormulaRawMaterial>
        {
            new() { Percentage = 10, IsActive = true, RawMaterial = new RawMaterial { Code = "RM1", ProductName = "Test1", IsActive = true } },
            new() { Percentage = 25, IsActive = true, RawMaterial = new RawMaterial { Code = "RM2", ProductName = "Test2", IsActive = true } },
            new() { Percentage = 5, IsActive = false, RawMaterial = new RawMaterial { Code = "RM3", ProductName = "Test3", IsActive = true } } // Inactive
        };

        var result = formula.CalculateTotalRawMaterialPercentage();

        result.Should().Be(35);
    }

    [Fact]
    public void CalculateRequiredWaterPercentage_ShouldReturn100MinusTotal()
    {
        var formula = new Formula();
        formula.FormulaRawMaterials = new List<FormulaRawMaterial>
        {
            new() { Percentage = 30, IsActive = true, RawMaterial = new RawMaterial { Code = "RM1", ProductName = "Test1", IsActive = true } },
            new() { Percentage = 20, IsActive = true, RawMaterial = new RawMaterial { Code = "RM2", ProductName = "Test2", IsActive = true } }
        };

        var result = formula.CalculateRequiredWaterPercentage();

        result.Should().Be(50);
    }

    [Fact]
    public void CanBeAnhydrous_WhenTotalIs100_ShouldReturnTrue()
    {
        var formula = new Formula();
        formula.FormulaRawMaterials = new List<FormulaRawMaterial>
        {
            new() { Percentage = 60, IsActive = true, RawMaterial = new RawMaterial { Code = "RM1", ProductName = "Test1", IsActive = true } },
            new() { Percentage = 40, IsActive = true, RawMaterial = new RawMaterial { Code = "RM2", ProductName = "Test2", IsActive = true } }
        };

        formula.CanBeAnhydrous().Should().BeTrue();
    }

    [Fact]
    public void CanBeAnhydrous_WhenTotalIsLessThan100_ShouldReturnFalse()
    {
        var formula = new Formula();
        formula.FormulaRawMaterials = new List<FormulaRawMaterial>
        {
            new() { Percentage = 60, IsActive = true, RawMaterial = new RawMaterial { Code = "RM1", ProductName = "Test1", IsActive = true } }
        };

        formula.CanBeAnhydrous().Should().BeFalse();
    }

    [Fact]
    public void IsValidated_WhenValidationDateSet_ShouldReturnTrue()
    {
        var formula = new Formula { ValidationDate = DateTime.UtcNow };

        formula.IsValidated.Should().BeTrue();
    }

    [Fact]
    public void IsValidated_WhenValidationDateNull_ShouldReturnFalse()
    {
        var formula = new Formula { ValidationDate = null };

        formula.IsValidated.Should().BeFalse();
    }

    [Fact]
    public void IsRawMaterialPercentageValid_WhenUnder100_ShouldReturnTrue()
    {
        var formula = new Formula();
        formula.FormulaRawMaterials = new List<FormulaRawMaterial>
        {
            new() { Percentage = 50, IsActive = true, RawMaterial = new RawMaterial { Code = "RM1", ProductName = "T", IsActive = true } }
        };

        formula.IsRawMaterialPercentageValid().Should().BeTrue();
    }
}
