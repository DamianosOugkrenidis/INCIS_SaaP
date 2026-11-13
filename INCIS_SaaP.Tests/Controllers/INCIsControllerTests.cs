using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Controllers;

/// <summary>
/// Unit tests for INCIsController CRUD + AdvanceSearch using InMemory DbContext.
/// </summary>
public class INCIsControllerTests : IDisposable
{
    private readonly INCISDbContext _context;
    private readonly INCIsController _controller;

    public INCIsControllerTests()
    {
        _context = TestDbContextFactory.Create();
        _controller = new INCIsController(_context);
    }

    private async Task<INCI> Seed(string name = "Aqua", string? cas = "7732-18-5")
    {
        var i = new INCI { Name = name, CASNumber = cas };
        _context.INCIs.Add(i);
        await _context.SaveChangesAsync();
        return i;
    }

    [Fact]
    public async Task Index_ReturnsView()
    {
        await Seed();
        (await _controller.Index(null!, 1)).Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Index_WithSearch_ReturnsView()
    {
        await Seed("Glycerin", "56-81-5");
        await Seed("Aqua", "7732-18-5");

        (await _controller.Index("Glycerin", 1)).Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Details_NullId_ReturnsNotFound()
    {
        (await _controller.Details(null)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Details_ExistingId_ReturnsViewWithModel()
    {
        var i = await Seed();
        var result = await _controller.Details(i.Id) as ViewResult;

        result.Should().NotBeNull();
        ((INCI)result!.Model!).Id.Should().Be(i.Id);
    }

    [Fact]
    public void Create_Get_ReturnsView()
    {
        _controller.Create().Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Create_Post_Valid_RedirectsToIndex_AndPersists()
    {
        var i = new INCI { Name = "Niacinamide" };

        var result = await _controller.Create(i);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
        _context.INCIs.Should().ContainSingle(x => x.Name == "Niacinamide");
    }

    [Fact]
    public async Task Create_Post_InvalidModel_ReturnsView_NoPersist()
    {
        _controller.ModelState.AddModelError("Name", "Required");

        var result = await _controller.Create(new INCI());

        result.Should().BeOfType<ViewResult>();
        _context.INCIs.Should().BeEmpty();
    }

    [Fact]
    public async Task Edit_Get_ExistingId_ReturnsView()
    {
        var i = await Seed();
        (await _controller.Edit((int?)i.Id)).Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Edit_Post_IdMismatch_ReturnsNotFound()
    {
        (await _controller.Edit(999, new INCI { Id = 1, Name = "X" }))
            .Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Post_Valid_RedirectsAndUpdates()
    {
        var i = await Seed();
        i.Name = "Renamed INCI";

        var result = await _controller.Edit(i.Id, i);

        result.Should().BeOfType<RedirectToActionResult>();
        _context.INCIs.First(x => x.Id == i.Id).Name.Should().Be("Renamed INCI");
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesEntity_AndRedirects()
    {
        var i = await Seed();

        var result = await _controller.DeleteConfirmed(i.Id);

        result.Should().BeOfType<RedirectToActionResult>();
        _context.INCIs.Should().NotContain(x => x.Id == i.Id);
    }

    [Fact]
    public async Task AdvanceSearch_ReturnsView()
    {
        await Seed();
        (await _controller.AdvanceSearch(null!, 1)).Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Index_SearchByFullNameTail_FindsTruncatedNameRow()
    {
        var prefix = new string('X', 500);
        _context.INCIs.Add(new INCI { Name = prefix, FullName = prefix + "TAILMARKER" });
        await _context.SaveChangesAsync();

        var result = (ViewResult)await _controller.Index("TAILMARKER", 1);
        var list = (PaginatedList<INCI>)result.Model!;

        list.Should().ContainSingle(i => i.FullName != null && i.FullName.EndsWith("TAILMARKER"));
    }

    public void Dispose() => _context.Dispose();
}
