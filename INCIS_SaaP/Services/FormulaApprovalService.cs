// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Services;

/// <summary>
/// Service για approval workflow φορμούλων
/// </summary>
public class FormulaApprovalService
{
    private readonly INCISDbContext _context;
    private readonly ILogger<FormulaApprovalService> _logger;

    public FormulaApprovalService(INCISDbContext context, ILogger<FormulaApprovalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Υποβολή αίτησης έγκρισης
    /// </summary>
    public async Task<FormulaApproval> SubmitForApprovalAsync(int formulaId, string title, 
        string? description, string approvalType, string priority, int? workflowTemplateId, string userId)
    {
        var formula = await _context.Formulas.FindAsync(formulaId);
        if (formula == null)
            throw new ArgumentException($"Formula with ID {formulaId} not found");

        // Δημιουργία αίτησης
        var approval = new FormulaApproval
        {
            FormulaId = formulaId,
            ApprovalRequestCode = GenerateApprovalCode(),
            Title = title ?? $"Approval Request for {formula.FormulaCode}",
            Description = description,
            ApprovalType = approvalType,
            Priority = priority,
            Status = "Pending",
            CurrentStepNumber = 1,
            SubmittedBy = userId,
            SubmittedAt = DateTime.UtcNow
        };

        _context.Set<FormulaApproval>().Add(approval);
        await _context.SaveChangesAsync();

        // Δημιουργία steps από template ή default workflow
        var steps = await CreateApprovalStepsAsync(approval.Id, workflowTemplateId);
        approval.TotalSteps = steps.Count;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Submitted formula {formulaId} for approval by user {userId}");

        return approval;
    }

    /// <summary>
    /// Έγκριση step
    /// </summary>
    public async Task<bool> ApproveStepAsync(int approvalId, int stepId, string comments, string userId)
    {
        var approval = await _context.Set<FormulaApproval>()
            .Include(a => a.ApprovalSteps)
            .FirstOrDefaultAsync(a => a.Id == approvalId);

        if (approval == null)
            return false;

        var step = approval.ApprovalSteps.FirstOrDefault(s => s.Id == stepId);
        if (step == null)
            return false;

        // Ενημέρωση step
        step.Status = "Approved";
        step.Decision = "Approve";
        step.Comments = comments;
        step.ApprovedBy = userId;
        step.ApprovedAt = DateTime.UtcNow;
        step.DurationHours = (decimal?)((DateTime.UtcNow - (step.StartedAt ?? approval.SubmittedAt)).TotalHours);
        step.LastModified = DateTime.UtcNow;

        // Προώθηση στο επόμενο step
        if (step.StepNumber < approval.TotalSteps)
        {
            approval.CurrentStepNumber = step.StepNumber + 1;
            approval.Status = "InProgress";

            var nextStep = approval.ApprovalSteps.FirstOrDefault(s => s.StepNumber == approval.CurrentStepNumber);
            if (nextStep != null)
            {
                nextStep.Status = "InReview";
                nextStep.StartedAt = DateTime.UtcNow;
            }
        }
        else
        {
            // Τελευταίο  step εγκρίθηκε - ολοκλήρωση approval
            approval.Status = "Approved";
            approval.CompletedAt = DateTime.UtcNow;
            approval.FinalApprover = userId;
            approval.TotalApprovalTimeHours = (decimal?)((DateTime.UtcNow - approval.SubmittedAt).TotalHours);
        }

        approval.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"User {userId} approved step {stepId} of approval {approvalId}");

        return true;
    }

    /// <summary>
    /// Απόρριψη step
    /// </summary>
    public async Task<bool> RejectStepAsync(int approvalId, int stepId, string reason, string userId)
    {
        var approval = await _context.Set<FormulaApproval>()
            .Include(a => a.ApprovalSteps)
            .FirstOrDefaultAsync(a => a.Id == approvalId);

        if (approval == null)
            return false;

        var step = approval.ApprovalSteps.FirstOrDefault(s => s.Id == stepId);
        if (step == null)
            return false;

        // Ενημέρωση step
        step.Status = "Rejected";
        step.Decision = "Reject";
        step.Comments = reason;
        step.ApprovedBy = userId;
        step.ApprovedAt = DateTime.UtcNow;
        step.DurationHours = (decimal?)((DateTime.UtcNow - (step.StartedAt ?? approval.SubmittedAt)).TotalHours);
        step.LastModified = DateTime.UtcNow;

        // Απόρριψη ολόκληρης της αίτησης
        approval.Status = "Rejected";
        approval.CompletedAt = DateTime.UtcNow;
        approval.FinalApprover = userId;
        approval.FinalComments = reason;
        approval.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"User {userId} rejected step {stepId} of approval {approvalId}");

        return true;
    }

    /// <summary>
    /// Προσθήκη σχολίου
    /// </summary>
    public async Task<ApprovalComment> AddCommentAsync(int approvalId, int? stepId, string commentText, 
        string commentType, int? parentCommentId, string userId)
    {
        var comment = new ApprovalComment
        {
            FormulaApprovalId = approvalId,
            ApprovalStepId = stepId,
            CommentText = commentText,
            CommentType = commentType,
            ParentCommentId = parentCommentId,
            CommentedBy = userId,
            CommentedAt = DateTime.UtcNow
        };

        _context.Set<ApprovalComment>().Add(comment);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"User {userId} added comment to approval {approvalId}");

        return comment;
    }

    /// <summary>
    /// Λήψη approvals χρήστη
    /// </summary>
    public async Task<List<FormulaApproval>> GetUserApprovalsAsync(string userId, string? status = null)
    {
        var query = _context.Set<FormulaApproval>()
            .Include(a => a.Formula)
            .Include(a => a.ApprovalSteps)
            .Where(a => a.ApprovalSteps.Any(s => s.AssignedTo == userId || s.ApproverRole == userId));

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        return await query
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Λήψη pending approvals
    /// </summary>
    public async Task<List<FormulaApproval>> GetPendingApprovalsAsync()
    {
        return await _context.Set<FormulaApproval>()
            .Include(a => a.Formula)
            .Include(a => a.ApprovalSteps)
            .Where(a => a.Status == "Pending" || a.Status == "InProgress")
            .OrderBy(a => a.Deadline)
            .ThenByDescending(a => a.Priority == "Critical" ? 4 : 
                                   a.Priority == "High" ? 3 : 
                                   a.Priority == "Medium" ? 2 : 1)
            .ToListAsync();
    }

    /// <summary>
    /// Λήψη overdue approvals
    /// </summary>
    public async Task<List<FormulaApproval>> GetOverdueApprovalsAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Set<FormulaApproval>()
            .Include(a => a.Formula)
            .Include(a => a.ApprovalSteps)
            .Where(a => (a.Status == "Pending" || a.Status == "InProgress") && 
                        a.Deadline.HasValue && a.Deadline.Value < now)
            .ToListAsync();
    }

    /// <summary>
    /// Ακύρωση αίτησης
    /// </summary>
    public async Task<bool> CancelApprovalAsync(int approvalId, string reason, string userId)
    {
        var approval = await _context.Set<FormulaApproval>().FindAsync(approvalId);
        if (approval == null || approval.SubmittedBy != userId)
            return false;

        approval.Status = "Cancelled";
        approval.FinalComments = $"Cancelled by {userId}: {reason}";
        approval.CompletedAt = DateTime.UtcNow;
        approval.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"User {userId} cancelled approval {approvalId}");

        return true;
    }

    /// <summary>
    /// Δημιουργία approval steps
    /// </summary>
    private async Task<List<ApprovalStep>> CreateApprovalStepsAsync(int approvalId, int? templateId = null)
    {
        var steps = new List<ApprovalStep>();

        if (templateId.HasValue)
        {
            // TODO: Load from template
        }
        else
        {
            // Default workflow: R&D → RA → Production
            steps.Add(new ApprovalStep
            {
                FormulaApprovalId = approvalId,
                StepNumber = 1,
                StepName = "R&D Review",
                ApproverRole = "R&D",
                Status = "InReview",
                IsRequired = true,
                StartedAt = DateTime.UtcNow
            });

            steps.Add(new ApprovalStep
            {
                FormulaApprovalId = approvalId,
                StepNumber = 2,
                StepName = "Regulatory Affairs Review",
                ApproverRole = "RA",
                Status = "Pending",
                IsRequired = true
            });

            steps.Add(new ApprovalStep
            {
                FormulaApprovalId = approvalId,
                StepNumber = 3,
                StepName = "Production Review",
                ApproverRole = "Production",
                Status = "Pending",
                IsRequired = true
            });
        }

        foreach (var step in steps)
        {
            _context.Set<ApprovalStep>().Add(step);
        }

        await _context.SaveChangesAsync();

        return steps;
    }

    /// <summary>
    /// Δημιουργία unique approval code
    /// </summary>
    private string GenerateApprovalCode()
    {
        return $"APR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
    }

    /// <summary>
    /// Λήψη statistics
    /// </summary>
    public async Task<Dictionary<string, object>> GetApprovalStatisticsAsync()
    {
        var total = await _context.Set<FormulaApproval>().CountAsync();
        var pending = await _context.Set<FormulaApproval>().CountAsync(a => a.Status == "Pending");
        var inProgress = await _context.Set<FormulaApproval>().CountAsync(a => a.Status == "InProgress");
        var approved = await _context.Set<FormulaApproval>().CountAsync(a => a.Status == "Approved");
        var rejected = await _context.Set<FormulaApproval>().CountAsync(a => a.Status == "Rejected");

        var avgTimeHours = await _context.Set<FormulaApproval>()
            .Where(a => a.TotalApprovalTimeHours.HasValue)
            .AverageAsync(a => a.TotalApprovalTimeHours);

        return new Dictionary<string, object>
        {
            ["Total"] = total,
            ["Pending"] = pending,
            ["InProgress"] = inProgress,
            ["Approved"] = approved,
            ["Rejected"] = rejected,
            ["AverageTimeHours"] = avgTimeHours ?? 0
        };
    }
}
