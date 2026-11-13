using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Controllers;

/// <summary>
/// Unit tests for SuppliersController CRUD actions using InMemory DbContext.
/// </summary>
public class SuppliersControllerTests : IDisposable
{
    private readonly INCISDbContext _context;
    private readonly SuppliersController _controller;

    public SuppliersControllerTests()
    {
        _context = TestDbContextFactory.Create();
        _controller = new SuppliersController(_context);
    }

    private async Task<Supplier> Seed(string name = "Ingredients Ltd", string? email = "s@ing.gr")
    {
        var s = new Supplier { Name = name, Email = email };
        _context.Suppliers.Add(s);
        await _context.SaveChangesAsync();
        return s;
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
        await Seed("Alpha Supplies", "alpha@x.gr");
        await Seed("Beta Supplies", "beta@x.gr");

        (await _controller.Index("Alpha", 1)).Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Details_NullId_ReturnsNotFound()
    {
        (await _controller.Details(null)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Details_ExistingId_ReturnsViewWithModel()
    {
        var s = await Seed();
        var result = await _controller.Details(s.Id) as ViewResult;

        result.Should().NotBeNull();
        ((Supplier)result!.Model!).Id.Should().Be(s.Id);
    }

    [Fact]
    public void Create_Get_ReturnsView()
    {
        _controller.Create().Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Create_Post_Valid_RedirectsToIndex_AndPersists()
    {
        var s = new Supplier { Name = "NewSupplier" };

        var result = await _controller.Create(s);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
        _context.Suppliers.Should().ContainSingle(x => x.Name == "NewSupplier");
    }

    [Fact]
    public async Task Create_Post_InvalidModel_ReturnsView_NoPersist()
    {
        _controller.ModelState.AddModelError("Name", "Required");

        var result = await _controller.Create(new Supplier());

        result.Should().BeOfType<ViewResult>();
        _context.Suppliers.Should().BeEmpty();
    }

    [Fact]
    public async Task Edit_Get_ExistingId_ReturnsView()
    {
        var s = await Seed();
        (await _controller.Edit((int?)s.Id)).Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Edit_Post_IdMismatch_ReturnsNotFound()
    {
        (await _controller.Edit(999, new Supplier { Id = 1, Name = "X" }))
            .Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Post_Valid_RedirectsAndUpdates()
    {
        var s = await Seed();
        s.Name = "Renamed Supplier";

        var result = await _controller.Edit(s.Id, s);

        result.Should().BeOfType<RedirectToActionResult>();
        _context.Suppliers.First(x => x.Id == s.Id).Name.Should().Be("Renamed Supplier");
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesEntity_AndRedirects()
    {
        var s = await Seed();

        var result = await _controller.DeleteConfirmed(s.Id);

        result.Should().BeOfType<RedirectToActionResult>();
        _context.Suppliers.Should().NotContain(x => x.Id == s.Id);
    }

    public void Dispose() => _context.Dispose();
}
