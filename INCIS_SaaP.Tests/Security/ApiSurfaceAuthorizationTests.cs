using System.Net;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Security;

/// <summary>
/// Πλήρης κάλυψη της επιφάνειας API writes:
///  - Reflection: ΚΑΘΕ HTTP write action σε ΚΑΘΕ Api*Controller απαιτεί authentication
///    (class/action [Authorize]) και ΔΕΝ είναι [AllowAnonymous].
///  - Runtime: ανώνυμο write → 401 (cookie scheme, χωρίς redirect για [ApiController]).
/// Καταγράφει με σαφήνεια την επιφάνεια (πέραν των 23 του ApiExtendedController).
/// </summary>
[Trait("Category", "Security")]
public class ApiSurfaceAuthorizationTests
{
    private static HttpClient Client(RoleWebApplicationFactory f) =>
        f.EnsureSeeded().CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static IEnumerable<Type> ApiControllers() =>
        typeof(INCIS_SaaP.Controllers.ApiExtendedController).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                        && typeof(ControllerBase).IsAssignableFrom(t)
                        && t.Name.StartsWith("Api", StringComparison.Ordinal))
            .OrderBy(t => t.Name);

    private static readonly HashSet<string> IntentionalPublicWrites = new(StringComparer.Ordinal)
    {
        // Read-only δημόσιοι υπολογισμοί (δεν μεταβάλλουν δεδομένα): ρητά επιτρεπτοί anonymous POST.
        "BatchRegulatoryCheck"
    };

    [Fact]
    public void EveryApiWriteAction_RequiresAuthorization_NotAnonymous()
    {
        var report = new List<string>();
        int writeCount = 0;

        foreach (var t in ApiControllers())
        {
            var classHasAuth = t.GetCustomAttribute<AuthorizeAttribute>() != null;
            var classAnon = t.GetCustomAttribute<AllowAnonymousAttribute>() != null;

            var writes = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes().Any(a => a.GetType().Name is
                    "HttpPostAttribute" or "HttpPutAttribute" or "HttpPatchAttribute" or "HttpDeleteAttribute"));

            foreach (var m in writes)
            {
                writeCount++;
                var isAnon = classAnon || m.GetCustomAttribute<AllowAnonymousAttribute>() != null;

                if (isAnon)
                {
                    IntentionalPublicWrites.Should().Contain(m.Name,
                        $"ανώνυμο write επιτρέπεται ΜΟΝΟ για read-only δημόσια endpoints (βρέθηκε {t.Name}.{m.Name})");
                    report.Add($"{t.Name}.{m.Name} [public-readonly]");
                    continue;
                }

                var hasAuth = classHasAuth || m.GetCustomAttribute<AuthorizeAttribute>() != null;
                hasAuth.Should().BeTrue($"{t.Name}.{m.Name} πρέπει να απαιτεί authentication");
                report.Add($"{t.Name}.{m.Name}");
            }
        }

        writeCount.Should().BeGreaterThanOrEqualTo(23, "η επιφάνεια API writes περιλαμβάνει τουλάχιστον τα 23 του ApiExtendedController");
        report.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("POST", "/api/v1/clients")]
    [InlineData("POST", "/api/v1/incis")]
    [InlineData("PUT", "/api/v1/incis/1")]
    [InlineData("DELETE", "/api/v1/clients/1")]
    [InlineData("POST", "/api/v1/feature-flags")]
    [InlineData("POST", "/api/v1/documents/upload")]
    public async Task Anonymous_WriteAcrossApiControllers_IsDenied(string method, string url)
    {
        using var factory = new RoleWebApplicationFactory(s => s.IsAuthenticated = false, disableAntiforgery: true);
        var client = Client(factory);

        var req = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        var res = await client.SendAsync(req);

        var denied = res.StatusCode == HttpStatusCode.Found || (int)res.StatusCode >= 400;
        denied.Should().BeTrue($"{method} {url} ανώνυμο -> απορρίπτεται (βρέθηκε {(int)res.StatusCode})");
    }
}
