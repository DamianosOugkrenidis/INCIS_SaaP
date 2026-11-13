using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Controllers;

/// <summary>
/// Unit tests for ClientsController CRUD actions using InMemory DbContext.
/// </summary>
public class ClientsControllerTests : IDisposable
{
    private readonly INCISDbContext _context;
    private readonly ClientsController _controller;

    public ClientsControllerTests()
    {
        _context = TestDbContextFactory.Create();
        _controller = new ClientsController(_context);
    }

    private async Task<Client> Seed(string name = "Acme", string? code = "C1", string? email = "a@acme.gr")
    {
        var c = new Client { Name = name, Code = code, Email = email };
        _context.Clients.Add(c);
        await _context.SaveChangesAsync();
        return c;
    }

    [Fact]
    public async Task Index_ReturnsView()
    {
        await Seed();
        (await _controller.Index(null!, 1)).Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Index_WithSearch_DoesNotThrow_ReturnsView()
    {
        await Seed("Alpha", "AAA", "alpha@x.gr");
        await Seed("Beta", "BBB", "beta@x.gr");

        (await _controller.Index("Alpha", 1)).Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Details_NullId_ReturnsNotFound()
    {
        (await _controller.Details(null)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Details_UnknownId_ReturnsNotFound()
    {
        (await _controller.Details(9999)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Details_ExistingId_ReturnsViewWithModel()
    {
        var c = await Seed();
        var result = await _controller.Details(c.Id) as ViewResult;

        result.Should().NotBeNull();
        ((Client)result!.Model!).Id.Should().Be(c.Id);
    }

    [Fact]
    public void Create_Get_ReturnsView()
    {
        _controller.Create().Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Create_Post_Valid_RedirectsToIndex_AndPersists()
    {
        var c = new Client { Name = "NewClient" };

        var result = await _controller.Create(c);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
        _context.Clients.Should().ContainSingle(x => x.Name == "NewClient");
    }

    [Fact]
    public async Task Create_Post_InvalidModel_ReturnsViewWithModel()
    {
        _controller.ModelState.AddModelError("Name", "Required");
        var c = new Client();

        var result = await _controller.Create(c);

        result.Should().BeOfType<ViewResult>();
        _context.Clients.Should().BeEmpty();
    }

    [Fact]
    public async Task Edit_Get_NullId_ReturnsNotFound()
    {
        (await _controller.Edit((int?)null)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Get_ExistingId_ReturnsView()
    {
        var c = await Seed();
        (await _controller.Edit((int?)c.Id)).Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Edit_Post_IdMismatch_ReturnsNotFound()
    {
        var c = new Client { Id = 3, Name = "X" };
        (await _controller.Edit(999, c)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Post_Valid_RedirectsToIndex()
    {
        var c = await Seed();
        c.Name = "Renamed";

        var result = await _controller.Edit(c.Id, c);

        result.Should().BeOfType<RedirectToActionResult>();
        _context.Clients.First(x => x.Id == c.Id).Name.Should().Be("Renamed");
    }

    [Fact]
    public async Task Delete_Get_UnknownId_ReturnsNotFound()
    {
        (await _controller.Delete(4242)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesEntity_AndRedirects()
    {
        var c = await Seed();

        var result = await _controller.DeleteConfirmed(c.Id);

        result.Should().BeOfType<RedirectToActionResult>();
        _context.Clients.Should().NotContain(x => x.Id == c.Id);
    }

    public void Dispose() => _context.Dispose();
}
