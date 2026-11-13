using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Integration;

/// <summary>Factory: Admin/SuperUser με απενεργοποιημένο antiforgery (η CSRF προστασία ελέγχεται ξεχωριστά).</summary>
public class MutatingFactory : RoleWebApplicationFactory
{
    public MutatingFactory()
        : base(s => s.Roles = new[] { "Admin", "SuperUser" }, disableAntiforgery: true) { }
}

/// <summary>
/// Integration tests μεταλλακτικών endpoints (Create/Edit/Delete) μέσω πραγματικού HTTP pipeline,
/// με επαλήθευση persistence στη βάση (καλύπτει το High κενό των POST actions).
/// </summary>
[Trait("Category", "Integration")]
public class MutatingEndpointTests : IClassFixture<MutatingFactory>
{
    private readonly MutatingFactory _factory;
    private readonly HttpClient _client;

    public MutatingEndpointTests(MutatingFactory factory)
    {
        _factory = factory;
        factory.EnsureSeeded();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private INCISDbContext Db()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<INCISDbContext>();
    }

    private static FormUrlEncodedContent Form(params (string k, string v)[] kv) =>
        new(kv.Select(x => new KeyValuePair<string, string>(x.k, x.v)));

    [Fact]
    public async Task CreateClient_PersistsAndRedirects()
    {
        var name = "POST CLIENT " + Guid.NewGuid().ToString("N")[..6];

        var res = await _client.PostAsync("/Clients/Create", Form(("Name", name), ("Code", "PC1")));

        res.StatusCode.Should().Be(HttpStatusCode.Found);
        res.Headers.Location!.ToString().Should().Contain("/Clients");

        await using var db = Db();
        (await db.Clients.AnyAsync(c => c.Name == name)).Should().BeTrue();
    }

    [Fact]
    public async Task CreateInci_PersistsAndRedirects()
    {
        var name = "POST INCI " + Guid.NewGuid().ToString("N")[..6];

        var res = await _client.PostAsync("/INCIs/Create",
            Form(("Name", name), ("CASNumber", "1111-22-3"), ("IsActive", "true")));

        res.StatusCode.Should().Be(HttpStatusCode.Found);
        await using var db = Db();
        (await db.INCIs.AnyAsync(i => i.Name == name)).Should().BeTrue();
    }

    [Fact]
    public async Task CreateInci_MissingName_ReturnsView_NoPersist()
    {
        await using var db = Db();
        var before = await db.INCIs.CountAsync();

        var res = await _client.PostAsync("/INCIs/Create", Form(("Name", ""), ("IsActive", "true")));

        res.StatusCode.Should().Be(HttpStatusCode.OK); // επιστρέφει το view με validation errors
        await using var db2 = Db();
        (await db2.INCIs.CountAsync()).Should().Be(before);
    }

    [Fact]
    public async Task EditInci_UpdatesAndRedirects()
    {
        var name = "EDIT INCI " + Guid.NewGuid().ToString("N")[..6];
        int id;
        await using (var db = Db())
        {
            var e = new INCI { Name = name, IsActive = true };
            db.INCIs.Add(e);
            await db.SaveChangesAsync();
            id = e.Id;
        }

        var res = await _client.PostAsync($"/INCIs/Edit/{id}",
            Form(("Id", id.ToString()), ("Name", name + " RENAMED"), ("IsActive", "true")));

        res.StatusCode.Should().Be(HttpStatusCode.Found);
        await using var db2 = Db();
        (await db2.INCIs.FindAsync(id))!.Name.Should().Be(name + " RENAMED");
    }

    [Fact]
    public async Task DeleteInci_RemovesAndRedirects()
    {
        int id;
        await using (var db = Db())
        {
            var e = new INCI { Name = "DEL INCI " + Guid.NewGuid().ToString("N")[..6], IsActive = true };
            db.INCIs.Add(e);
            await db.SaveChangesAsync();
            id = e.Id;
        }

        var res = await _client.PostAsync($"/INCIs/Delete/{id}", Form(("id", id.ToString())));

        res.StatusCode.Should().Be(HttpStatusCode.Found);
        await using var db2 = Db();
        (await db2.INCIs.FindAsync(id)).Should().BeNull();
    }

    [Fact]
    public async Task AnonymousPost_CreateInci_IsDenied()
    {
        using var anon = new RoleWebApplicationFactory(s => s.IsAuthenticated = false, disableAntiforgery: true);
        var client = anon.EnsureSeeded().CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var res = await client.PostAsync("/INCIs/Create", Form(("Name", "ANON"), ("IsActive", "true")));

        ((int)res.StatusCode).Should().BeOneOf(401, 302, 403);
    }
}
