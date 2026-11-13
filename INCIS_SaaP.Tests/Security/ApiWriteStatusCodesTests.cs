using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Security;

/// <summary>
/// H-2 — Ακριβείς κωδικοί HTTP (401 anonymous / 403 non-admin) και CSRF posture των API writes.
/// Τα API endpoints αυθεντικοποιούνται με **cookie** (δεν υπάρχει ενεργό JWT bearer).
/// </summary>
[Trait("Category", "Security")]
public class ApiWriteStatusCodesTests
{
    private static HttpClient Client(RoleWebApplicationFactory f) =>
        f.EnsureSeeded().CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static StringContent Json(object o) => new(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");

    [Fact]
    public async Task Anonymous_ApiWrite_Returns401()
    {
        using var factory = new RoleWebApplicationFactory(s => s.IsAuthenticated = false, disableAntiforgery: true);
        var client = Client(factory);

        var res = await client.PostAsync("/api/v1/incis", Json(new { name = "X", casNumber = "1-1-1" }));

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "ανώνυμο API write -> 401 (cookie scheme, χωρίς redirect για [ApiController])");
    }

    [Fact]
    public async Task Phase1_NonAdmin_ApiWrite_Returns403()
    {
        using var factory = new RoleWebApplicationFactory(
            s => { s.Roles = new[] { "User" }; },
            disableAntiforgery: true,
            settings: new Dictionary<string, string> { ["Api:WritePolicy:Enforce"] = "true" });
        var client = Client(factory);

        var res = await client.PostAsync("/api/v1/incis", Json(new { name = "X", casNumber = "1-1-1" }));

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Phase1_Admin_ApiWrite_Succeeds()
    {
        using var factory = new RoleWebApplicationFactory(
            s => { s.Roles = new[] { "Admin" }; },
            disableAntiforgery: true,
            settings: new Dictionary<string, string> { ["Api:WritePolicy:Enforce"] = "true" });
        var client = Client(factory);

        var res = await client.PostAsync("/api/v1/incis",
            Json(new { name = "STATUS " + Guid.NewGuid().ToString("N")[..6], casNumber = "1-1-1" }));

        ((int)res.StatusCode).Should().BeLessThan(400);
    }

    private static bool HasAntiforgery(MethodInfo m) =>
        m.GetCustomAttributes().Any(a => a.GetType().Name == "ValidateAntiForgeryTokenAttribute");

    /// <summary>
    /// ΤΕΚΜΗΡΙΩΣΗ CSRF posture: τα API write endpoints (cookie-authenticated) ΔΕΝ φέρουν
    /// [ValidateAntiForgeryToken] — αντίθετα με τα MVC POST actions του Import.
    /// Μετριασμός: cookies SameSite=Lax (Program.cs:461) -> δεν αποστέλλονται σε cross-site POST.
    /// </summary>
    [Fact]
    public void ApiWrites_DoNotUseAntiforgeryToken_MvcImportDoes()
    {
        var apiWrites = typeof(ApiExtendedController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes().Any(a => a.GetType().Name is "HttpPostAttribute" or "HttpPutAttribute" or "HttpDeleteAttribute"))
            .ToList();

        apiWrites.Should().NotBeEmpty();
        apiWrites.Should().OnlyContain(m => !HasAntiforgery(m), "τα API writes είναι API-style (χωρίς antiforgery token)");

        var importPost = typeof(ImportController)
            .GetMethod(nameof(ImportController.InciCatalog))!;

        HasAntiforgery(importPost).Should().BeTrue("τα MVC POST actions του Import χρησιμοποιούν αντι-CSRF token");
    }
}
