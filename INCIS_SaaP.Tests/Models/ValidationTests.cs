using FluentAssertions;
using FluentValidation.TestHelper;
using INCIS_SaaP.Infrastructure.Validators;
using INCIS_SaaP.Models;
using Xunit;

namespace INCIS_SaaP.Tests.Models;

/// <summary>
/// Tests for FluentValidation validators
/// </summary>
public class ValidationTests
{
    private readonly FormulaValidator _formulaValidator = new();
    private readonly ClientValidator _clientValidator = new();
    private readonly SupplierValidator _supplierValidator = new();
    private readonly RawMaterialValidator _rawMaterialValidator = new();
    private readonly INCIValidator _inciValidator = new();
    private readonly ProductionBatchValidator _batchValidator = new();

    // Formula validation
    [Fact]
    public void FormulaValidator_EmptyCode_ShouldFail()
    {
        var formula = new Formula { FormulaCode = "" };
        var result = _formulaValidator.TestValidate(formula);
        result.ShouldHaveValidationErrorFor(x => x.FormulaCode);
    }

    [Fact]
    public void FormulaValidator_ValidFormula_ShouldPass()
    {
        var formula = new Formula { FormulaCode = "CL001-0001-T01", ProductType = "Leave on" };
        var result = _formulaValidator.TestValidate(formula);
        result.ShouldNotHaveValidationErrorFor(x => x.FormulaCode);
        result.ShouldNotHaveValidationErrorFor(x => x.ProductType);
    }

    [Fact]
    public void FormulaValidator_InvalidProductType_ShouldFail()
    {
        var formula = new Formula { FormulaCode = "TEST", ProductType = "InvalidType" };
        var result = _formulaValidator.TestValidate(formula);
        result.ShouldHaveValidationErrorFor(x => x.ProductType);
    }

    // Client validation
    [Fact]
    public void ClientValidator_EmptyName_ShouldFail()
    {
        var client = new Client { Name = "" };
        var result = _clientValidator.TestValidate(client);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ClientValidator_InvalidEmail_ShouldFail()
    {
        var client = new Client { Name = "Test", Email = "not-an-email" };
        var result = _clientValidator.TestValidate(client);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ClientValidator_ValidClient_ShouldPass()
    {
        var client = new Client { Name = "Test Client", Email = "test@example.com" };
        var result = _clientValidator.TestValidate(client);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // Supplier validation
    [Fact]
    public void SupplierValidator_EmptyName_ShouldFail()
    {
        var supplier = new Supplier { Name = "" };
        var result = _supplierValidator.TestValidate(supplier);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    // RawMaterial validation
    [Fact]
    public void RawMaterialValidator_EmptyCode_ShouldFail()
    {
        var rm = new RawMaterial { Code = "", ProductName = "Test" };
        var result = _rawMaterialValidator.TestValidate(rm);
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void RawMaterialValidator_NegativePrice_ShouldFail()
    {
        var rm = new RawMaterial { Code = "RM001", ProductName = "Test", Price = -5 };
        var result = _rawMaterialValidator.TestValidate(rm);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void RawMaterialValidator_NaturalPercentageOver100_ShouldFail()
    {
        var rm = new RawMaterial { Code = "RM001", ProductName = "Test", NaturalOriginPercentage = 110 };
        var result = _rawMaterialValidator.TestValidate(rm);
        result.ShouldHaveValidationErrorFor(x => x.NaturalOriginPercentage);
    }

    // INCI validation
    [Fact]
    public void INCIValidator_EmptyName_ShouldFail()
    {
        var inci = new INCI { Name = "" };
        var result = _inciValidator.TestValidate(inci);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    // ProductionBatch validation
    [Fact]
    public void BatchValidator_ZeroBatchSize_ShouldFail()
    {
        var batch = new ProductionBatch { BatchSize = 0, Unit = "kg", FormulaId = 1 };
        var result = _batchValidator.TestValidate(batch);
        result.ShouldHaveValidationErrorFor(x => x.BatchSize);
    }

    [Fact]
    public void BatchValidator_InvalidUnit_ShouldFail()
    {
        var batch = new ProductionBatch { BatchSize = 50, Unit = "tons", FormulaId = 1 };
        var result = _batchValidator.TestValidate(batch);
        result.ShouldHaveValidationErrorFor(x => x.Unit);
    }

    [Fact]
    public void BatchValidator_ValidBatch_ShouldPass()
    {
        var batch = new ProductionBatch { BatchSize = 50, Unit = "kg", FormulaId = 1 };
        var result = _batchValidator.TestValidate(batch);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
