using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models;

/// <summary>
/// Document storage - Stores uploaded files (TDS, MSDS, ISO certificates, etc.)
/// </summary>
public class Document : ITenantOwned
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Tenant owner
    /// </summary>
    public int? TenantId { get; set; }
    [ForeignKey("TenantId")]
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// Original file name
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Stored file path (relative to storage root)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// MIME type (application/pdf, image/png, etc.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Document type/category
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string DocumentType { get; set; } = string.Empty;
    // Types: TDS, MSDS, ISO16128, BSE_TSE, Vegan, RMI, AllergenCertificate,
    //        PhthalatesStatement, SolventVehicle, MarketingInfo, COA, Other

    /// <summary>
    /// Related entity type (RawMaterial, Formula, Client, Supplier)
    /// </summary>
    [MaxLength(50)]
    public string? EntityType { get; set; }

    /// <summary>
    /// Related entity ID
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Description/notes
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Document version
    /// </summary>
    [MaxLength(20)]
    public string? Version { get; set; }

    /// <summary>
    /// Uploaded by user ID
    /// </summary>
    [MaxLength(450)]
    public string? UploadedByUserId { get; set; }

    /// <summary>
    /// SHA-256 hash for integrity check
    /// </summary>
    [MaxLength(64)]
    public string? FileHash { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}
