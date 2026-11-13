// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Services;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// API Controller για Formula Cloning
/// </summary>
[Authorize]
[ApiController]
[Route("api/formulas/{formulaId}/clone")]
[Produces("application/json")]
public class ApiFormulaCloneController : ControllerBase
{
    private readonly FormulaCloneService _cloneService;
    private readonly ILogger<ApiFormulaCloneController> _logger;

    public ApiFormulaCloneController(FormulaCloneService cloneService, 
        ILogger<ApiFormulaCloneController> logger)
    {
        _cloneService = cloneService;
        _logger = logger;
    }

    /// <summary>
    /// Κλωνοποίηση φόρμουλας
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperUser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CloneFormula(int formulaId, 
        [FromBody] CloneFormulaRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            
            var clonedFormula = await _cloneService.CloneFormulaAsync(
                formulaId,
                request.NewFormulaCode,
                userId
            );

            return CreatedAtAction("GetFormula", "Formulas", 
                new { id = clonedFormula.Id }, 
                new { success = true, data = clonedFormula });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error cloning formula {formulaId}: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Κλωνοποίηση σε διαφορετικό πελάτη
    /// </summary>
    [HttpPost("to-client")]
    [Authorize(Roles = "Admin,SuperUser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CloneToClient(int formulaId, 
        [FromBody] CloneToClientRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            
            var clonedFormula = await _cloneService.CloneFormulaToClientAsync(
                formulaId,
                request.NewFormulaCode,
                request.NewClientId,
                userId
            );

            return CreatedAtAction("GetFormula", "Formulas", 
                new { id = clonedFormula.Id }, 
                new { success = true, data = clonedFormula });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error cloning formula {formulaId} to client: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Κλωνοποίηση με τροποποιήσεις
    /// </summary>
    [HttpPost("with-modifications")]
    [Authorize(Roles = "Admin,SuperUser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CloneWithModifications(int formulaId, 
        [FromBody] CloneWithModificationsRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            
            var clonedFormula = await _cloneService.CloneFormulaWithModificationsAsync(
                formulaId,
                request.NewFormulaCode,
                request.Modifications ?? new Dictionary<string, object>(),
                userId
            );

            return CreatedAtAction("GetFormula", "Formulas", 
                new { id = clonedFormula.Id }, 
                new { success = true, data = clonedFormula });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error cloning formula {formulaId} with modifications: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Έλεγχος διαθεσιμότητας formula code
    /// </summary>
    [HttpGet("check-code")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckCodeAvailability([FromQuery] string formulaCode)
    {
        try
        {
            var exists = await _cloneService.CheckFormulaCodeExistsAsync(formulaCode);
            
            return Ok(new 
            { 
                success = true, 
                available = !exists,
                message = exists ? "Code already exists" : "Code is available"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking code availability: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Δημιουργία unique formula code
    /// </summary>
    [HttpGet("generate-code")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateCode([FromQuery] string baseCode)
    {
        try
        {
            var uniqueCode = await _cloneService.GenerateUniqueFormulaCodeAsync(baseCode);
            
            return Ok(new { success = true, code = uniqueCode });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating unique code: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

/// <summary>
/// DTOs
/// </summary>
public record CloneFormulaRequest(string NewFormulaCode);

public record CloneToClientRequest(string NewFormulaCode, int NewClientId);

public record CloneWithModificationsRequest(
    string NewFormulaCode, 
    Dictionary<string, object>? Modifications
);
