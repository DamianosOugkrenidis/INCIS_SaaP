using System.Security.Claims;
using FluentAssertions;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace INCIS_SaaP.Tests.Tenancy;

/// <summary>
/// Επαληθεύει ότι τα EF global query filters απομονώνουν τους tenants και ότι το
/// TenantId σφραγίζεται από το τρέχον tenant — ανεξάρτητα από τη σειρά κατασκευής
/// του DbContext (lazy resolution από το HttpContext).
/// </summary>
public class TenantIsolationTests
{
    private static INCISDbContext NewCtx(string db, int? tenantId, string role = AppRoles.User)
    {
        var http = new DefaultHttpContext();
        if (tenantId.HasValue) http.Items["TenantId"] = tenantId.Value;
        var identity = new ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, "user-1"),
            new System.Security.Claims.Claim(ClaimTypes.Role, role)
        }, "test");
        http.User = new ClaimsPrincipal(identity);
        var accessor = new HttpContextAccessor { HttpContext = http };
        var options = new DbContextOptionsBuilder<INCISDbContext>().UseInMemoryDatabase(db).Options;
        return new INCISDbContext(options, accessor);
    }

    private static int _n;
    private static string NewDb() => "tenant_iso_" + Guid.NewGuid().ToString("N") + "_" + Interlocked.Increment(ref _n);

    [Fact]
    public async Task GlobalFilter_Isolates_Tenants_WithPrivilegedException()
    {
        var db = NewDb();
        await using (var c1 = NewCtx(db, 1))
        {
            c1.Formulas.Add(new Formula { FormulaCode = "T1-001", ProductName = "Tenant 1 Formula", TenantId = 1 });
            await c1.SaveChangesAsync();
        }
        await using (var c2 = NewCtx(db, 2))
        {
            c2.Formulas.Add(new Formula { FormulaCode = "T2-001", ProductName = "Tenant 2 Formula", TenantId = 2 });
            await c2.SaveChangesAsync();
        }

        await using (var t1 = NewCtx(db, 1))
        {
            var rows = await t1.Formulas.ToListAsync();
            rows.Should().ContainSingle(f => f.FormulaCode == "T1-001");
            rows.Should().NotContain(f => f.FormulaCode == "T2-001", "ο tenant 1 δεν πρέπει να βλέπει δεδομένα του tenant 2");
        }
        await using (var t2 = NewCtx(db, 2))
        {
            var rows = await t2.Formulas.ToListAsync();
            rows.Should().ContainSingle(f => f.FormulaCode == "T2-001");
            rows.Should().NotContain(f => f.FormulaCode == "T1-001");
        }
        await using (var admin = NewCtx(db, 1, AppRoles.Admin))
        {
            (await admin.Formulas.CountAsync()).Should().Be(2, "ο global Admin βλέπει όλους τους tenants");
        }
    }

    [Fact]
    public async Task SaveChanges_StampsTenantId_OnNewTenantOwnedEntities()
    {
        var db = NewDb();
        await using (var c = NewCtx(db, 7))
        {
            c.Clients.Add(new Client { Name = "Pilot Client" });
            await c.SaveChangesAsync();
        }
        await using (var verify = NewCtx(db, 7))
        {
            var client = await verify.Clients.SingleAsync();
            client.TenantId.Should().Be(7, "το TenantId πρέπει να σφραγίζεται αυτόματα από το τρέχον tenant");
        }
    }

    [Fact]
    public async Task LegacyNullTenant_Rows_AreVisibleToAll()
    {
        var db = NewDb();
        await using (var c = NewCtx(db, 1))
        {
            c.Clients.Add(new Client { Name = "Shared Legacy" }); // TenantId == null
            await c.SaveChangesAsync();
        }
        // Διαγραφή του stamped tenant για να προσομοιώσουμε legacy null-tenant εγγραφή
        await using (var seed = NewCtx(db, 1, AppRoles.Admin))
        {
            var cl = await seed.Clients.SingleAsync();
            cl.TenantId = null;
            await seed.SaveChangesAsync();
        }

        await using (var t3 = NewCtx(db, 3))
        {
            (await t3.Clients.CountAsync()).Should().Be(1, "οι legacy (null-tenant) εγγραφές είναι κοινές");
        }
    }
}
