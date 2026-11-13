using FluentAssertions;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Data;
using INCIS_SaaP.Infrastructure.Tenancy;
using INCIS_SaaP.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace INCIS_SaaP.Tests.Tenancy;

/// <summary>
/// Επιβολή ορίων πακέτου: storage quota (MaxStorageMB) στο upload και feature gating
/// (HasWebhooks) στη δημιουργία webhooks.
/// </summary>
public class PlanLimitsEnforcementTests
{
    private static INCISDbContext Ctx(string db)
    {
        var options = new DbContextOptionsBuilder<INCISDbContext>().UseInMemoryDatabase(db).Options;
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        return new INCISDbContext(options, accessor);
    }

    private static ITenantService TenantSvc(int tenantId, Tenant? tenant = null)
    {
        var m = new Mock<ITenantService>();
        m.Setup(s => s.GetCurrentTenantId()).Returns(tenantId);
        m.Setup(s => s.GetCurrentTenant()).Returns(tenant);
        return m.Object;
    }

    [Fact]
    public async Task Upload_ExceedingStorageQuota_IsRejected()
    {
        var db = "quota_" + Guid.NewGuid().ToString("N");
        await using var ctx = Ctx(db);

        ctx.Documents.Add(new Document
        {
            TenantId = 7,
            FileName = "existing.pdf",
            StoragePath = "docs/existing.pdf",
            ContentType = "application/pdf",
            DocumentType = "TDS",
            FileSize = 1024 * 1024
        });
        await ctx.SaveChangesAsync();

        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.WebRootPath).Returns(Path.GetTempPath());

        var tenant = new Tenant { Id = 7, Name = "T7", Slug = "t7", MaxStorageMB = 1 }; // 1MB limit, 1MB used
        var controller = new ApiDocumentsController(ctx, NullLogger<ApiDocumentsController>.Instance, env.Object, TenantSvc(7, tenant))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var file = new Mock<IFormFile>();
        file.SetupGet(f => f.FileName).Returns("new.pdf");
        file.SetupGet(f => f.Length).Returns(2048);
        file.SetupGet(f => f.ContentType).Returns("application/pdf");
        file.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await controller.Upload(file.Object, "TDS", "RawMaterial", 1, "desc");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task WebhookCreation_PlanWithoutWebhooks_IsForbidden()
    {
        var db = "whgate_" + Guid.NewGuid().ToString("N");
        await using var ctx = Ctx(db);

        var plan = new SubscriptionPlan { Name = "NoHooks", MaxUsers = 5, HasWebhooks = false, IsActive = true };
        var tenant = new Tenant { Id = 9, Name = "T9", Slug = "t9", MaxUsers = 5, MaxStorageMB = 100, SubscriptionPlan = plan };

        var controller = new ApiWebhooksController(ctx, NullLogger<ApiWebhooksController>.Instance, TenantSvc(9, tenant))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Create(new CreateWebhookRequest { Url = "https://example.com/h", Events = "formula.created" });

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(403);
        (await ctx.WebhookSubscriptions.CountAsync()).Should().Be(0);
    }
}
