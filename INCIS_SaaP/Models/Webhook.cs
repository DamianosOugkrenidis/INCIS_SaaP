using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models;

/// <summary>
/// Webhook subscription - Allows tenants to receive real-time notifications
/// </summary>
public class WebhookSubscription : ITenantOwned
{
    [Key]
    public int Id { get; set; }

    public int? TenantId { get; set; }
    [ForeignKey("TenantId")]
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// Target URL to send webhook payloads
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Secret key for HMAC signature verification
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Events this webhook subscribes to
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Events { get; set; } = string.Empty;
    // Events: formula.created, formula.validated, formula.produced,
    //         rawmaterial.created, rawmaterial.expired,
    //         client.created, batch.completed, sample.expired

    /// <summary>
    /// Whether this webhook is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    public int FailureCount { get; set; } = 0;

    /// <summary>
    /// Last successful delivery
    /// </summary>
    public DateTime? LastSuccessAt { get; set; }

    /// <summary>
    /// Last failure
    /// </summary>
    public DateTime? LastFailureAt { get; set; }

    [MaxLength(500)]
    public string? LastFailureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Webhook delivery log
/// </summary>
public class WebhookDelivery
{
    [Key]
    public int Id { get; set; }

    public int WebhookSubscriptionId { get; set; }
    [ForeignKey("WebhookSubscriptionId")]
    public virtual WebhookSubscription? Subscription { get; set; }

    /// <summary>
    /// Event type that triggered this delivery
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// JSON payload sent
    /// </summary>
    public string Payload { get; set; } = "{}";

    /// <summary>
    /// HTTP response status code
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// Response body (truncated)
    /// </summary>
    [MaxLength(2000)]
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Delivery status
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Success, Failed, Retrying

    /// <summary>
    /// Number of attempts
    /// </summary>
    public int Attempts { get; set; } = 0;

    /// <summary>
    /// Time taken in ms
    /// </summary>
    public int? DurationMs { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
}
