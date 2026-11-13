using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models;

/// <summary>
/// Tenant (Organization) - Multi-tenancy support
/// Each tenant represents a separate company/lab using the platform
/// </summary>
public class Tenant
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Unique tenant identifier (slug, used in URLs/API keys)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Organization name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Business email
    /// </summary>
    [MaxLength(100)]
    public string? Email { get; set; }

    /// <summary>
    /// Business phone
    /// </summary>
    [MaxLength(50)]
    public string? Phone { get; set; }

    /// <summary>
    /// Business address
    /// </summary>
    [MaxLength(300)]
    public string? Address { get; set; }

    /// <summary>
    /// VAT/Tax ID
    /// </summary>
    [MaxLength(50)]
    public string? TaxId { get; set; }

    /// <summary>
    /// Country code (ISO 3166-1 alpha-2)
    /// </summary>
    [MaxLength(2)]
    public string Country { get; set; } = "GR";

    /// <summary>
    /// Active subscription plan
    /// </summary>
    public int? SubscriptionPlanId { get; set; }
    [ForeignKey("SubscriptionPlanId")]
    public virtual SubscriptionPlan? SubscriptionPlan { get; set; }

    /// <summary>
    /// Subscription status
    /// </summary>
    [MaxLength(20)]
    public string SubscriptionStatus { get; set; } = "Trial"; // Trial, Active, Suspended, Cancelled

    /// <summary>
    /// Trial/subscription end date
    /// </summary>
    public DateTime? SubscriptionExpiresAt { get; set; }

    /// <summary>
    /// Maximum number of users allowed
    /// </summary>
    public int MaxUsers { get; set; } = 5;

    /// <summary>
    /// Maximum storage in MB
    /// </summary>
    public long MaxStorageMB { get; set; } = 1024; // 1GB default

    /// <summary>
    /// Used storage in bytes
    /// </summary>
    public long UsedStorageBytes { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}

/// <summary>
/// Subscription plans for the SaaS platform
/// </summary>
public class SubscriptionPlan
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty; // Free, Starter, Professional, Enterprise

    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// Monthly price in EUR
    /// </summary>
    public decimal MonthlyPrice { get; set; }

    /// <summary>
    /// Annual price in EUR (discount)
    /// </summary>
    public decimal AnnualPrice { get; set; }

    /// <summary>
    /// Max users per tenant
    /// </summary>
    public int MaxUsers { get; set; }

    /// <summary>
    /// Max formulas allowed
    /// </summary>
    public int MaxFormulas { get; set; }

    /// <summary>
    /// Max raw materials allowed
    /// </summary>
    public int MaxRawMaterials { get; set; }

    /// <summary>
    /// Max storage in MB
    /// </summary>
    public long MaxStorageMB { get; set; }

    /// <summary>
    /// API requests per minute
    /// </summary>
    public int ApiRateLimit { get; set; }

    /// <summary>
    /// Whether this plan includes webhook support
    /// </summary>
    public bool HasWebhooks { get; set; }

    /// <summary>
    /// Whether this plan includes production batches
    /// </summary>
    public bool HasProduction { get; set; }

    /// <summary>
    /// Whether this plan includes reports/export
    /// </summary>
    public bool HasReports { get; set; }

    /// <summary>
    /// Whether API access is included
    /// </summary>
    public bool HasApiAccess { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Billing record / Invoice
/// </summary>
public class BillingInvoice
{
    [Key]
    public int Id { get; set; }

    public int TenantId { get; set; }
    [ForeignKey("TenantId")]
    public virtual Tenant? Tenant { get; set; }

    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public decimal Tax { get; set; }
    public decimal TotalAmount { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Paid, Failed, Refunded

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// External payment reference (Stripe, PayPal, etc.)
    /// </summary>
    [MaxLength(200)]
    public string? PaymentReference { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; } // Card, BankTransfer, PayPal
}
