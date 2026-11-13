using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Data;
using INCIS_SaaP.Infrastructure.Tenancy;
using INCIS_SaaP.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace INCIS_SaaP.Tests.Tenancy;

/// <summary>
/// Επαληθεύει την απομόνωση των Documents / WebhookSubscriptions / BillingInvoices
/// (tenant-owned με query filters) και ότι το Subscription API χρησιμοποιεί τον
/// τρέχοντα tenant και ΟΧΙ τον «πρώτο ενεργό» (cross-tenant leak).
/// </summary>
public class DocumentWebhookIsolationTests
{
    private static INCISDbContext NewCtx(string db, int? tenantId, string role = AppRoles.User)
    {
        var http = new DefaultHttpContext();
        if (tenantId.HasValue) http.Items["TenantId"] = tenantId.Value;
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, "user-1"),
            new System.Security.Claims.Claim(ClaimTypes.Role, role)
        }, "test"));
        var accessor = new HttpContextAccessor { HttpContext = http };
        var options = new DbContextOptionsBuilder<INCISDbContext>().UseInMemoryDatabase(db).Options;
        return new INCISDbContext(options, accessor);
    }

    private static string NewDb() => "doc_wh_" + Guid.NewGuid().ToString("N");

    private static Document Doc(int? tenantId, string name) => new()
    {
        TenantId = tenantId,
        FileName = name,
        StoragePath = "docs/" + name,
        ContentType = "application/pdf",
        DocumentType = "TDS"
    };

    private static WebhookSubscription Hook(int? tenantId, string url) => new()
    {
        TenantId = tenantId,
        Url = url,
        Secret = "secret",
        Events = "formula.created"
    };

    [Fact]
    public async Task Documents_AreTenantIsolated_AndStamped()
    {
        var db = NewDb();
        await using (var c7 = NewCtx(db, 7)) { c7.Documents.Add(Doc(null, "t7.pdf")); await c7.SaveChangesAsync(); }
        await using (var c8 = NewCtx(db, 8)) { c8.Documents.Add(Doc(null, "t8.pdf")); await c8.SaveChangesAsync(); }

        await using (var t7 = NewCtx(db, 7))
        {
            var rows = await t7.Documents.ToListAsync();
            rows.Should().ContainSingle(d => d.FileName == "t7.pdf");
            rows.Should().NotContain(d => d.FileName == "t8.pdf");
            rows.Single().TenantId.Should().Be(7, "το TenantId σφραγίζεται αυτόματα");
        }
        await using (var t8 = NewCtx(db, 8))
        {
            (await t8.Documents.CountAsync()).Should().Be(1);
            (await t8.Documents.SingleAsync()).FileName.Should().Be("t8.pdf");
        }
        await using (var admin = NewCtx(db, 7, AppRoles.Admin))
            (await admin.Documents.CountAsync()).Should().Be(2, "ο global Admin βλέπει όλα");
    }

    [Fact]
    public async Task Webhooks_AreTenantIsolated()
    {
        var db = NewDb();
        await using (var c7 = NewCtx(db, 7)) { c7.WebhookSubscriptions.Add(Hook(null, "https://a.example/h")); await c7.SaveChangesAsync(); }
        await using (var c8 = NewCtx(db, 8)) { c8.WebhookSubscriptions.Add(Hook(null, "https://b.example/h")); await c8.SaveChangesAsync(); }

        await using (var t7 = NewCtx(db, 7))
        {
            var rows = await t7.WebhookSubscriptions.ToListAsync();
            rows.Should().ContainSingle(w => w.Url == "https://a.example/h");
            rows.Should().NotContain(w => w.Url == "https://b.example/h");
        }
    }

    [Fact]
    public async Task BillingInvoices_AreTenantIsolated()
    {
        var db = NewDb();
        await using (var c7 = NewCtx(db, 7))
        {
            c7.BillingInvoices.Add(new BillingInvoice { TenantId = 7, InvoiceNumber = "INV-7" });
            await c7.SaveChangesAsync();
        }
        await using (var c8 = NewCtx(db, 8))
        {
            c8.BillingInvoices.Add(new BillingInvoice { TenantId = 8, InvoiceNumber = "INV-8" });
            await c8.SaveChangesAsync();
        }

        await using (var t7 = NewCtx(db, 7))
        {
            var rows = await t7.BillingInvoices.ToListAsync();
            rows.Should().ContainSingle(i => i.InvoiceNumber == "INV-7");
            rows.Should().NotContain(i => i.InvoiceNumber == "INV-8");
        }
    }

    [Fact]
    public async Task SubscriptionStatus_UsesCurrentTenant_NotFirstActive()
    {
        var db = NewDb();
        await using (var seed = NewCtx(db, 7))
        {
            seed.Tenants.Add(new Tenant { Id = 7, Name = "TENANT-SEVEN", Slug = "t7", IsActive = true, SubscriptionStatus = "Active" });
            seed.Tenants.Add(new Tenant { Id = 8, Name = "TENANT-EIGHT", Slug = "t8", IsActive = true, SubscriptionStatus = "Active" });
            await seed.SaveChangesAsync();
        }

        await using var ctx = NewCtx(db, 7);
        var http = new DefaultHttpContext();
        http.Items["TenantId"] = 7;
        var tenantService = new TenantService(new HttpContextAccessor { HttpContext = http });
        var controller = new ApiSubscriptionController(ctx, NullLogger<ApiSubscriptionController>.Instance, tenantService);

        var result = await controller.GetStatus();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(ok.Value);
        json.Should().Contain("TENANT-SEVEN");
        json.Should().NotContain("TENANT-EIGHT", "δεν πρέπει να επιστρέφεται άλλος tenant");
    }
}
