// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Services;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// API Controller για Formula Version Control
/// </summary>
[Authorize]
[ApiController]
[Route("api/formulas/{formulaId}/versions")]
[Produces("application/json")]
public class ApiFormulaVersionController : ControllerBase
{
    private readonly FormulaVersionService _versionService;
    private readonly ILogger<ApiFormulaVersionController> _logger;

    public ApiFormulaVersionController(FormulaVersionService versionService, 
        ILogger<ApiFormulaVersionController> logger)
    {
        _versionService = versionService;
        _logger = logger;
    }

    /// <summary>
    /// Λήψη όλων των εκδόσεων μιας φόρμουλας
    /// </summary>
    /// <param name="formulaId">Formula ID</param>
    /// <returns>List of versions</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersionHistory(int formulaId)
    {
        try
        {
            var versions = await _versionService.GetVersionHistoryAsync(formulaId);
            return Ok(new { success = true, data = versions, count = versions.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting version history for formula {formulaId}: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Λήψη συγκεκριμένης έκδοσης
    /// </summary>
    [HttpGet("{versionId}", Name = "GetVersion")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersion(int formulaId, int versionId)
    {
        try
        {
            var version = await _versionService.GetVersionAsync(versionId);
            
            if (version == null || version.FormulaId != formulaId)
                return NotFound(new { success = false, message = "Version not found" });

            return Ok(new { success = true, data = version });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting version {versionId}: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Δημιουργία νέας έκδοσης
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperUser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateVersion(int formulaId, 
        [FromBody] CreateVersionRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            
            var version = await _versionService.CreateVersionAsync(
                formulaId,
                request.ChangeDescription ?? "",
                request.ChangeType ?? "Minor",
                request.ChangeReason ?? "",
                userId
            );

            return CreatedAtRoute("GetVersion", 
                new { formulaId, versionId = version.Id }, 
                new { success = true, data = version });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating version for formula {formulaId}: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Σύγκριση δύο εκδόσεων
    /// </summary>
    [HttpGet("compare")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CompareVersions(int formulaId, 
        [FromQuery] int sourceVersionId, [FromQuery] int targetVersionId)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            var comparison = await _versionService.CompareVersionsAsync(sourceVersionId, targetVersionId, userId);

            return Ok(new { success = true, data = comparison });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error comparing versions: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Επαναφορά σε προηγούμενη έκδοση
    /// </summary>
    [HttpPost("{versionId}/restore")]
    [Authorize(Roles = "Admin,SuperUser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RestoreVersion(int formulaId, int versionId)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            var result = await _versionService.RestoreVersionAsync(formulaId, versionId, userId);

            if (result)
                return Ok(new { success = true, message = "Version restored successfully" });
            else
                return BadRequest(new { success = false, message = "Failed to restore version" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error restoring version {versionId}: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Έγκριση έκδοσης
    /// </summary>
    [HttpPut("{versionId}/approve")]
    [Authorize(Roles = "Admin,SuperUser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveVersion(int formulaId, int versionId)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            var result = await _versionService.ApproveVersionAsync(versionId, userId);

            if (result)
                return Ok(new { success = true, message = "Version approved successfully" });
            else
                return BadRequest(new { success = false, message = "Failed to approve version" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error approving version {versionId}: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Απόρριψη έκδοσης
    /// </summary>
    [HttpPut("{versionId}/reject")]
    [Authorize(Roles = "Admin,SuperUser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectVersion(int formulaId, int versionId, 
        [FromBody] RejectVersionRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            var result = await _versionService.RejectVersionAsync(versionId, userId, request.Reason ?? "");

            if (result)
                return Ok(new { success = true, message = "Version rejected" });
            else
                return BadRequest(new { success = false, message = "Failed to reject version" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error rejecting version {versionId}: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Επαλήθευση ακεραιότητας έκδοσης
    /// </summary>
    [HttpGet("{versionId}/verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyIntegrity(int formulaId, int versionId)
    {
        try
        {
            var version = await _versionService.GetVersionAsync(versionId);
            
            if (version == null || version.FormulaId != formulaId)
                return NotFound(new { success = false, message = "Version not found" });

            var isValid = _versionService.VerifyVersionIntegrity(version);
            
            return Ok(new 
            { 
                success = true, 
                isValid, 
                message = isValid ? "Version integrity verified" : "Version integrity check failed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error verifying version {versionId}: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

/// <summary>
/// DTO για δημιουργία έκδοσης
/// </summary>
public record CreateVersionRequest(
    string? ChangeDescription,
    string? ChangeType,
    string? ChangeReason
);

/// <summary>
/// DTO για απόρριψη έκδοσης
/// </summary>
public record RejectVersionRequest(string? Reason);
