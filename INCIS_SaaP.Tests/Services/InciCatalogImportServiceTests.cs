using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using INCIS_SaaP.Dtos;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>Tests for the ALL_INCIS_NEW_FILE catalog importer.</summary>
public class InciCatalogImportServiceTests : IDisposable
{
    private readonly Data.INCISDbContext _context;
    private readonly InciCatalogImportService _service;

    public InciCatalogImportServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _service = new InciCatalogImportService(_context, NullLogger<InciCatalogImportService>.Instance);
    }

    private static string[] DefaultHeaders => new[]
    {
        "a/n", "Name", "Origin", "Cas", "Einecs", "Tox. Profile", "Restriction", "Functions"
    };

    private static Stream MakeXlsx(string[] headers, params string[][] rows)
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("printing");
        ws.Cell(1, 1).Value = "INCI list"; // τίτλος, όπως στο πραγματικό αρχείο
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(3, i + 1).Value = headers[i];
        for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
                ws.Cell(r + 4, c + 1).Value = rows[r][c];
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task Import_NewRows_MapsAllFields()
    {
        using var stream = MakeXlsx(DefaultHeaders,
            new[] { "1", "AQUA", "None", "7732-18-5", "231-791-2", "some tox", "III/15a", "SOLVENT" });

        var result = await _service.ImportAsync(stream, "ALL_INCIS_NEW_FILE.xls");

        result.Success.Should().BeTrue();
        result.Inserted.Should().Be(1);
        result.Updated.Should().Be(0);
        result.DataSource.Should().Be(InciCatalogImportService.DataSourceName);

        var inci = _context.INCIs.Single();
        inci.Name.Should().Be("AQUA");
        inci.CASNumber.Should().Be("7732-18-5");
        inci.ECNumber.Should().Be("231-791-2");
        inci.Function.Should().Be("SOLVENT");
        inci.Restriction.Should().Be("III/15a");
        inci.ToxicologicalProfile.Should().Be("some tox");
        inci.Origin.Should().BeNull();
        inci.DataSource.Should().Be(InciCatalogImportService.DataSourceName);
        inci.ImportedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Import_IsIdempotent_UpdatesExistingByName()
    {
        using (var s1 = MakeXlsx(DefaultHeaders,
            new[] { "1", "GLYCERIN", "None", "56-81-5", "200-289-5", "", "", "HUMECTANT" }))
        {
            await _service.ImportAsync(s1, "a.xls");
        }

        using var s2 = MakeXlsx(DefaultHeaders,
            new[] { "1", "GLYCERIN", "None", "56-81-5", "200-289-5", "tox", "II/1", "SOLVENT" });
        var result = await _service.ImportAsync(s2, "b.xls");

        result.Inserted.Should().Be(0);
        result.Updated.Should().Be(1);
        _context.INCIs.Count().Should().Be(1);
        _context.INCIs.Single().Function.Should().Be("SOLVENT");
        _context.INCIs.Single().Restriction.Should().Be("II/1");
    }

    [Fact]
    public async Task Import_TreatsPlaceholdersAsNull()
    {
        using var stream = MakeXlsx(DefaultHeaders,
            new[] { "1", "X", "None", "0", "-", "", "-", "SOLVENT" });

        await _service.ImportAsync(stream, "a.xls");

        var inci = _context.INCIs.Single();
        inci.CASNumber.Should().BeNull();
        inci.ECNumber.Should().BeNull();
        inci.Origin.Should().BeNull();
        inci.Function.Should().Be("SOLVENT");
    }

    [Fact]
    public async Task Import_DeduplicatesByName_KeepsFirst()
    {
        using var stream = MakeXlsx(DefaultHeaders,
            new[] { "1", "DUP", "None", "1", "", "", "", "FIRST" },
            new[] { "2", "DUP", "None", "2", "", "", "", "SECOND" });

        var result = await _service.ImportAsync(stream, "a.xls");

        result.Inserted.Should().Be(1);
        result.DuplicateRows.Should().Be(1);
        _context.INCIs.Single().Function.Should().Be("FIRST");
    }

    [Fact]
    public async Task Import_SkipsRowsWithoutName()
    {
        using var stream = MakeXlsx(DefaultHeaders,
            new[] { "1", "", "None", "1", "", "", "", "SOLVENT" },
            new[] { "2", "OK", "None", "2", "", "", "", "SOLVENT" });

        var result = await _service.ImportAsync(stream, "a.xls");

        result.SkippedEmptyName.Should().Be(1);
        result.Inserted.Should().Be(1);
    }

    [Fact]
    public async Task Import_MissingNameColumn_ReturnsFatalError()
    {
        using var stream = MakeXlsx(new[] { "a/n", "Cas", "Functions" },
            new[] { "1", "7732-18-5", "SOLVENT" });

        var result = await _service.ImportAsync(stream, "a.xls");

        result.Success.Should().BeFalse();
        result.FatalError.Should().NotBeNull();
        result.Inserted.Should().Be(0);
    }

    [Fact]
    public async Task Import_LongName_KeepsFullNameWithoutLoss()
    {
        var full = new string('A', 500) + "TAIL-UNIQUE";
        using var stream = MakeXlsx(DefaultHeaders,
            new[] { "1", full, "None", "1", "", "", "", "SOLVENT" });

        var result = await _service.ImportAsync(stream, "a.xls");

        result.TruncatedFields.Should().BeGreaterThan(0);
        var inci = _context.INCIs.Single();
        inci.Name.Length.Should().Be(500);      // ευρετηριοποιήσιμο κλειδί
        inci.FullName.Should().Be(full);        // πλήρες όνομα χωρίς απώλεια
    }

    [Fact]
    public async Task Import_TwoLongNamesSharingPrefix_AreBothImported()
    {
        var prefix = new string('X', 500);
        var n1 = prefix + "ONE";
        var n2 = prefix + "TWO";

        using var stream = MakeXlsx(DefaultHeaders,
            new[] { "1", n1, "None", "1", "", "", "", "SOLVENT" },
            new[] { "2", n2, "None", "2", "", "", "", "SOLVENT" });

        var result = await _service.ImportAsync(stream, "a.xls");

        result.Inserted.Should().Be(2);
        result.DuplicateRows.Should().Be(0);
        _context.INCIs.Count().Should().Be(2);
        _context.INCIs.Select(i => i.FullName).Should().Contain(new[] { n1, n2 });
    }

    [Fact]
    public async Task Import_LongNames_IsIdempotent()
    {
        var full = new string('A', 700) + "TAIL";

        using (var s1 = MakeXlsx(DefaultHeaders, new[] { "1", full, "None", "1", "", "", "", "SOLVENT" }))
        {
            await _service.ImportAsync(s1, "a.xls");
        }
        using var s2 = MakeXlsx(DefaultHeaders, new[] { "1", full, "None", "1", "", "", "", "SOLVENT" });
        var result = await _service.ImportAsync(s2, "b.xls");

        result.Inserted.Should().Be(0);
        result.Updated.Should().Be(1);
        _context.INCIs.Count().Should().Be(1);
    }

    public void Dispose() => _context.Dispose();
}
