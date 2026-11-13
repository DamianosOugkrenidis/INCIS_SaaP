// Author: Damianos Pythagoras Ougkrenidis
//
using System.ComponentModel.DataAnnotations;

namespace INCIS_SaaP.Models;

/// <summary>
/// Μοντέλο για καταγραφή αλλαγών (Audit Trail)
/// </summary>
public class AuditLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;

    [Required]
    public string EntityId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // Created, Updated, Deleted, Viewed

    [MaxLength(450)]
    public string? UserId { get; set; }

    [MaxLength(200)]
    public string? UserEmail { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(200)]
    public string? UserAgent { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Enum για τύπους αλλαγών
/// </summary>
public enum AuditAction
{
    Created,
    Updated,
    Deleted,
    Viewed,
    Exported,
    Imported,
    Login,
    Logout,
    FailedLogin,
    PasswordChanged,
    RoleChanged
}
