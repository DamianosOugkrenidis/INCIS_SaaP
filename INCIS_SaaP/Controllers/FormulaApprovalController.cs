// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Services;

namespace INCIS_SaaP.Controllers;

[Authorize]
public class FormulaApprovalController : Controller
{
    private readonly FormulaApprovalService _approvalService;

    public FormulaApprovalController(FormulaApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    public async Task<IActionResult> Dashboard()
    {
        var userId = User.Identity?.Name ?? "System";
        var myApprovals = await _approvalService.GetUserApprovalsAsync(userId);
        var pending = await _approvalService.GetPendingApprovalsAsync();
        
        ViewBag.MyApprovals = myApprovals;
        ViewBag.PendingApprovals = pending;
        
        return View();
    }

    public async Task<IActionResult> MyApprovals(string? status)
    {
        var userId = User.Identity?.Name ?? "System";
        var approvals = await _approvalService.GetUserApprovalsAsync(userId, status);
        return View(approvals);
    }

    public async Task<IActionResult> Details(int id)
    {
        // Implementation details
        await Task.CompletedTask;
        return View();
    }

    [Authorize(Roles = "Admin,SuperUser")]
    public IActionResult Submit(int formulaId)
    {
        ViewBag.FormulaId = formulaId;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveStep(int approvalId, int stepId, string comments)
    {
        var userId = User.Identity?.Name ?? "System";
        await _approvalService.ApproveStepAsync(approvalId, stepId, comments, userId);
        return RedirectToAction("Details", new { id = approvalId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectStep(int approvalId, int stepId, string reason)
    {
        var userId = User.Identity?.Name ?? "System";
        await _approvalService.RejectStepAsync(approvalId, stepId, reason, userId);
        return RedirectToAction("Details", new { id = approvalId });
    }
}
