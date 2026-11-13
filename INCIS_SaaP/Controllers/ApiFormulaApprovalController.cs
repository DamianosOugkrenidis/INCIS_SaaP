// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Services;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// API Controller για formula approval workflow
/// </summary>
[Authorize]
[ApiController]
[Route("api/formula-approvals")]
[Produces("application/json")]
public class ApiFormulaApprovalController : ControllerBase
{
    private readonly FormulaApprovalService _approvalService;
    private readonly ILogger<ApiFormulaApprovalController> _logger;

    public ApiFormulaApprovalController(FormulaApprovalService approvalService, 
        ILogger<ApiFormulaApprovalController> logger)
    {
        _approvalService = approvalService;
        _logger = logger;
    }

    /// <summary>
    /// Λήψη approvals χρήστη
    /// </summary>
    [HttpGet("my-approvals")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyApprovals([FromQuery] string? status = null)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            var approvals = await _approvalService.GetUserApprovalsAsync(userId, status);
            
            return Ok(new { success = true, data = approvals, count = approvals.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting user approvals: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Λήψη pending approvals
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingApprovals()
    {
        try
        {
            var approvals = await _approvalService.GetPendingApprovalsAsync();
            return Ok(new { success = true, data = approvals, count = approvals.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting pending approvals: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Λήψη overdue approvals
    /// </summary>
    [HttpGet("overdue")]
    [Authorize(Roles = "Admin,SuperUser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdueApprovals()
    {
        try
        {
            var approvals = await _approvalService.GetOverdueApprovalsAsync();
            return Ok(new { success = true, data = approvals, count = approvals.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting overdue approvals: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Υποβολή φόρμουλας για έγκριση
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperUser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitForApproval([FromBody] SubmitApprovalRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            
            var approval = await _approvalService.SubmitForApprovalAsync(
                request.FormulaId,
                request.Title ?? "",
                request.Description,
                request.ApprovalType ?? "Standard",
                request.Priority ?? "Medium",
                request.WorkflowTemplateId,
                userId
            );

            return CreatedAtAction(nameof(GetMyApprovals), 
                new { id = approval.Id }, 
                new { success = true, data = approval });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error submitting approval: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Έγκριση step
    /// </summary>
    [HttpPut("{approvalId}/steps/{stepId}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveStep(int approvalId, int stepId, 
        [FromBody] ApproveStepRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            var result = await _approvalService.ApproveStepAsync(
                approvalId, 
                stepId, 
                request.Comments ?? "", 
                userId
            );

            if (result)
                return Ok(new { success = true, message = "Step approved successfully" });
            else
                return BadRequest(new { success = false, message = "Failed to approve step" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error approving step: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Απόρριψη step
    /// </summary>
    [HttpPut("{approvalId}/steps/{stepId}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectStep(int approvalId, int stepId, 
        [FromBody] RejectStepRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            var result = await _approvalService.RejectStepAsync(
                approvalId, 
                stepId, 
                request.Reason ?? "", 
                userId
            );

            if (result)
                return Ok(new { success = true, message = "Step rejected" });
            else
                return BadRequest(new { success = false, message = "Failed to reject step" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error rejecting step: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Προσθήκη σχολίου
    /// </summary>
    [HttpPost("{approvalId}/comments")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddComment(int approvalId, 
        [FromBody] AddCommentRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            
            var comment = await _approvalService.AddCommentAsync(
                approvalId,
                request.StepId,
                request.CommentText,
                request.CommentType ?? "Comment",
                request.ParentCommentId,
                userId
            );

            return CreatedAtAction(nameof(GetMyApprovals), 
                new { id = comment.Id }, 
                new { success = true, data = comment });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding comment: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Ακύρωση αίτησης
    /// </summary>
    [HttpPut("{approvalId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelApproval(int approvalId, 
        [FromBody] CancelApprovalRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            var result = await _approvalService.CancelApprovalAsync(
                approvalId, 
                request.Reason ?? "", 
                userId
            );

            if (result)
                return Ok(new { success = true, message = "Approval cancelled successfully" });
            else
                return BadRequest(new { success = false, message = "Failed to cancel approval" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error cancelling approval: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Λήψη statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var statistics = await _approvalService.GetApprovalStatisticsAsync();
            return Ok(new { success = true, data = statistics });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting statistics: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

/// <summary>
/// DTOs
/// </summary>
public record SubmitApprovalRequest(
    int FormulaId,
    string? Title,
    string? Description,
    string? ApprovalType,
    string? Priority,
    int? WorkflowTemplateId
);

public record ApproveStepRequest(string? Comments);

public record RejectStepRequest(string? Reason);

public record AddCommentRequest(
    int? StepId,
    string CommentText,
    string? CommentType,
    int? ParentCommentId
);

public record CancelApprovalRequest(string? Reason);
