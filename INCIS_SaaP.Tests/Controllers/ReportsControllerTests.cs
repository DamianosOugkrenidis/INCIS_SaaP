using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Controllers;

/// <summary>
/// Unit tests for the Reports controller
/// </summary>
public class ReportsControllerTests : IDisposable
{
    private readonly Data.INCISDbContext _context;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _context = TestDbContextFactory.Create();

        IDistributedCache distributedCache =
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cache = new CacheService(distributedCache, NullLogger<CacheService>.Instance);

        _controller = new ReportsController(_context, cache);
    }

    [Fact]
    public async Task Index_ShouldReturnViewResult()
    {
        // Arrange
        _context.Clients.Add(new Client { Name = "C1" });
        _context.Clients.Add(new Client { Name = "C2" });
        _context.Suppliers.Add(new Supplier { Name = "S1" });
        _context.INCIs.Add(new INCI { Name = "INCI1" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task ExportClients_ShouldReturnExcelFile()
    {
        _context.Clients.Add(new Client { Name = "Test Client", Email = "t@t.com" });
        await _context.SaveChangesAsync();

        var result = await _controller.ExportClients() as FileContentResult;

        result.Should().NotBeNull();
        result!.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.FileDownloadName.Should().StartWith("Clients_Report_");
    }

    [Fact]
    public async Task ExportSuppliers_ShouldReturnExcelFile()
    {
        _context.Suppliers.Add(new Supplier { Name = "Test Supplier" });
        await _context.SaveChangesAsync();

        var result = await _controller.ExportSuppliers() as FileContentResult;

        result.Should().NotBeNull();
        result!.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task ExportFormulas_ShouldReturnExcelFile()
    {
        _context.Formulas.Add(new Formula { FormulaCode = "TEST-0001-T01", ProductName = "Test" });
        await _context.SaveChangesAsync();

        var result = await _controller.ExportFormulas() as FileContentResult;

        result.Should().NotBeNull();
        result!.FileDownloadName.Should().StartWith("Formulas_Report_");
    }

    [Fact]
    public async Task ExportUsedRawMaterials_ShouldOnlyIncludeUsedOnes()
    {
        var rm1 = new RawMaterial { Code = "RM1", ProductName = "Used Material" };
        var rm2 = new RawMaterial { Code = "RM2", ProductName = "Unused Material" };
        _context.RawMaterials.AddRange(rm1, rm2);
        await _context.SaveChangesAsync();

        var formula = new Formula { FormulaCode = "F001-0001-T01" };
        _context.Formulas.Add(formula);
        await _context.SaveChangesAsync();

        _context.FormulaRawMaterials.Add(new FormulaRawMaterial
        {
            FormulaId = formula.Id,
            RawMaterialId = rm1.Id,
            Percentage = 10,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var result = await _controller.ExportUsedRawMaterials() as FileContentResult;

        result.Should().NotBeNull();
        result!.FileDownloadName.Should().StartWith("UsedRawMaterials_Report_");
    }

    [Fact]
    public async Task ExportUnusedRawMaterials_ShouldOnlyIncludeUnusedOnes()
    {
        var rm = new RawMaterial { Code = "RM1", ProductName = "Unused" };
        _context.RawMaterials.Add(rm);
        await _context.SaveChangesAsync();

        var result = await _controller.ExportUnusedRawMaterials() as FileContentResult;

        result.Should().NotBeNull();
        result!.FileDownloadName.Should().StartWith("UnusedRawMaterials_Report_");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
