using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// Tests για το ExportService (Excel + PDF) — κρίσιμες εξαγωγές/αναφορές.
/// Ελέγχουν πραγματικό περιεχόμενο αρχείου και τη συμπεριφορά σε σφάλματα.
/// </summary>
public class ExportServiceBranchTests : IDisposable
{
    private readonly Data.INCISDbContext _context;
    private readonly ExportService _service;

    public ExportServiceBranchTests()
    {
        _context = TestDbContextFactory.Create();
        _service = new ExportService(_context, NullLogger<ExportService>.Instance);
    }

    private async Task<Formula> SeedFormulaAsync(int inciCount = 1)
    {
        var client = new Client { Name = "C" };
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        var formula = new Formula
        {
            FormulaCode = "EXP-0001-T01",
            FormulaName = "Export Formula",
            ProductType = "Leave on",
            Version = "1.0",
            ClientId = client.Id
        };
        _context.Formulas.Add(formula);
        await _context.SaveChangesAsync();

        for (int i = 0; i < inciCount; i++)
        {
            _context.INCIIngredients.Add(new INCIIngredient
            {
                FormulaId = formula.Id,
                INCIName = $"INCI-{i}",
                Function = "SOLVENT",
                Percentage = 10m + i
            });
        }
        await _context.SaveChangesAsync();
        return formula;
    }

    [Fact]
    public async Task ExportFormulasToExcel_ProducesReadableWorkbookWithData()
    {
        await SeedFormulaAsync();

        var bytes = await _service.ExportFormulasToExcelAsync();

        bytes.Should().NotBeNullOrEmpty();
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();
        ws.Cell(1, 1).GetString().Should().Be("Κωδικός");
        ws.Cell(2, 1).GetString().Should().Be("EXP-0001-T01");
    }

    [Fact]
    public async Task ExportFormulaToPdf_ReturnsValidPdfBytes()
    {
        var formula = await SeedFormulaAsync(inciCount: 2);

        var bytes = await _service.ExportFormulaToPdfAsync(formula.Id);

        bytes.Should().NotBeNullOrEmpty();
        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task ExportFormulaToPdf_MissingFormula_Throws()
    {
        await FluentActions
            .Awaiting(() => _service.ExportFormulaToPdfAsync(424242))
            .Should().ThrowAsync<ArgumentException>();
    }

    public void Dispose() => _context.Dispose();
}
