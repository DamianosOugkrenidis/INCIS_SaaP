using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Controllers;

/// <summary>
/// Unit tests for RestrictionsController CRUD actions using InMemory DbContext.
/// </summary>
public class RestrictionsControllerTests : IDisposable
{
    private readonly INCISDbContext _context;
    private readonly RestrictionsController _controller;

    public RestrictionsControllerTests()
    {
        _context = TestDbContextFactory.Create();
        _controller = new RestrictionsController(_context);
    }

    private async Task<INCI> SeedInci(string name = "Phenoxyethanol")
    {
        var i = new INCI { Name = name, IsActive = true };
        _context.INCIs.Add(i);
        await _context.SaveChangesAsync();
        return i;
    }

    private async Task<Restriction> Seed(int inciId, string regulation = "EU 1223/2009", decimal? max = 1.0m)
    {
        var r = new Restriction { INCIId = inciId, Regulation = regulation, MaxPercentage = max };
        _context.Restrictions.Add(r);
        await _context.SaveChangesAsync();
        return r;
    }

    [Fact]
    public async Task Index_ReturnsView()
    {
        var inci = await SeedInci();
        await Seed(inci.Id);
        (await _controller.Index(null!, 1)).Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Index_WithSearch_ReturnsView()
    {
        var inci = await SeedInci();
        await Seed(inci.Id, "EU 1223/2009");
        await Seed(inci.Id, "FDA");

        (await _controller.Index("FDA", 1)).Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Details_NullId_ReturnsNotFound()
    {
        (await _controller.Details(null)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Details_ExistingId_ReturnsViewWithModel()
    {
        var inci = await SeedInci();
        var r = await Seed(inci.Id);

        var result = await _controller.Details(r.Id) as ViewResult;

        result.Should().NotBeNull();
        ((Restriction)result!.Model!).Id.Should().Be(r.Id);
    }

    [Fact]
    public void Create_Get_ReturnsView_WithInciList()
    {
        var result = _controller.Create() as ViewResult;

        result.Should().NotBeNull();
        object? inciList = _controller.ViewBag.INCIList;
        inciList.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_Post_Valid_RedirectsToIndex_AndPersists()
    {
        var inci = await SeedInci();
        var r = new Restriction { INCIId = inci.Id, Regulation = "EU 1223/2009", MaxPercentage = 0.5m };

        var result = await _controller.Create(r);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
        _context.Restrictions.Should().ContainSingle(x => x.INCIId == inci.Id);
    }

    [Fact]
    public async Task Create_Post_InvalidModel_ReturnsView_NoPersist()
    {
        await SeedInci();
        _controller.ModelState.AddModelError("Regulation", "Required");

        var result = await _controller.Create(new Restriction());

        result.Should().BeOfType<ViewResult>();
        _context.Restrictions.Should().BeEmpty();
    }

    [Fact]
    public async Task Edit_Get_ExistingId_ReturnsView()
    {
        var inci = await SeedInci();
        var r = await Seed(inci.Id);

        (await _controller.Edit((int?)r.Id)).Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Edit_Post_IdMismatch_ReturnsNotFound()
    {
        (await _controller.Edit(999, new Restriction { Id = 1 }))
            .Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Post_Valid_RedirectsAndUpdates()
    {
        var inci = await SeedInci();
        var r = await Seed(inci.Id, "EU 1223/2009");
        r.Regulation = "Updated Regulation";

        var result = await _controller.Edit(r.Id, r);

        result.Should().BeOfType<RedirectToActionResult>();
        _context.Restrictions.First(x => x.Id == r.Id).Regulation.Should().Be("Updated Regulation");
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesEntity_AndRedirects()
    {
        var inci = await SeedInci();
        var r = await Seed(inci.Id);

        var result = await _controller.DeleteConfirmed(r.Id);

        result.Should().BeOfType<RedirectToActionResult>();
        _context.Restrictions.Should().NotContain(x => x.Id == r.Id);
    }

    public void Dispose() => _context.Dispose();
}
