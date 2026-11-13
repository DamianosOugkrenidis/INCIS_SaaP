// Author: Damianos Pythagoras Ougkrenidis
//
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models;

/// <summary>
/// Κατηγορίες templates φορμούλων
/// </summary>
public class TemplateCategory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Parent category για ιεραρχική δομή
    /// </summary>
    public int? ParentCategoryId { get; set; }

    [ForeignKey("ParentCategoryId")]
    public virtual TemplateCategory? ParentCategory { get; set; }

    public virtual ICollection<TemplateCategory> SubCategories { get; set; } = new List<TemplateCategory>();

    public virtual ICollection<FormulaTemplate> Templates { get; set; } = new List<FormulaTemplate>();

    /// <summary>
    /// Εικονίδιο/icon για UI
    /// </summary>
    [MaxLength(50)]
    public string? Icon { get; set; }

    /// <summary>
    /// Σειρά εμφάνισης
    /// </summary>
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(256)]
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Προσχέδια (templates) φορμούλων για επαναχρησιμοποίηση
/// </summary>
public class FormulaTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string TemplateName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string TemplateCode { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Κατηγορία template
    /// </summary>
    public int? CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    public virtual TemplateCategory? Category { get; set; }

    /// <summary>
    /// Τύπος προϊόντος (Shampoo, Cream, Lotion, Serum, κλπ)
    /// </summary>
    [MaxLength(100)]
    public string? ProductType { get; set; }

    /// <summary>
    /// JSON snapshot του template (περιέχει default α' ύλες, ποσοστά, κλπ)
    /// </summary>
    public string TemplateData { get; set; } = string.Empty;

    /// <summary>
    /// Είδος template (Base Formula, Modification, Complete)
    /// </summary>
    [MaxLength(50)]
    public string? TemplateType { get; set; }

    /// <summary>
    /// Target χρήση (Leave-on, Rinse-off)
    /// </summary>
    [MaxLength(50)]
    public string? TargetUse { get; set; }

    /// <summary>
    /// Επιθυμητά χαρακτηριστικά (Natural, Organic, Vegan, κλπ)
    /// </summary>
    [MaxLength(500)]
    public string? DesiredAttributes { get; set; }

    /// <summary>
    /// Πλήθος χρήσεων του template
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Τελευταία χρήση του template
    /// </summary>
    public DateTime? LastUsed { get; set; }

    /// <summary>
    /// Tags για αναζήτηση
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// Εκτιμώμενο κόστος ανά μονάδα
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? EstimatedCost { get; set; }

    /// <summary>
    /// Εκτιμώμενο φυσικό ποσοστό
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? EstimatedNaturalPercentage { get; set; }

    /// <summary>
    /// Υποδεικνύει αν είναι δημόσιο template (διαθέσιμο σε όλους)
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Υποδεικνύει αν είναι προκαθορισμένο template του συστήματος
    /// </summary>
    public bool IsSystemTemplate { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Rating/βαθμολογία από χρήστες
    /// </summary>
    [Column(TypeName = "decimal(3,2)")]
    public decimal? Rating { get; set; }

    /// <summary>
    /// Πλήθος αξιολογήσεων
    /// </summary>
    public int RatingCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(256)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? LastModified { get; set; }

    [MaxLength(256)]
    public string? LastModifiedBy { get; set; }

    /// <summary>
    /// Σχόλια/οδηγίες χρήσης
    /// </summary>
    [MaxLength(2000)]
    public string? UsageInstructions { get; set; }

    /// <summary>
    /// Προειδοποιήσεις/περιορισμοί
    /// </summary>
    [MaxLength(1000)]
    public string? Warnings { get; set; }

    /// <summary>
    /// Εικόνα/thumbnail του template
    /// </summary>
    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Formulas που δημιουργήθηκαν από αυτό το template
    /// </summary>
    public virtual ICollection<FormulaTemplateUsage> TemplateUsages { get; set; } = new List<FormulaTemplateUsage>();
}

/// <summary>
/// Καταγραφή χρήσης template σε φόρμουλα
/// </summary>
public class FormulaTemplateUsage
{
    [Key]
    public int Id { get; set; }

    public int TemplateId { get; set; }

    [ForeignKey("TemplateId")]
    public virtual FormulaTemplate? Template { get; set; }

    public int FormulaId { get; set; }

    [ForeignKey("FormulaId")]
    public virtual Formula? Formula { get; set; }

    /// <summary>
    /// Ποσοστό τροποποίησης από το αρχικό template
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? ModificationPercentage { get; set; }

    [MaxLength(1000)]
    public string? ModificationNotes { get; set; }

    public DateTime UsedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(256)]
    public string UsedBy { get; set; } = string.Empty;

    /// <summary>
    /// Feedback για το template
    /// </summary>
    [MaxLength(2000)]
    public string? Feedback { get; set; }

    /// <summary>
    /// Βαθμολογία της χρήσης
    /// </summary>
    public int? UserRating { get; set; }
}

/// <summary>
/// Raw materials συνδεδεμένες με template
/// </summary>
public class TemplateRawMaterial
{
    [Key]
    public int Id { get; set; }

    public int TemplateId { get; set; }

    [ForeignKey("TemplateId")]
    public virtual FormulaTemplate? Template { get; set; }

    public int? RawMaterialId { get; set; }

    [ForeignKey("RawMaterialId")]
    public virtual RawMaterial? RawMaterial { get; set; }

    /// <summary>
    /// Όνομα α' ύλης (αν δεν υπάρχει FK)
    /// </summary>
    [MaxLength(200)]
    public string? RawMaterialName { get; set; }

    /// <summary>
    /// Προτεινόμενο ποσοστό
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? SuggestedPercentage { get; set; }

    /// <summary>
    /// Ελάχιστο ποσοστό
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MinPercentage { get; set; }

    /// <summary>
    /// Μέγιστο ποσοστό
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MaxPercentage { get; set; }

    /// <summary>
    /// Φάση προσθήκης
    /// </summary>
    [MaxLength(100)]
    public string? Phase { get; set; }

    /// <summary>
    /// Υποδεικνύει αν είναι υποχρεωτικό συστατικό
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Εναλλακτικές α' ύλες (comma-separated IDs ή names)
    /// </summary>
    [MaxLength(500)]
    public string? Alternatives { get; set; }

    /// <summary>
    /// Σειρά εμφάνισης/προσθήκης
    /// </summary>
    public int DisplayOrder { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
