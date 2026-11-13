using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Models;
using Xunit;

namespace INCIS_SaaP.Tests.Controllers;

/// <summary>
/// Επαληθεύει ότι τα write endpoints εισαγωγής του καταλόγου INCI είναι
/// προσβάσιμα μόνο από Admin/SuperUser (δέσμευση κατά μη εξουσιοδοτημένης αλλαγής).
/// </summary>
public class ImportControllerAuthorizationTests
{
    [Fact]
    public void Controller_RequiresAuthenticatedUser()
    {
        typeof(ImportController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(nameof(ImportController.InciCatalog))]
    [InlineData(nameof(ImportController.SeedInciCatalog))]
    public void MutatingActions_RequireAdminOrSuperUser(string methodName)
    {
        var method = typeof(ImportController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(m => m.Name == methodName && m.GetParameters().All(p => true));

        var roles = method
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
            .Select(a => a.Roles!)
            .ToList();

        roles.Should().NotBeEmpty("το endpoint πρέπει να έχει [Authorize(Roles=...)]");
        roles.Should().Contain(r => r.Contains(AppRoles.Admin) && r.Contains(AppRoles.SuperUser));
    }
}
