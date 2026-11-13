// Author: Damianos Pythagoras Ougkrenidis
//
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models;

/// <summary>
/// Version control για φόρμουλες - καταγραφή αλλαγών και ιστορικό
/// </summary>
public class FormulaVersion
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key προς τη φόρμουλα
    /// </summary>
    public int FormulaId { get; set; }

    [ForeignKey("FormulaId")]
    public virtual Formula?Formula { get; set; }

    /// <summary>
    /// Αριθμός έκδοσης (1, 2, 3, ...)
    /// </summary>
    [Required]
    public int VersionNumber { get; set; }

    /// <summary>
    /// Περιγραφή των αλλαγών της έκδοσης
    /// </summary>
    [MaxLength(2000)]
    public string? ChangeDescription { get; set; }

    /// <summary>
    /// Τύπος αλλαγής (Major, Minor, Patch, Emergency)
    /// </summary>
    [MaxLength(50)]
    public string? ChangeType { get; set; }

    /// <summary>
    /// Λόγος της αλλαγής
    /// </summary>
    [MaxLength(1000)]
    public string? ChangeReason { get; set; }

    /// <summary>
    /// JSON snapshot ολόκληρης της φόρμουλας σε αυτή την έκδοση
    /// </summary>
    public string VersionData { get; set; } = string.Empty;

    /// <summary>
    /// Χρήστης που δημιούργησε την έκδοση
    /// </summary>
    [MaxLength(256)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Ημερομηνία δημιουργίας έκδοσης
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Κατάσταση έγκρισης της έκδοσης
    /// </summary>
    [MaxLength(50)]
    public string? ApprovalStatus { get; set; }

    /// <summary>
    /// Χρήστης που ενέκρινε την έκδοση
    /// </summary>
    [MaxLength(256)]
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Ημερομηνία έγκρισης
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Υποδεικνύει αν αυτή είναι η τρέχουσα ενεργή έκδοση
    /// </summary>
    public bool IsCurrentVersion { get; set; }

    /// <summary>
    /// Tags για κατηγοριοποίηση εκδόσεων
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// Σχόλια/σημειώσεις για την έκδοση
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Μέγεθος snapshot σε bytes
    /// </summary>
    public long? SnapshotSize { get; set; }

    /// <summary>
    /// Checksum για επαλήθευση ακεραιότητας
    /// </summary>
    [MaxLength(64)]
    public string? Checksum { get; set; }
}

/// <summary>
/// Λεπτομερές snapshot στοιχείων φόρμουλας για σύγκριση εκδόσεων
/// </summary>
public class FormulaVersionDetail
{
    [Key]
    public int Id { get; set; }

    public int FormulaVersionId { get; set; }

    [ForeignKey("FormulaVersionId")]
    public virtual FormulaVersion? FormulaVersion { get; set; }

    /// <summary>
    /// Τύπος στοιχείου (Property, RawMaterial, INCI, Safety, Price, κλπ)
    /// </summary>
    [MaxLength(100)]
    public string DetailType { get; set; } = string.Empty;

    /// <summary>
    /// Όνομα πεδίου που άλλαξε
    /// </summary>
    [MaxLength(200)]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Προηγούμενη τιμή
    /// </summary>
    [MaxLength(2000)]
    public string? OldValue { get; set; }

    /// <summary>
    /// Νέα τιμή
    /// </summary>
    [MaxLength(2000)]
    public string? NewValue { get; set; }

    /// <summary>
    /// Τύπος αλλαγής (Added, Modified, Deleted)
    /// </summary>
    [MaxLength(50)]
    public string? ChangeAction { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Σύγκριση δύο εκδόσεων φόρμουλας
/// </summary>
public class FormulaVersionComparison
{
    public int Id { get; set; }

    public int FormulaId { get; set; }

    public int SourceVersionId { get; set; }

    public int TargetVersionId { get; set; }

    /// <summary>
    /// JSON με τις διαφορές μεταξύ των εκδόσεων
    /// </summary>
    public string DifferencesJson { get; set; } = string.Empty;

    /// <summary>
    /// Σύνοψη διαφορών
    /// </summary>
    [MaxLength(2000)]
    public string? Summary { get; set; }

    public DateTime ComparedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(256)]
    public string ComparedBy { get; set; } = string.Empty;
}
