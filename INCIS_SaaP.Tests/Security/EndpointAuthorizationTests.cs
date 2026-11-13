using System.Net;
using FluentAssertions;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Security;

/// <summary>
/// Έλεγχοι αυθεντικοποίησης/εξουσιοδότησης σε επίπεδο HTTP pipeline:
/// anonymous πρόσβαση, πρόσβαση με ανεπαρκή ρόλο, και έλεγχος CSRF/antiforgery
/// στα endpoints εισαγωγής.
/// </summary>
[Trait("Category", "Security")]
public class EndpointAuthorizationTests
{
    private static readonly HttpStatusCode[] Denied = { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.Found };

    private static HttpClient Client(RoleWebApplicationFactory f) =>
        f.EnsureSeeded().CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    // ---------------- Anonymous ----------------
    [Theory]
    [InlineData("/Clients")]
    [InlineData("/INCIs")]
    [InlineData("/RawMaterials")]
    [InlineData("/Formulas")]
    [InlineData("/Suppliers")]
    [InlineData("/Reports")]
    [InlineData("/api/v1/Api/me")]
    [InlineData("/api/v1/feature-flags")]
    [InlineData("/api/v1/clients")]
    public async Task Anonymous_ProtectedEndpoints_AreDenied(string url)
    {
        using var factory = new RoleWebApplicationFactory(s => s.IsAuthenticated = false);
        var client = Client(factory);

        var res = await client.GetAsync(url);

        Denied.Should().Contain(res.StatusCode, $"το {url} δεν πρέπει να είναι προσβάσιμο χωρίς σύνδεση");
    }

    /// <summary>
    /// Intentionally public endpoints ([AllowAnonymous]) πρέπει να παραμένουν προσβάσιμα —
    /// ώστε να μην περιοριστεί κατά λάθος δημόσια λειτουργία.
    /// </summary>
    [Theory]
    [InlineData("/api/cosing/stats")]
    [InlineData("/api/v1/subscription/plans")]
    public async Task Anonymous_PublicEndpoints_RemainAccessible(string url)
    {
        using var factory = new RoleWebApplicationFactory(s => s.IsAuthenticated = false);
        var client = Client(factory);

        var res = await client.GetAsync(url);

        res.StatusCode.Should().Be(HttpStatusCode.OK, $"το {url} είναι δημόσιο ([AllowAnonymous]) και πρέπει να λειτουργεί");
    }

    // ---------------- Non-admin (insufficient role) ----------------
    [Theory]
    [InlineData("/Tools")]                       // [Authorize(Roles=Admin)]
    [InlineData("/Users")]                       // [Authorize(Roles=Admin,TenantAdmin)]
    [InlineData("/api/v1/feature-flags")]        // [Authorize(Roles=Admin)]
    [InlineData("/api/v1/webhooks")]             // [Authorize(Roles=Admin,SuperUser)]
    public async Task NonAdminUser_RoleRestrictedEndpoints_AreDenied(string url)
    {
        using var factory = new RoleWebApplicationFactory(s => s.Roles = new[] { "User" });
        var client = Client(factory);

        var res = await client.GetAsync(url);

        Denied.Should().Contain(res.StatusCode, $"με ρόλο 'User' το {url} πρέπει να απορρίπτεται");
    }

    // ---------------- Admin (control) ----------------
    [Theory]
    [InlineData("/Tools")]
    [InlineData("/api/v1/feature-flags")]
    [InlineData("/Clients")]
    public async Task Admin_AllowedEndpoints_ReturnSuccess(string url)
    {
        using var factory = new RoleWebApplicationFactory(s => s.Roles = new[] { "Admin" });
        var client = Client(factory);

        var res = await client.GetAsync(url);

        ((int)res.StatusCode).Should().BeLessThan(400, $"ο Admin πρέπει να έχει πρόσβαση στο {url}");
    }

    // ---------------- CSRF / antiforgery ----------------
    [Theory]
    [InlineData("/Import/InciCatalog")]
    [InlineData("/Import/SeedInciCatalog")]
    public async Task ImportEndpoints_WithoutAntiforgeryToken_AreRejected(string url)
    {
        using var factory = new RoleWebApplicationFactory(s => s.Roles = new[] { "Admin" });
        var client = Client(factory);

        var content = new MultipartFormDataContent();
        var res = await client.PostAsync(url, content);

        ((int)res.StatusCode).Should().Be(400, "POST χωρίς antiforgery token πρέπει να απορρίπτεται (400)");
    }

    [Theory]
    [InlineData("/Import/InciCatalog")]
    [InlineData("/Import/SeedInciCatalog")]
    public async Task ImportEndpoints_Anonymous_AreDenied(string url)
    {
        using var factory = new RoleWebApplicationFactory(s => s.IsAuthenticated = false);
        var client = Client(factory);

        var content = new MultipartFormDataContent();
        var res = await client.PostAsync(url, content);

        Denied.Should().Contain(res.StatusCode, $"το {url} δεν πρέπει να είναι προσβάσιμο χωρίς σύνδεση");
    }
}
