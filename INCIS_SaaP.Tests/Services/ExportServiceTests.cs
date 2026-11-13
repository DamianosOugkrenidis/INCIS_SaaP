using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>Unit tests for ExportService (Excel + PDF).</summary>
public class ExportServiceTests : IDisposable
{
    private readonly Data.INCISDbContext _context;
    private readonly ExportService _service;
    private int _formulaId;

    public ExportServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _service = new ExportService(_context, NullLogger<ExportService>.Instance);
        Seed();
    }

    private void Seed()
    {
        var supplier = new Supplier { Name = "Sup" };
        _context.Suppliers.Add(supplier);
        _context.SaveChanges();
        var rm = new RawMaterial { Code = "RM-1", ProductName = "Glycerin", SupplierId = supplier.Id };
        _context.RawMaterials.Add(rm);
        var formula = new Formula { FormulaCode = "EXP-0001-T01", ProductName = "Cream", ProductType = "Leave on" };
        _context.Formulas.Add(formula);
        _context.SaveChanges();
        _formulaId = formula.Id;
        _context.FormulaRawMaterials.Add(new FormulaRawMaterial { FormulaId = formula.Id, RawMaterialId = rm.Id, Percentage = 5, IsActive = true });
        _context.SaveChanges();
    }

    [Fact]
    public async Task ExportFormulasToExcel_ProducesNonEmptyWorkbook()
    {
        var bytes = await _service.ExportFormulasToExcelAsync();
        bytes.Should().NotBeNullOrEmpty();
        bytes.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public async Task ExportFormulaToPdf_ProducesNonEmptyPdf()
    {
        var bytes = await _service.ExportFormulaToPdfAsync(_formulaId);
        bytes.Should().NotBeNullOrEmpty();
        // PDF files start with "%PDF"
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    public void Dispose() => _context.Dispose();
}
