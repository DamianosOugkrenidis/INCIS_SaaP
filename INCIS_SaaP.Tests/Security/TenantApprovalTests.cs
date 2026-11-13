using System.Net;
using FluentAssertions;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace INCIS_SaaP.Tests.Security;

/// <summary>
/// Επαληθεύει τη λογική έγκρισης χρήστη από TenantAdmin (tenant-scoped) in-process.
/// </summary>
[Trait("Category", "Security")]
public class TenantApprovalTests
{
    [Fact]
    public async Task TenantAdmin_CanApprove_SameTenantPendingUser()
    {
        using var factory = new RoleWebApplicationFactory(
            s => { s.IsAuthenticated = true; s.Roles = new[] { AppRoles.TenantAdmin }; },
            disableAntiforgery: true);
        factory.EnsureSeeded();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<INCISDbContext>();
            db.Tenants.Add(new Tenant { Id = 1, Name = "Tenant One", Slug = "tenant-one" });
            // Ο test auth handler χρησιμοποιεί σταθερό id "test-user-id" → ο actor έχει αυτό το Id.
            db.Users.Add(new ApplicationUser { Id = "test-user-id", UserName = "actor@t1.local", Email = "actor@t1.local", TenantId = 1, IsApproved = true, IsActive = true });
            db.Users.Add(new ApplicationUser { Id = "pending-1", UserName = "p@t1.local", Email = "p@t1.local", TenantId = 1, IsApproved = false, IsActive = false });
            db.SaveChanges();
        }

        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var res = await client.PostAsync("/Users/Approve?id=pending-1", new FormUrlEncodedContent(new Dictionary<string, string>()));

        res.StatusCode.Should().Be(HttpStatusCode.Found, "η έγκριση πρέπει να πετύχει και να κάνει redirect (όχι 403/400)");

        using var verify = factory.Services.CreateScope();
        var db2 = verify.ServiceProvider.GetRequiredService<INCISDbContext>();
        var pending = await db2.Users.FirstAsync(u => u.Id == "pending-1");
        pending.IsApproved.Should().BeTrue("ο εγκεκριμένος χρήστης πρέπει να έχει IsApproved=true");
        pending.IsActive.Should().BeTrue();
    }
}
