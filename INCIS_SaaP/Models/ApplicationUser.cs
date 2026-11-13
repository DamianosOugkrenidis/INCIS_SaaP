// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Identity;

namespace INCIS_SaaP.Models;

/// <summary>
/// Custom application user with roles
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
    
    // Ευαίσθητα πεδία (κρυπτογραφημένα)
    public string? EncryptedPhone { get; set; }     // Κρυπτογραφημένο τηλέφωνο
    public string? EncryptedAFM { get; set; }       // Κρυπτογραφημένο ΑΦΜ
    public string? EncryptedAddress { get; set; }  // Κρυπτογραφημένη διεύθυνση
    public string? Notes { get; set; }             // Σημειώσεις
    
    // Two-Factor Authentication
    public new bool TwoFactorEnabled { get; set; } = false;
    public string? TwoFactorSecretKey { get; set; }
    public DateTime? TwoFactorEnabledAt { get; set; }
    
    // Email Verification
    public new bool EmailConfirmed { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    
    // Password Reset
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    
    // Last Login
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    
    // Profile
    public string? ProfileImageUrl { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    
    // Security: Force password change on first login
    public bool MustChangePassword { get; set; } = false;

    // Multi-tenancy
    /// <summary>
    /// Tenant (Organization) this user belongs to
    /// </summary>
    public int? TenantId { get; set; }
    public virtual Tenant? Tenant { get; set; }

    // Self-registration
    /// <summary>
    /// Whether user registered themselves (vs admin-created)
    /// </summary>
    public bool IsSelfRegistered { get; set; } = false;

    /// <summary>
    /// Whether user's registration has been approved by admin
    /// </summary>
    public bool IsApproved { get; set; } = true;
}

/// <summary>
/// User roles enum
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string SuperUser = "SuperUser";
    public const string User = "User";
    /// <summary>Διαχειριστής του δικού του Tenant (εταιρείας) — διαχειρίζεται seats/χρήστες μόνο της εταιρείας του.</summary>
    public const string TenantAdmin = "TenantAdmin";

    /// <summary>
    /// Ρόλοι με δικαίωμα επιχειρησιακής εγγραφής (business write).
    /// Admin/SuperUser = provider-level (βλέπουν όλα)· TenantAdmin = tenant-scoped administrator
    /// (η απομόνωση επιβάλλεται από τα EF global query filters στο INCISDbContext).
    /// Ο ρόλος 'User' παραμένει read-only (πολιτική H-1).
    /// </summary>
    public const string WriteAccess = Admin + "," + SuperUser + "," + TenantAdmin;
}
