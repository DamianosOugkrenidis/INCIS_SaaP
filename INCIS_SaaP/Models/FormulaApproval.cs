// Author: Damianos Pythagoras Ougkrenidis
//
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INCIS_SaaP.Models;

/// <summary>
/// Workflow έγκρισης φόρμουλας (Multi-level approval)
/// </summary>
public class FormulaApproval
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Φόρμουλα που υποβλήθηκε για έγκριση
    /// </summary>
    public int FormulaId { get; set; }

    [ForeignKey("FormulaId")]
    public virtual Formula? Formula { get; set; }

    /// <summary>
    /// Έκδοση φόρμουλας που υποβλήθηκε
    /// </summary>
    public int? FormulaVersionId { get; set; }

    [ForeignKey("FormulaVersionId")]
    public virtual FormulaVersion? FormulaVersion { get; set; }

    /// <summary>
    /// Κωδικός αίτησης έγκρισης
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ApprovalRequestCode { get; set; } = string.Empty;

    /// <summary>
    /// Τίτλος αίτησης
    /// </summary>
    [MaxLength(300)]
    public string? Title { get; set; }

    /// <summary>
    /// Περιγραφή/αιτιολόγηση αίτησης
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Τύπος έγκρισης (Standard, Emergency, Fast-track)
    /// </summary>
    [MaxLength(50)]
    public string? ApprovalType { get; set; }

    /// <summary>
    /// Προτεραιότητα (Low, Medium, High, Critical)
    /// </summary>
    [MaxLength(50)]
    public string? Priority { get; set; }

    /// <summary>
    /// Τρέχουσα κατάσταση (Pending, InProgress, Approved, Rejected, Cancelled)
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Τρέχον βήμα στο workflow
    /// </summary>
    public int CurrentStepNumber { get; set; } = 1;

    /// <summary>
    /// Σύνολο βημάτων
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Χρήστης που υπέβαλε την αίτηση
    /// </summary>
    [MaxLength(256)]
    public string SubmittedBy { get; set; } = string.Empty;

    /// <summary>
    /// Ημερομηνία υποβολής
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ημερομηνία ολοκλήρωσης
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Deadline για την έγκριση
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// Υπερβολή deadline (σε ώρες)
    /// </summary>
    public int? DeadlineOverdueHours { get; set; }

    /// <summary>
    /// Συνολικός χρόνος έγκρισης (σε ώρες)
    /// </summary>
    public decimal? TotalApprovalTimeHours { get; set; }

    /// <summary>
    /// Σχόλια στην τελική απόφαση
    /// </summary>
    [MaxLength(2000)]
    public string? FinalComments { get; set; }

    /// <summary>
    /// Attachments (paths comma-separated)
    /// </summary>
    [MaxLength(2000)]
    public string? Attachments { get; set; }

    /// <summary>
    /// Τελικός εγκριτής
    /// </summary>
    [MaxLength(256)]
    public string? FinalApprover { get; set; }

    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Βήματα έγκρισης
    /// </summary>
    public virtual ICollection<ApprovalStep> ApprovalSteps { get; set; } = new List<ApprovalStep>();

    /// <summary>
    /// Σχόλια στην αίτηση
    /// </summary>
    public virtual ICollection<ApprovalComment> Comments { get; set; } = new List<ApprovalComment>();
}

/// <summary>
/// Βήμα στο approval workflow (R&amp;D → RA → Production)
/// </summary>
public class ApprovalStep
{
    [Key]
    public int Id { get; set; }

    public int FormulaApprovalId { get; set; }

    [ForeignKey("FormulaApprovalId")]
    public virtual FormulaApproval? FormulaApproval { get; set; }

    /// <summary>
    /// Αριθμός βήματος (1, 2, 3, ...)
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// Όνομα βήματος (R&amp;D Review, RA Review, Production Review, Regulatory Approval, κλπ)
    /// </summary>
    [MaxLength(200)]
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// Τμήμα/ρόλος που εγκρίνει
    /// </summary>
    [MaxLength(100)]
    public string? ApproverRole { get; set; }

    /// <summary>
    /// Συγκεκριμένος χρήστης (αν είναι καθορισμένος)
    /// </summary>
    [MaxLength(256)]
    public string? AssignedTo { get; set; }

    /// <summary>
    /// Κατάσταση βήματος (Pending, InReview, Approved, Rejected, Skipped)
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Απόφαση (Approve, Reject, RequestChanges)
    /// </summary>
    [MaxLength(50)]
    public string? Decision { get; set; }

    /// <summary>
    /// Σχόλια του εγκριτή
    /// </summary>
    [MaxLength(2000)]
    public string? Comments { get; set; }

    /// <summary>
    /// Χρήστης που έδωσε την έγκριση
    /// </summary>
    [MaxLength(256)]
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Ημερομηνία έγκρισης
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Αν το βήμα είναι υποχρεωτικό
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Αν το βήμα μπορεί να παραληφθεί
    /// </summary>
    public bool CanBeSkipped { get; set; }

    /// <summary>
    /// Deadline για το βήμα
    /// </summary>
    public DateTime? StepDeadline { get; set; }

    /// <summary>
    /// Διάρκεια που πήρε το βήμα (σε ώρες)
    /// </summary>
    public decimal? DurationHours { get; set; }

    /// <summary>
    /// Ημερομηνία έναρξης βήματος
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Attachments specific to this step
    /// </summary>
    [MaxLength(1000)]
    public string? Attachments { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastModified { get; set; }
}

/// <summary>
/// Σχόλια/discussion στην αίτηση έγκρισης
/// </summary>
public class ApprovalComment
{
    [Key]
    public int Id { get; set; }

    public int FormulaApprovalId { get; set; }

    [ForeignKey("FormulaApprovalId")]
    public virtual FormulaApproval? FormulaApproval { get; set; }

    /// <summary>
    /// Βήμα στο οποίο αναφέρεται (null = γενικό σχόλιο)
    /// </summary>
    public int? ApprovalStepId { get; set; }

    [ForeignKey("ApprovalStepId")]
    public virtual ApprovalStep? ApprovalStep { get; set; }

    /// <summary>
    /// Κείμενο σχολίου
    /// </summary>
    [Required]
    [MaxLength(4000)]
    public string CommentText { get; set; } = string.Empty;

    /// <summary>
    /// Τύπος σχολίου (Comment, Question, Concern, Suggestion)
    /// </summary>
    [MaxLength(50)]
    public string? CommentType { get; set; }

    /// <summary>
    /// Χρήστης που έκανε το σχόλιο
    /// </summary>
    [MaxLength(256)]
    public string CommentedBy { get; set; } = string.Empty;

    public DateTime CommentedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Parent comment (για replies/threading)
    /// </summary>
    public int? ParentCommentId { get; set; }

    [ForeignKey("ParentCommentId")]
    public virtual ApprovalComment? ParentComment { get; set; }

    public virtual ICollection<ApprovalComment> Replies { get; set; } = new List<ApprovalComment>();

    /// <summary>
    /// Αν το σχόλιο έχει διευθετηθεί
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// Χρήστης που έλυσε το θέμα
    /// </summary>
    [MaxLength(256)]
    public string? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Attachments
    /// </summary>
    [MaxLength(1000)]
    public string? Attachments { get; set; }

    public DateTime? LastModified { get; set; }
}

/// <summary>
/// Προκαθορισμένα workflow templates
/// </summary>
public class ApprovalWorkflowTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string TemplateName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Τύπος προϊόντος για τον οποίο ισχύει
    /// </summary>
    [MaxLength(100)]
    public string? ProductType { get; set; }

    /// <summary>
    /// Τύπος φόρμουλας (New, Modification, Critical)
    /// </summary>
    [MaxLength(50)]
    public string? FormulaType { get; set; }

    /// <summary>
    /// JSON με τα steps του workflow
    /// </summary>
    public string WorkflowStepsJson { get; set; } = string.Empty;

    /// <summary>
    /// Εκτιμώμενος χρόνος ολοκλήρωσης (σε ώρες)
    /// </summary>
    public int? EstimatedCompletionHours { get; set; }

    public bool IsDefault { get; set; }
    
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(256)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? LastModified { get; set; }
}
