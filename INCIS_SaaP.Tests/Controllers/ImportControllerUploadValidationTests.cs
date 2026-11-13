using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Dtos;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Controllers;

/// <summary>
/// Έλεγχοι επικύρωσης εισόδου των endpoints εισαγωγής καταλόγου INCI
/// (τύπος αρχείου, μέγεθος, κενό, ανύπαρκτη διαδρομή) — πραγματικός έλεγχος αποτελέσματος.
/// </summary>
public class ImportControllerUploadValidationTests : IDisposable
{
    private readonly Data.INCISDbContext _context;
    private readonly ImportController _controller;

    public ImportControllerUploadValidationTests()
    {
        _context = TestDbContextFactory.Create();
        var importService = new ImportService(_context, NullLogger<ImportService>.Instance);
        var catalogService = new InciCatalogImportService(_context, NullLogger<InciCatalogImportService>.Instance);

        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.ContentRootPath).Returns(Path.GetTempPath());

        _controller = new ImportController(
            importService,
            catalogService,
            new ConfigurationBuilder().Build(),
            env.Object,
            NullLogger<ImportController>.Instance);
    }

    private static IFormFile File(string name, byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "file", name);
    }

    private static InciCatalogImportResult ResultOf(IActionResult action) =>
        (InciCatalogImportResult)((JsonResult)action).Value!;

    [Fact]
    public async Task InciCatalog_NullFile_ReturnsFatalError()
    {
        var result = ResultOf(await _controller.InciCatalog(null));
        result.Success.Should().BeFalse();
        result.FatalError.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InciCatalog_WrongExtension_Rejected()
    {
        var result = ResultOf(await _controller.InciCatalog(File("data.txt", Encoding.UTF8.GetBytes("x"))));
        result.Success.Should().BeFalse();
        result.FatalError.Should().Contain("Excel");
    }

    [Fact]
    public async Task InciCatalog_OversizeFile_Rejected()
    {
        var big = new byte[11 * 1024 * 1024];
        var result = ResultOf(await _controller.InciCatalog(File("big.xlsx", big)));
        result.Success.Should().BeFalse();
        result.FatalError.Should().Contain("10 MB");
    }

    [Fact]
    public async Task InciCatalog_ValidWorkbook_Imports()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("printing");
        ws.Cell(3, 1).Value = "a/n"; ws.Cell(3, 2).Value = "Name"; ws.Cell(3, 3).Value = "Cas";
        ws.Cell(4, 1).Value = "1"; ws.Cell(4, 2).Value = "UPLOAD TEST INCI"; ws.Cell(4, 3).Value = "1234-5-6";
        using var ms = new MemoryStream();
        wb.SaveAs(ms);

        var result = ResultOf(await _controller.InciCatalog(File("ok.xlsx", ms.ToArray())));

        result.Success.Should().BeTrue();
        result.Inserted.Should().Be(1);
        _context.INCIs.Should().ContainSingle(i => i.Name == "UPLOAD TEST INCI");
    }

    [Fact]
    public async Task SeedInciCatalog_MissingSource_ReturnsFatalError()
    {
        var result = ResultOf(await _controller.SeedInciCatalog());
        result.Success.Should().BeFalse();
        result.FatalError.Should().NotBeNullOrWhiteSpace();
    }

    public void Dispose() => _context.Dispose();
}
