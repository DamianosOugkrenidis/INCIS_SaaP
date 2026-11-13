using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Security;

/// <summary>
/// Συνδυασμός CSRF + H-2: σειρά εφαρμογής (CSRF 400 πριν το policy 403), GET/HEAD ανεπηρέαστα,
/// invalid token, unauthenticated. Καμία αλλαγή παραγωγικών defaults (και τα δύο flags false).
/// </summary>
[Trait("Category", "Security")]
public class CsrfH2CombinationTests
{
    private static HttpClient Client(RoleWebApplicationFactory f) =>
        f.EnsureSeeded().CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static StringContent Json(object o) => new(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");

    private static RoleWebApplicationFactory Factory(
        bool csrf, bool policy, string[]? roles = null, bool authenticated = true, bool disableAntiforgery = false)
    {
        var settings = new Dictionary<string, string>
        {
            ["Api:Csrf:Enforce"] = csrf ? "true" : "false",
            ["Api:WritePolicy:Enforce"] = policy ? "true" : "false"
        };
        return new RoleWebApplicationFactory(
            s => { s.IsAuthenticated = authenticated; s.Roles = roles ?? new[] { "Admin" }; },
            disableAntiforgery: disableAntiforgery,
            settings: settings);
    }

    [Fact]
    public async Task BothEnforced_Admin_MissingCsrf_Returns400()
    {
        using var f = Factory(csrf: true, policy: true, roles: new[] { "Admin" });
        var res = await Client(f).PostAsync("/api/v1/incis", Json(new { name = "C1", casNumber = "1-1-1" }));

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest, "το CSRF (400) εφαρμόζεται πριν/ανεξάρτητα από το policy");
    }

    [Fact]
    public async Task BothEnforced_NonAdmin_MissingCsrf_Returns400_NotForbidden()
    {
        using var f = Factory(csrf: true, policy: true, roles: new[] { "User" });
        var res = await Client(f).PostAsync("/api/v1/incis", Json(new { name = "C2", casNumber = "1-1-1" }));

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest, "σειρά: CSRF filter πριν το write-policy filter");
    }

    [Fact]
    public async Task CsrfOff_PolicyOn_NonAdmin_Returns403()
    {
        using var f = Factory(csrf: false, policy: true, roles: new[] { "User" });
        var res = await Client(f).PostAsync("/api/v1/incis", Json(new { name = "C3", casNumber = "1-1-1" }));

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BothEnforced_ValidToken_Simulated_Admin_Allowed()
    {
        // NoOp antiforgery = προσομοίωση έγκυρου token.
        using var f = Factory(csrf: true, policy: true, roles: new[] { "Admin" }, disableAntiforgery: true);
        var res = await Client(f).PostAsync("/api/v1/incis",
            Json(new { name = "C4 " + Guid.NewGuid().ToString("N")[..6], casNumber = "1-1-1" }));

        ((int)res.StatusCode).Should().BeLessThan(400);
    }

    [Fact]
    public async Task BothEnforced_InvalidToken_Returns400()
    {
        using var f = Factory(csrf: true, policy: true, roles: new[] { "Admin" });
        var client = Client(f);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "definitely-invalid-token");

        var res = await client.PostAsync("/api/v1/incis", Json(new { name = "C5", casNumber = "1-1-1" }));

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BothEnforced_Get_IsNotRejected()
    {
        using var f = Factory(csrf: true, policy: true, roles: new[] { "Admin" });
        var client = Client(f);

        var get = await client.GetAsync("/api/v1/incis");
        ((int)get.StatusCode).Should().BeLessThan(400, "τα GET δεν πρέπει να απορρίπτονται από CSRF/policy");

        var head = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/api/v1/incis"));
        head.StatusCode.Should().NotBe(HttpStatusCode.BadRequest, "το HEAD δεν πρέπει να απορρίπτεται από CSRF");
        head.StatusCode.Should().NotBe(HttpStatusCode.Forbidden, "το HEAD δεν πρέπει να απορρίπτεται από το write-policy");
        // Σημ.: 405 = routing (το endpoint εκθέτει GET)· δεν προέρχεται από CSRF/policy.
    }

    [Fact]
    public async Task BothEnforced_Anonymous_Returns401()
    {
        using var f = Factory(csrf: true, policy: true, authenticated: false);
        var res = await Client(f).PostAsync("/api/v1/incis", Json(new { name = "C6", casNumber = "1-1-1" }));

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
