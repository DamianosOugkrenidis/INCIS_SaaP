using System.Net;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Models;
using INCIS_SaaP.Tests.Helpers;
using Xunit;

namespace INCIS_SaaP.Tests.Security;

/// <summary>
/// Tests πολιτικής πρόσβασης (H-1). ΔΕΝ αλλάζει συμπεριφορά· τεκμηριώνει και «κλειδώνει»
/// την υφιστάμενη κατάσταση ώστε οποιαδήποτε αλλαγή να είναι συνειδητή.
///
/// Αποδεδειγμένη πολιτική (υπάρχουσα αρχιτεκτονική):
///  - WriteRoles = "Admin,SuperUser" (ProductManagement write actions).
///  - GET Create/Edit/Delete σε Clients/INCIs/RawMaterials/Suppliers/Restrictions/Formulas = Admin,SuperUser.
///  - Ο ρόλος 'User' απονέμεται στην εγγραφή (AccountController) -> υπάρχει στην πράξη.
///
/// ΓΝΩΣΤΟ ΚΕΝΟ (H-1): τα POST Create/Edit/Delete των παραπάνω MVC controllers δεν φέρουν role restriction
/// (μόνο class-level [Authorize] = authenticated). Το παρακάτω characterization test το καταγράφει.
/// </summary>
[Trait("Category", "Security")]
public class AccessPolicyTests
{
    private static HttpClient Client(RoleWebApplicationFactory f) =>
        f.EnsureSeeded().CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static readonly HttpStatusCode[] Denied =
        { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.Found };

    [Fact]
    public void WriteRoles_Policy_IsProviderPlusTenantAdmin()
    {
        var field = typeof(ProductManagementController)
            .GetField("WriteRoles", BindingFlags.NonPublic | BindingFlags.Static);
        var value = (string?)field?.GetValue(null);

        value.Should().Be(AppRoles.WriteAccess,
            "η πολιτική εγγραφής = Admin,SuperUser,TenantAdmin (provider + tenant admins· ο User παραμένει read-only)");
        value.Should().Contain(AppRoles.TenantAdmin, "οι tenant admins πρέπει να διαχειρίζονται τα δεδομένα του tenant τους");
        value!.Split(',', StringSplitOptions.TrimEntries).Should().NotContain(AppRoles.User, "ο απλός ρόλος User παραμένει read-only");
    }

    [Theory]
    [InlineData("/INCIs/Create")]
    [InlineData("/Clients/Create")]
    public async Task TenantAdminRole_Allowed_OnWritePages_Get(string url)
    {
        using var factory = new RoleWebApplicationFactory(s => s.Roles = new[] { AppRoles.TenantAdmin });
        var client = Client(factory);

        var res = await client.GetAsync(url);

        ((int)res.StatusCode).Should().BeLessThan(400, $"ο TenantAdmin πρέπει να ανοίγει τη σελίδα {url} του tenant του");
    }

    [Theory]
    [InlineData("/INCIs/Create")]
    [InlineData("/Clients/Create")]
    [InlineData("/Suppliers/Create")]
    [InlineData("/RawMaterials/Create")]
    [InlineData("/Restrictions/Create")]
    [InlineData("/Formulas/Create")]
    public async Task UserRole_Denied_OnWritePages_Get(string url)
    {
        using var factory = new RoleWebApplicationFactory(s => s.Roles = new[] { AppRoles.User });
        var client = Client(factory);

        var res = await client.GetAsync(url);

        Denied.Should().Contain(res.StatusCode, $"ο ρόλος User δεν πρέπει να ανοίγει τη σελίδα {url}");
    }

    [Theory]
    [InlineData("/INCIs/Create")]
    [InlineData("/Clients/Create")]
    public async Task AdminRole_Allowed_OnWritePages_Get(string url)
    {
        using var factory = new RoleWebApplicationFactory(s => s.Roles = new[] { AppRoles.Admin });
        var client = Client(factory);

        var res = await client.GetAsync(url);

        ((int)res.StatusCode).Should().BeLessThan(400, $"ο Admin πρέπει να ανοίγει τη σελίδα {url}");
    }

    /// <summary>
    /// H-1 ΕΦΑΡΜΟΣΤΗΚΕ: ο ρόλος 'User' απορρίπτεται server-side στο POST Create INCI.
    /// </summary>
    [Fact]
    public async Task UserRole_Denied_OnPostCreateInci()
    {
        using var factory = new RoleWebApplicationFactory(s => s.Roles = new[] { AppRoles.User }, disableAntiforgery: true);
        var client = Client(factory);
        var name = "DENIED " + Guid.NewGuid().ToString("N")[..6];

        var res = await client.PostAsync("/INCIs/Create",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Name", name),
                new KeyValuePair<string, string>("IsActive", "true")
            }));

        Denied.Should().Contain(res.StatusCode, "ο ρόλος User δεν πρέπει να μπορεί να δημιουργήσει INCI");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCIS_SaaP.Data.INCISDbContext>();
        (await db.INCIs.AnyAsync(i => i.Name == name)).Should().BeFalse("δεν πρέπει να αποθηκεύεται εγγραφή");
    }

    /// <summary>Ο ρόλος Admin επιτρέπεται (control) στο POST Create INCI.</summary>
    [Fact]
    public async Task AdminRole_Allowed_OnPostCreateInci()
    {
        using var factory = new RoleWebApplicationFactory(s => s.Roles = new[] { AppRoles.Admin }, disableAntiforgery: true);
        var client = Client(factory);
        var name = "ALLOWED " + Guid.NewGuid().ToString("N")[..6];

        var res = await client.PostAsync("/INCIs/Create",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Name", name),
                new KeyValuePair<string, string>("IsActive", "true")
            }));

        res.StatusCode.Should().Be(HttpStatusCode.Found);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INCIS_SaaP.Data.INCISDbContext>();
        (await db.INCIs.AnyAsync(i => i.Name == name)).Should().BeTrue();
    }
}
