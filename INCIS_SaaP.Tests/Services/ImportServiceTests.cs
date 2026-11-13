using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using INCIS_SaaP.Dtos;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>Tests for bulk Excel import (Clients / Suppliers / INCIs).</summary>
public class ImportServiceTests : IDisposable
{
    private readonly Data.INCISDbContext _context;
    private readonly ImportService _service;

    public ImportServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _service = new ImportService(_context, NullLogger<ImportService>.Instance);
    }

    private static Stream MakeXlsx(string[] headers, params string[][] rows)
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
                ws.Cell(r + 2, c + 1).Value = rows[r][c];
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void GenerateTemplate_Clients_ProducesReadableWorkbookWithHeaders()
    {
        var bytes = _service.GenerateTemplate(ImportEntityType.Clients);
        bytes.Should().NotBeNullOrEmpty();

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();
        ws.Cell(1, 1).GetString().Should().Contain("Name");
    }

    [Fact]
    public async Task Preview_MissingRequiredHeader_ReturnsFatalError()
    {
        using var stream = MakeXlsx(new[] { "Code", "Email" }, new[] { "CL-1", "a@b.gr" });
        var result = await _service.ParseAndValidateAsync(ImportEntityType.Clients, stream, "f.xlsx");

        result.FatalError.Should().NotBeNull();
        result.CanImport.Should().BeFalse();
    }

    [Fact]
    public async Task Preview_Clients_CategorizesValidDuplicateAndError()
    {
        _context.Clients.Add(new Client { Name = "Existing SA" });
        await _context.SaveChangesAsync();

        using var stream = MakeXlsx(
            new[] { "Name", "Code", "Email" },
            new[] { "Fresh Client", "CL-1", "info@fresh.gr" }, // valid
            new[] { "Existing SA", "CL-2", "x@y.gr" },          // duplicate (in DB)
            new[] { "Dup Client", "CL-3", "a@a.gr" },           // valid (1st)
            new[] { "Dup Client", "CL-4", "b@b.gr" },           // duplicate (in file)
            new[] { "", "CL-5", "no@name.gr" }                  // error (missing name)
        );

        var result = await _service.ParseAndValidateAsync(ImportEntityType.Clients, stream, "clients.xlsx");

        result.FatalError.Should().BeNull();
        result.TotalRows.Should().Be(5);
        result.ValidRows.Should().Be(2);
        result.DuplicateRows.Should().Be(2);
        result.ErrorRows.Should().Be(1);
        result.CanImport.Should().BeTrue();
    }

    [Fact]
    public async Task Commit_Clients_ImportsOnlyValidRows()
    {
        using var stream = MakeXlsx(
            new[] { "Name", "Code" },
            new[] { "Alpha", "A" },
            new[] { "Beta", "B" },
            new[] { "", "C" } // invalid
        );

        var result = await _service.CommitAsync(ImportEntityType.Clients, stream, "c.xlsx");

        result.Success.Should().BeTrue();
        result.Imported.Should().Be(2);
        result.SkippedErrors.Should().Be(1);
        _context.Clients.Count().Should().Be(2);
    }

    [Fact]
    public async Task Commit_INCIs_DetectsDuplicateByCasNumber()
    {
        _context.INCIs.Add(new INCI { Name = "Water", CASNumber = "7732-18-5" });
        await _context.SaveChangesAsync();

        using var stream = MakeXlsx(
            new[] { "Name", "CASNumber" },
            new[] { "Aqua", "7732-18-5" },   // duplicate by CAS
            new[] { "Glycerin", "56-81-5" }  // new
        );

        var result = await _service.CommitAsync(ImportEntityType.INCIs, stream, "inci.xlsx");

        result.Imported.Should().Be(1);
        result.SkippedDuplicates.Should().Be(1);
        _context.INCIs.Count().Should().Be(2);
    }

    [Fact]
    public async Task Commit_Suppliers_PersistsFields()
    {
        using var stream = MakeXlsx(
            new[] { "Name", "AFM", "Email" },
            new[] { "Supplier X", "123456789", "sx@x.gr" }
        );

        var result = await _service.CommitAsync(ImportEntityType.Suppliers, stream, "s.xlsx");

        result.Imported.Should().Be(1);
        var saved = _context.Suppliers.Single();
        saved.Name.Should().Be("Supplier X");
        saved.AFM.Should().Be("123456789");
        saved.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Commit_RawMaterials_ResolvesSupplierByNameAndParsesPrice()
    {
        _context.Suppliers.Add(new Supplier { Name = "Ingredients Hellas" });
        await _context.SaveChangesAsync();
        var supplierId = _context.Suppliers.Single().Id;

        using var stream = MakeXlsx(
            new[] { "Code", "ProductName", "SupplierName", "Price" },
            new[] { "RM-1", "Glycerin", "Ingredients Hellas", "3,50" }, // supplier found, comma decimal
            new[] { "RM-2", "Unknown Mat", "Nonexistent Sup", "x" },     // supplier not found, bad price -> still imports
            new[] { "", "No Code", "Ingredients Hellas", "1" }           // error: missing Code
        );

        var result = await _service.CommitAsync(ImportEntityType.RawMaterials, stream, "rm.xlsx");

        result.Imported.Should().Be(2);
        result.SkippedErrors.Should().Be(1);

        var rm1 = _context.RawMaterials.Single(r => r.Code == "RM-1");
        rm1.SupplierId.Should().Be(supplierId);
        rm1.Price.Should().Be(3.50m);

        var rm2 = _context.RawMaterials.Single(r => r.Code == "RM-2");
        rm2.SupplierId.Should().BeNull();   // unknown supplier -> null
        rm2.Price.Should().BeNull();        // invalid price -> null
    }

    [Fact]
    public async Task Commit_Formulas_ResolvesClientByNameAndDedupsByCode()
    {
        _context.Clients.Add(new Client { Name = "ACME Cosmetics AE" });
        _context.Formulas.Add(new Formula { FormulaCode = "EXIST-0001-T01" });
        await _context.SaveChangesAsync();
        var clientId = _context.Clients.Single().Id;

        using var stream = MakeXlsx(
            new[] { "FormulaCode", "ProductName", "ClientName", "ProductType" },
            new[] { "NEW-0001-T01", "Face Cream", "ACME Cosmetics AE", "Leave on" }, // valid, client found
            new[] { "EXIST-0001-T01", "Dup", "ACME Cosmetics AE", "Leave on" }        // duplicate (in DB)
        );

        var result = await _service.CommitAsync(ImportEntityType.Formulas, stream, "f.xlsx");

        result.Imported.Should().Be(1);
        result.SkippedDuplicates.Should().Be(1);

        var f = _context.Formulas.Single(x => x.FormulaCode == "NEW-0001-T01");
        f.ClientId.Should().Be(clientId);
        f.ProductType.Should().Be("Leave on");
    }

    public void Dispose() => _context.Dispose();
}
