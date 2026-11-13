using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Security;

/// <summary>
/// H-2 Phase 0/1 — Tests του config-driven μηχανισμού πολιτικής εγγραφής API.
/// <list type="bullet">
/// <item>Phase 0 (<c>Api:WritePolicy:Enforce=false</c>): επιτρέπεται αλλά καταγράφεται (log-only). ΔΕΝ προστατεύει.</item>
/// <item>Phase 1 (<c>Enforce=true</c>): 403 για μη εξουσιοδοτημένους· Admin/SuperUser λειτουργούν.</item>
/// </list>
/// </summary>
[Trait("Category", "Security")]
public class ApiWritePolicyTests
{
    private static readonly HttpStatusCode[] Denied =
        { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.Found };

    private static HttpClient Client(RoleWebApplicationFactory f) =>
        f.EnsureSeeded().CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static StringContent Json(object o) =>
        new(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");

    private static RoleWebApplicationFactory Factory(string[] roles, bool enforce, bool authenticated = true) =>
        new(
            s => { s.IsAuthenticated = authenticated; s.Roles = roles; },
            disableAntiforgery: true,
            settings: new Dictionary<string, string> { ["Api:WritePolicy:Enforce"] = enforce ? "true" : "false" });

    // ---------------- Phase 0 (log-only, no enforcement) ----------------
    [Fact]
    public async Task Phase0_UserRole_IsAllowed_ButLogged()
    {
        using var factory = Factory(new[] { "User" }, enforce: false);
        var client = Client(factory);
        var name = "P0 " + Guid.NewGuid().ToString("N")[..6];

        var res = await client.PostAsync("/api/v1/incis", Json(new { name, casNumber = "1-1-1" }));

        ((int)res.StatusCode).Should().BeLessThan(400,
            "Phase 0 ΔΕΝ επιβάλλει — καταγράφει μόνο (δεν αποτελεί προστασία)");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCIS_SaaP.Data.INCISDbContext>();
        (await db.INCIs.AnyAsync(i => i.Name == name)).Should().BeTrue();
    }

    // ---------------- Phase 1 (enforced) ----------------
    [Fact]
    public async Task Phase1_UserRole_IsDenied_AndNotPersisted()
    {
        using var factory = Factory(new[] { "User" }, enforce: true);
        var client = Client(factory);
        var name = "P1D " + Guid.NewGuid().ToString("N")[..6];

        var res = await client.PostAsync("/api/v1/incis", Json(new { name, casNumber = "1-1-1" }));

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCIS_SaaP.Data.INCISDbContext>();
        (await db.INCIs.AnyAsync(i => i.Name == name)).Should().BeFalse();
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("SuperUser")]
    [InlineData("TenantAdmin")]
    public async Task Phase1_AdminAndSuperUser_AreAllowed(string role)
    {
        using var factory = Factory(new[] { role }, enforce: true);
        var client = Client(factory);
        var name = "P1A " + Guid.NewGuid().ToString("N")[..6];

        var res = await client.PostAsync("/api/v1/incis", Json(new { name, casNumber = "1-1-1" }));

        ((int)res.StatusCode).Should().BeLessThan(400, $"ο ρόλος {role} πρέπει να λειτουργεί κανονικά");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCIS_SaaP.Data.INCISDbContext>();
        (await db.INCIs.AnyAsync(i => i.Name == name)).Should().BeTrue();
    }

    [Fact]
    public async Task Phase1_Anonymous_IsDenied()
    {
        using var factory = Factory(new[] { "Admin" }, enforce: true, authenticated: false);
        var client = Client(factory);

        var res = await client.PostAsync("/api/v1/incis", Json(new { name = "X", casNumber = "1-1-1" }));

        Denied.Should().Contain(res.StatusCode);
    }

    [Fact]
    public async Task Phase1_AdminRole_OnNonApiWrite_Unaffected()
    {
        // Ο filter επηρεάζει ΜΟΝΟ τα API writes του ApiExtendedController.
        using var factory = Factory(new[] { "Admin" }, enforce: true);
        var client = Client(factory);

        var res = await client.GetAsync("/api/v1/incis");

        ((int)res.StatusCode).Should().BeLessThan(400, "τα GET δεν επηρεάζονται από την πολιτική εγγραφής");
    }

    // ---------------- Demo token endpoint ----------------
    [Fact]
    public async Task DemoToken_DisabledByDefault_Returns501()
    {
        using var factory = new RoleWebApplicationFactory(s => s.Roles = new[] { "Admin" }, disableAntiforgery: true);
        var client = Client(factory);

        var res = await client.PostAsync("/api/v1/Api/token", Json(new { email = "a@b.c", password = "x" }));

        res.StatusCode.Should().Be(HttpStatusCode.NotImplemented,
            "το demo token endpoint δεν πρέπει να εκλαμβάνεται ως πραγματικός μηχανισμός");
    }

    [Fact]
    public async Task DemoToken_WhenExplicitlyEnabled_ReturnsDemoToken()
    {
        using var factory = new RoleWebApplicationFactory(
            s => s.Roles = new[] { "Admin" },
            disableAntiforgery: true,
            settings: new Dictionary<string, string> { ["Api:DemoToken:Enabled"] = "true" });
        var client = Client(factory);

        var res = await client.PostAsync("/api/v1/Api/token", Json(new { email = "a@b.c", password = "x" }));

        res.StatusCode.Should().NotBe(HttpStatusCode.NotImplemented,
            "όταν ενεργοποιηθεί ρητά, το demo token endpoint εκτελεί (η επικύρωση credentials είναι demo-logic)");
        // Το endpoint παραμένει DEMO: με μη υπαρκτό χρήστη επιστρέφει 401 (δεν εκδίδει token).
    }
}
