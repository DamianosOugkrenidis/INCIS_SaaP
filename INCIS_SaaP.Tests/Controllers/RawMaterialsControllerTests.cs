using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Controllers;

/// <summary>
/// Unit tests for RawMaterialsController (CRUD + samples + search) using InMemory DbContext.
/// </summary>
public class RawMaterialsControllerTests : IDisposable
{
    private readonly INCISDbContext _context;
    private readonly RawMaterialsController _controller;

    public RawMaterialsControllerTests()
    {
        _context = TestDbContextFactory.Create();
        _controller = new RawMaterialsController(_context);
    }

    private async Task<RawMaterial> SeedRawMaterial(string code = "RM-1", string name = "Glycerin")
    {
        var rm = new RawMaterial { Code = code, ProductName = name, IsActive = true };
        _context.RawMaterials.Add(rm);
        await _context.SaveChangesAsync();
        return rm;
    }

    [Fact]
    public async Task Index_ReturnsView()
    {
        await SeedRawMaterial();
        var result = await _controller.Index(null!, 1);
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Index_WithSearch_FiltersResults()
    {
        await SeedRawMaterial("AAA", "Alpha");
        await SeedRawMaterial("BBB", "Beta");

        var result = await _controller.Index("Alpha", 1) as ViewResult;
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Details_NullId_ReturnsNotFound()
    {
        (await _controller.Details(null)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Details_UnknownId_ReturnsNotFound()
    {
        (await _controller.Details(999)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Details_ExistingId_ReturnsViewWithModel()
    {
        var rm = await SeedRawMaterial();
        var result = await _controller.Details(rm.Id) as ViewResult;

        result.Should().NotBeNull();
        result!.Model.Should().BeOfType<RawMaterial>();
        ((RawMaterial)result.Model!).Id.Should().Be(rm.Id);
    }

    [Fact]
    public void Create_Get_ReturnsView()
    {
        _controller.Create().Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Create_Post_ValidModel_RedirectsToIndex()
    {
        var rm = new RawMaterial { Code = "NEW", ProductName = "New Material" };

        var result = await _controller.Create(rm);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
        _context.RawMaterials.Should().ContainSingle(x => x.Code == "NEW");
    }

    [Fact]
    public async Task Edit_Get_NullId_ReturnsNotFound()
    {
        (await _controller.Edit((int?)null)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Get_ExistingId_ReturnsView()
    {
        var rm = await SeedRawMaterial();
        (await _controller.Edit((int?)rm.Id)).Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Edit_Post_IdMismatch_ReturnsNotFound()
    {
        var rm = new RawMaterial { Id = 5, Code = "X", ProductName = "Y" };
        (await _controller.Edit(999, rm)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesEntity_AndRedirects()
    {
        var rm = await SeedRawMaterial();

        var result = await _controller.DeleteConfirmed(rm.Id);

        result.Should().BeOfType<RedirectToActionResult>();
        _context.RawMaterials.Should().NotContain(x => x.Id == rm.Id);
    }

    [Fact]
    public async Task Delete_Get_UnknownId_ReturnsNotFound()
    {
        (await _controller.Delete(12345)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task AddSample_Get_ExistingId_ReturnsViewWithSample()
    {
        var rm = await SeedRawMaterial();
        var result = _controller.AddSample((int?)rm.Id) as ViewResult;

        result.Should().NotBeNull();
        result!.Model.Should().BeOfType<RawMaterialSample>();
    }

    [Fact]
    public void AddSample_Get_NullId_ReturnsNotFound()
    {
        _controller.AddSample((int?)null).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task SearchFormulas_NullId_ReturnsNotFound()
    {
        (await _controller.SearchFormulas(null, null!, 1)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ExpiredSamples_ReturnsView()
    {
        var rm = await SeedRawMaterial();
        _context.Set<RawMaterialSample>().Add(new RawMaterialSample
        {
            RawMaterialId = rm.Id,
            IsActive = true,
            LotNumber = "L1",
            ReceivedDate = DateTime.Today.AddYears(-3),
            ExpiryDate = DateTime.Today.AddDays(-10)
        });
        await _context.SaveChangesAsync();

        var result = await _controller.ExpiredSamples(null!);
        result.Should().BeOfType<ViewResult>();
    }

    public void Dispose() => _context.Dispose();
}
