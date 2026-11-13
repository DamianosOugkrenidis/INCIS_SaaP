// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Services;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// MVC Controller για Formula Version UI
/// </summary>
[Authorize]
public class FormulaVersionController : Controller
{
    private readonly FormulaVersionService _versionService;
    private readonly ILogger<FormulaVersionController> _logger;

    public FormulaVersionController(FormulaVersionService versionService, 
        ILogger<FormulaVersionController> logger)
    {
        _versionService = versionService;
        _logger = logger;
    }

    /// <summary>
    /// Εμφάνιση version history
    /// </summary>
    public async Task<IActionResult> History(int formulaId)
    {
        try
        {
            var versions = await _versionService.GetVersionHistoryAsync(formulaId);
            ViewBag.FormulaId = formulaId;
            return View(versions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading version history: {ex.Message}");
            TempData["Error"] = "Failed to load version history";
            return RedirectToAction("Index", "Formulas");
        }
    }

    /// <summary>
    /// Εμφάνιση version details
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var version = await _versionService.GetVersionAsync(id);
            
            if (version == null)
                return NotFound();

            return View(version);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading version details: {ex.Message}");
            return NotFound();
        }
    }

    /// <summary>
    /// Σύγκριση δύο εκδόσεων
    /// </summary>
    public async Task<IActionResult> Compare(int sourceVersionId, int targetVersionId)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            var comparison = await _versionService.CompareVersionsAsync(sourceVersionId, targetVersionId, userId);
            
            ViewBag.SourceVersion = await _versionService.GetVersionAsync(sourceVersionId);
            ViewBag.TargetVersion = await _versionService.GetVersionAsync(targetVersionId);
            
            return View(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error comparing versions: {ex.Message}");
            TempData["Error"] = "Failed to compare versions";
            return RedirectToAction("Index", "Formulas");
        }
    }

    /// <summary>
    /// Restore confirmation page
    /// </summary>
    [Authorize(Roles = "Admin,SuperUser")]
    public async Task<IActionResult> RestoreConfirm(int formulaId, int versionId)
    {
        var version = await _versionService.GetVersionAsync(versionId);
        
        if (version == null || version.FormulaId != formulaId)
            return NotFound();

        ViewBag.FormulaId = formulaId;
        return View(version);
    }

    /// <summary>
    /// Restore version (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperUser")]
    public async Task<IActionResult> Restore(int formulaId, int versionId)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            var result = await _versionService.RestoreVersionAsync(formulaId, versionId, userId);

            if (result)
            {
                TempData["Success"] = "Version restored successfully";
                return RedirectToAction("History", new { formulaId });
            }
            else
            {
                TempData["Error"] = "Failed to restore version";
                return RedirectToAction("History", new { formulaId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error restoring version: {ex.Message}");
            TempData["Error"] = "An error occurred while restoring version";
            return RedirectToAction("History", new { formulaId });
        }
    }
}
