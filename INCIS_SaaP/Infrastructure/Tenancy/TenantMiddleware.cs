using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Infrastructure.Tenancy;

/// <summary>
/// Middleware that resolves the current tenant from the authenticated user
/// and stores it in HttpContext.Items for use by controllers and services
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, INCISDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await dbContext.Users
                    .OfType<ApplicationUser>()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user?.TenantId != null)
                {
                    context.Items["TenantId"] = user.TenantId.Value;
                    context.Items["Tenant"] = await dbContext.Tenants
                        .Include(t => t.SubscriptionPlan)
                        .FirstOrDefaultAsync(t => t.Id == user.TenantId.Value);
                }
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method for tenant middleware registration
/// </summary>
public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantMiddleware>();
    }
}

/// <summary>
/// Service to get current tenant from HttpContext (injectable)
/// </summary>
public interface ITenantService
{
    int? GetCurrentTenantId();
    Tenant? GetCurrentTenant();
    bool HasFeature(string feature);
}

/// <summary>
/// Implementation that reads tenant from HttpContext.Items (set by middleware)
/// </summary>
public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? GetCurrentTenantId()
    {
        return _httpContextAccessor.HttpContext?.Items["TenantId"] as int?;
    }

    public Tenant? GetCurrentTenant()
    {
        return _httpContextAccessor.HttpContext?.Items["Tenant"] as Tenant;
    }

    public bool HasFeature(string feature)
    {
        var tenant = GetCurrentTenant();
        if (tenant?.SubscriptionPlan == null) return false;

        return feature switch
        {
            "Webhooks" => tenant.SubscriptionPlan.HasWebhooks,
            "Production" => tenant.SubscriptionPlan.HasProduction,
            "Reports" => tenant.SubscriptionPlan.HasReports,
            "ApiAccess" => tenant.SubscriptionPlan.HasApiAccess,
            _ => false
        };
    }
}
