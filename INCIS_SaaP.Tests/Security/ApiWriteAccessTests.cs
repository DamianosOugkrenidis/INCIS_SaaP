using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Security;

/// <summary>
/// H-2 — Tests πολιτικής πρόσβασης στα API write endpoints (ApiExtendedController).
/// ΔΕΝ αλλάζει συμπεριφορά· τεκμηριώνει την τρέχουσα κατάσταση και αποδεικνύει ότι
/// οι μη-συνδεδεμένοι χρήστες δεν μπορούν να γράψουν. Βλ. docs/H2_API_WRITE_ASSESSMENT.md.
/// </summary>
[Trait("Category", "Security")]
public class ApiWriteAccessTests
{
    private static HttpClient Client(RoleWebApplicationFactory f) =>
        f.EnsureSeeded().CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static readonly HttpStatusCode[] Denied =
        { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.Found };

    private static StringContent Json(object o) =>
        new(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");

    // ---------------- Anonymous ----------------
    [Theory]
    [InlineData("POST", "/api/v1/clients")]
    [InlineData("PUT", "/api/v1/clients/1")]
    [InlineData("DELETE", "/api/v1/clients/1")]
    [InlineData("POST", "/api/v1/incis")]
    [InlineData("PUT", "/api/v1/incis/1")]
    [InlineData("DELETE", "/api/v1/incis/1")]
    [InlineData("POST", "/api/v1/suppliers")]
    [InlineData("POST", "/api/v1/rawmaterials")]
    [InlineData("POST", "/api/v1/restrictions")]
    [InlineData("POST", "/api/v1/formulas/1/validate")]
    public async Task Anonymous_ApiWrites_AreDenied(string method, string url)
    {
        using var factory = new RoleWebApplicationFactory(s => s.IsAuthenticated = false, disableAntiforgery: true);
        var client = Client(factory);

        var req = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "DELETE" ? null : Json(new { name = "X", description = "X" })
        };
        var res = await client.SendAsync(req);

        Denied.Should().Contain(res.StatusCode, $"χωρίς σύνδεση το {method} {url} πρέπει να απορρίπτεται");
    }

    // ---------------- Inventory lock (documents H-2) ----------------
    [Fact]
    public void ApiExtended_WriteEndpoints_InventoryAndCurrentAuth()
    {
        var writes = typeof(ApiExtendedController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any())
            .Select(m => new
            {
                Method = m.GetCustomAttributes<HttpMethodAttribute>().First().HttpMethods.First(),
                Route = string.Join(",", m.GetCustomAttributes<HttpMethodAttribute>().First().Template),
                HasRole = m.GetCustomAttributes<AuthorizeAttribute>().Any(a => !string.IsNullOrWhiteSpace(a.Roles))
            })
            .Where(x => x.Method is "POST" or "PUT" or "DELETE")
            .ToList();

        writes.Should().HaveCount(23, "η απογραφή των write endpoints του ApiExtendedController");

        // ΤΕΚΜΗΡΙΩΣΗ H-2: όλα τα writes είναι σήμερα ΧΩΡΙΣ role restriction (μόνο authenticated).
        writes.Should().OnlyContain(w => !w.HasRole,
            "H-2: τα API writes δεν έχουν role restriction — αναμένεται αλλαγή όταν αποφασιστεί το patch");
    }

    // ---------------- Characterization (H-2 gap) ----------------
    /// <summary>
    /// CHARACTERIZATION: καταγράφει ότι ο ρόλος 'User' μπορεί ΣΗΜΕΡΑ να γράψει μέσω API (H-2).
    /// Όταν εφαρμοστεί ο περιορισμός, αυτό το test αντικαθίσταται από έλεγχο απόρριψης.
    /// </summary>
    [Fact]
    public async Task CHARACTERIZATION_UserRole_CanCreateInciViaApi_CurrentBehavior()
    {
        using var factory = new RoleWebApplicationFactory(s => s.Roles = new[] { "User" }, disableAntiforgery: true);
        var client = Client(factory);
        var name = "API H2 " + Guid.NewGuid().ToString("N")[..6];

        var res = await client.PostAsync("/api/v1/incis", Json(new { name, casNumber = "1-1-1" }));

        ((int)res.StatusCode).Should().BeLessThan(400,
            "H-2 ΤΡΕΧΟΥΣΑ συμπεριφορά: authenticated (και μη-Admin) χρήστης μπορεί να γράψει μέσω API");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCIS_SaaP.Data.INCISDbContext>();
        (await db.INCIs.AnyAsync(i => i.Name == name)).Should().BeTrue();
    }

    // ---------------- Phase 1 (enforce) — ΕΝΕΡΓΟΠΟΙΕΙΤΑΙ ΜΟΝΟ στο απομονωμένο test env ----------------
    // Το production default παραμένει Phase 0 (Enforce=false). Εδώ θέτουμε Enforce=true μέσω settings.
    private static RoleWebApplicationFactory EnforcedFactory(string[] roles, bool authenticated = true) =>
        new(
            s => { s.IsAuthenticated = authenticated; s.Roles = roles; },
            disableAntiforgery: true,
            settings: new Dictionary<string, string> { ["Api:WritePolicy:Enforce"] = "true" });

    [Fact]
    public async Task H2_TARGET_UserRole_Denied_OnApiWrite()
    {
        using var factory = EnforcedFactory(new[] { "User" });
        var client = Client(factory);
        var name = "H2P1 " + Guid.NewGuid().ToString("N")[..6];

        var res = await client.PostAsync("/api/v1/incis", Json(new { name, casNumber = "1-1-1" }));

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Phase 1: ο μη εξουσιοδοτημένος ρόλος απορρίπτεται με 403");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCIS_SaaP.Data.INCISDbContext>();
        (await db.INCIs.AnyAsync(i => i.Name == name)).Should().BeFalse("δεν πρέπει να αποθηκεύεται εγγραφή");
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("SuperUser")]
    public async Task H2_PHASE1_AuthorizedRoles_StillWork(string role)
    {
        using var factory = EnforcedFactory(new[] { role });
        var client = Client(factory);
        var name = "H2OK " + Guid.NewGuid().ToString("N")[..6];

        var res = await client.PostAsync("/api/v1/incis", Json(new { name, casNumber = "1-1-1" }));

        ((int)res.StatusCode).Should().BeLessThan(400, $"ο ρόλος {role} διατηρεί την προβλεπόμενη λειτουργικότητα");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCIS_SaaP.Data.INCISDbContext>();
        (await db.INCIs.AnyAsync(i => i.Name == name)).Should().BeTrue();
    }

    [Fact]
    public async Task H2_PHASE1_Anonymous_IsDenied()
    {
        using var factory = EnforcedFactory(new[] { "Admin" }, authenticated: false);
        var client = Client(factory);

        var res = await client.PostAsync("/api/v1/incis", Json(new { name = "X", casNumber = "1-1-1" }));

        Denied.Should().Contain(res.StatusCode);
    }
}
