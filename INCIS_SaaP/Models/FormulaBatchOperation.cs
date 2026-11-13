// Author: Damianos Pythagoras Ougkrenidis
//
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models;

/// <summary>
/// Μαζικές λειτουργίες (batch operations) στις φόρμουλες
/// </summary>
public class BatchOperation
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Κωδικός batch operation
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string OperationCode { get; set; } = string.Empty;

    /// <summary>
    /// Τύπος λειτουργίας (BulkValidate, BulkChangeSupplier, BulkUpdatePrices, BulkArchive, BulkExport, BulkApprove)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Τίτλος/περιγραφή
    /// </summary>
    [MaxLength(500)]
    public string? Title { get; set; }

    /// <summary>
    /// Λεπτομερής περιγραφή
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Κατάσταση (Pending, Running, Completed, Failed, Cancelled, PartiallyCompleted)
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Πλήθος φορμούλων να επεξεργαστεί
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Πλήθος επεξεργασμένων
    /// </summary>
    public int ProcessedItems { get; set; }

    /// <summary>
    /// Πλήθος επιτυχών
    /// </summary>
    public int SuccessfulItems { get; set; }

    /// <summary>
    /// Πλήθος που απέτυχαν
    /// </summary>
    public int FailedItems { get; set; }

    /// <summary>
    /// Ποσοστό ολοκλήρωσης (0-100)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal ProgressPercentage { get; set; }

    /// <summary>
    /// Παράμετροι λειτουργίας (JSON)
    /// </summary>
    public string? OperationParameters { get; set; }

    /// <summary>
    /// Αποτελέσματα (JSON summary)
    /// </summary>
    public string? Results { get; set; }

    /// <summary>
    /// Errors που προέκυψαν
    /// </summary>
    public string? Errors { get; set; }

    /// <summary>
    /// Προτεραιότητα (Low, Medium, High)
    /// </summary>
    [MaxLength(50)]
    public string? Priority { get; set; }

    /// <summary>
    /// Χρήστης που ξεκίνησε τη λειτουργία
    /// </summary>
    [MaxLength(256)]
    public string InitiatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Ημερομηνία έναρξης
    /// </summary>
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ημερομηνία ολοκλήρωσης
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Διάρκεια σε δευτερόλεπτα
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Εκτιμώμενος χρόνος ολοκλήρωσης
    /// </summary>
    public DateTime? EstimatedCompletionTime { get; set; }

    /// <summary>
    /// Αν επιτρέπεται rollback
    /// </summary>
    public bool CanRollback { get; set; }

    /// <summary>
    /// Αν έχει γίνει rollback
    /// </summary>
    public bool IsRolledBack { get; set; }

    /// <summary>
    /// Ημερομηνία rollback
    /// </summary>
    public DateTime? RolledBackAt { get; set; }

    /// <summary>
    /// Χρήστης που έκανε rollback
    /// </summary>
    [MaxLength(256)]
    public string? RolledBackBy { get; set; }

    /// <summary>
    /// Αποθηκευμένο snapshot πριν τη λειτουργία (για rollback)
    /// </summary>
    public string? PreOperationSnapshot { get; set; }

    /// <summary>
    /// Logs της λειτουργίας
    /// </summary>
    public string? OperationLogs { get; set; }

    /// <summary>
    /// Αν η λειτουργία τρέχει στο background
    /// </summary>
    public bool IsBackgroundJob { get; set; } = true;

    /// <summary>
    /// Job ID (αν τρέχει σε Hangfire ή άλλο job scheduler)
    /// </summary>
    [MaxLength(200)]
    public string? JobId { get; set; }

    /// <summary>
    /// Αν έχει σταλεί notification στον χρήστη
    /// </summary>
    public bool NotificationSent { get; set; }

    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Items που επηρεάζονται από τη λειτουργία
    /// </summary>
    public virtual ICollection<BatchOperationItem> Items { get; set; } = new List<BatchOperationItem>();
}

/// <summary>
/// Επιμέρους στοιχείο batch operation (μια φόρμουλα)
/// </summary>
public class BatchOperationItem
{
    [Key]
    public int Id { get; set; }

    public int BatchOperationId { get; set; }

    [ForeignKey("BatchOperationId")]
    public virtual BatchOperation? BatchOperation { get; set; }

    /// <summary>
    /// Φόρμουλα που επηρεάζεται
    /// </summary>
    public int FormulaId { get; set; }

    [ForeignKey("FormulaId")]
    public virtual Formula? Formula { get; set; }

    /// <summary>
    /// Σειρά επεξεργασίας
    /// </summary>
    public int ProcessingOrder { get; set; }

    /// <summary>
    /// Κατάσταση (Pending, Processing, Success, Failed, Skipped)
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Αποτέλεσμα επεξεργασίας
    /// </summary>
    [MaxLength(2000)]
    public string? Result { get; set; }

    /// <summary>
    /// Error message αν απέτυχε
    /// </summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Snapshot πριν την αλλαγή (για rollback)
    /// </summary>
    public string? PreChangeSnapshot { get; set; }

    /// <summary>
    /// Snapshot μετά την αλλαγή
    /// </summary>
    public string? PostChangeSnapshot { get; set; }

    /// <summary>
    /// Ημερομηνία επεξεργασίας
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Διάρκεια επεξεργασίας σε milliseconds
    /// </summary>
    public int? ProcessingDurationMs { get; set; }

    /// <summary>
    /// Αν έχει γίνει rollback
    /// </summary>
    public bool IsRolledBack { get; set; }

    /// <summary>
    /// Retry attempts
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Μέγιστο πλήθος retries
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Επόμενη προσπάθεια retry
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Scheduled batch operations (προγραμματισμένες μαζικές λειτουργίες)
/// </summary>
public class ScheduledBatchOperation
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string ScheduleName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Τύπος λειτουργίας
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Παράμετροι λειτουργίας (JSON)
    /// </summary>
    public string? OperationParameters { get; set; }

    /// <summary>
    /// Cron expression για το schedule (π.χ. "0 2 * * *" για κάθε μέρα στις 2πμ)
    /// </summary>
    [MaxLength(100)]
    public string? CronExpression { get; set; }

    /// <summary>
    /// Επόμενη προγραμματισμένη εκτέλεση
    /// </summary>
    public DateTime? NextRun { get; set; }

    /// <summary>
    /// Τελευταία εκτέλεση
    /// </summary>
    public DateTime? LastRun { get; set; }

    /// <summary>
    /// Κατάσταση τελευταίας εκτέλεσης
    /// </summary>
    [MaxLength(50)]
    public string? LastRunStatus { get; set; }

    /// <summary>
    /// ID τελευταίου batch operation
    /// </summary>
    public int? LastBatchOperationId { get; set; }

    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Αν θα στέλνει email notification
    /// </summary>
    public bool SendNotification { get; set; } = true;

    /// <summary>
    /// Email recipients (comma-separated)
    /// </summary>
    [MaxLength(1000)]
    public string? NotificationRecipients { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(256)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? LastModified { get; set; }
}

/// <summary>
/// Batch operation templates (για γρήγορη εκτέλεση συχνών λειτουργιών)
/// </summary>
public class BatchOperationTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string TemplateName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Προκαθορισμένες παράμετροι (JSON)
    /// </summary>
    public string? DefaultParameters { get; set; }

    /// <summary>
    /// Κριτήρια επιλογής φορμούλων (JSON)
    /// </summary>
    public string? SelectionCriteria { get; set; }

    /// <summary>
    /// Πλήθος χρήσεων
    /// </summary>
    public int UsageCount { get; set; }

    public DateTime? LastUsed { get; set; }

    public bool IsPublic { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(256)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? LastModified { get; set; }
}
