using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Infrastructure.Tenancy;

/// <summary>
/// Interface for entities that belong to a tenant
/// </summary>
public interface ITenantEntity
{
    int TenantId { get; set; }
}

/// <summary>
/// Extension methods for applying tenant query filters to EF Core
/// </summary>
public static class TenantQueryFilterExtensions
{
    /// <summary>
    /// Apply global query filter for tenant isolation on Document and Webhook entities.
    /// For entities that don't have TenantId (Formulas, RawMaterials, etc.),
    /// filtering is done at the controller/service level using ITenantService.
    /// </summary>
    public static void ApplyTenantQueryFilters(this ModelBuilder modelBuilder, int? tenantId)
    {
        // Documents are strictly tenant-scoped
        if (tenantId.HasValue)
        {
            modelBuilder.Entity<Document>().HasQueryFilter(d => d.TenantId == tenantId.Value);
            modelBuilder.Entity<WebhookSubscription>().HasQueryFilter(w => w.TenantId == tenantId.Value);
            modelBuilder.Entity<WebhookDelivery>().HasQueryFilter(wd =>
                wd.Subscription != null && wd.Subscription.TenantId == tenantId.Value);
        }
    }
}
