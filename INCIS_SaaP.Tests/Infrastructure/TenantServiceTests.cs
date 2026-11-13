using FluentAssertions;
using INCIS_SaaP.Infrastructure.Tenancy;
using INCIS_SaaP.Models;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace INCIS_SaaP.Tests.Infrastructure;

/// <summary>
/// Tests for TenantService reading tenant context from HttpContext.Items.
/// </summary>
public class TenantServiceTests
{
    private static TenantService CreateService(HttpContext? context)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(context);
        return new TenantService(accessor.Object);
    }

    [Fact]
    public void GetCurrentTenantId_ReturnsValue_FromItems()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items["TenantId"] = 7;

        CreateService(ctx).GetCurrentTenantId().Should().Be(7);
    }

    [Fact]
    public void GetCurrentTenantId_ReturnsNull_WhenNotSet()
    {
        CreateService(new DefaultHttpContext()).GetCurrentTenantId().Should().BeNull();
    }

    [Fact]
    public void GetCurrentTenant_ReturnsTenant_FromItems()
    {
        var tenant = new Tenant { Id = 3, Name = "Acme" };
        var ctx = new DefaultHttpContext();
        ctx.Items["Tenant"] = tenant;

        CreateService(ctx).GetCurrentTenant().Should().BeSameAs(tenant);
    }

    [Fact]
    public void HasFeature_ReturnsFalse_WhenNoTenantOrPlan()
    {
        CreateService(new DefaultHttpContext()).HasFeature("Reports").Should().BeFalse();

        var ctxNoPlan = new DefaultHttpContext();
        ctxNoPlan.Items["Tenant"] = new Tenant { Id = 1, SubscriptionPlan = null };
        CreateService(ctxNoPlan).HasFeature("Reports").Should().BeFalse();
    }

    [Theory]
    [InlineData("Webhooks", true)]
    [InlineData("Production", true)]
    [InlineData("Reports", false)]
    [InlineData("ApiAccess", false)]
    [InlineData("Unknown", false)]
    public void HasFeature_MapsToSubscriptionPlanFlags(string feature, bool expected)
    {
        var plan = new SubscriptionPlan
        {
            HasWebhooks = true,
            HasProduction = true,
            HasReports = false,
            HasApiAccess = false
        };
        var ctx = new DefaultHttpContext();
        ctx.Items["Tenant"] = new Tenant { Id = 1, SubscriptionPlan = plan };

        CreateService(ctx).HasFeature(feature).Should().Be(expected);
    }
}
