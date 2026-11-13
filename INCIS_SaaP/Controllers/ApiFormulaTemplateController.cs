// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Services;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// API Controller για Formula Templates
/// </summary>
[Authorize]
[ApiController]
[Route("api/formula-templates")]
[Produces("application/json")]
public class ApiFormulaTemplateController : ControllerBase
{
    private readonly FormulaTemplateService _templateService;
    private readonly ILogger<ApiFormulaTemplateController> _logger;

    public ApiFormulaTemplateController(FormulaTemplateService templateService, 
        ILogger<ApiFormulaTemplateController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Λήψη όλων των templates
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] int? categoryId,
        [FromQuery] string? productType,
        [FromQuery] bool? isPublic)
    {
        try
        {
            var templates = await _templateService.GetTemplatesAsync(categoryId, productType, isPublic);
            return Ok(new { success = true, data = templates, count = templates.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting templates: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Αναζήτηση templates
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchTemplates([FromQuery] string searchTerm)
    {
        try
        {
            var templates = await _templateService.SearchTemplatesAsync(searchTerm);
            return Ok(new { success = true, data = templates, count = templates.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error searching templates: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Top rated templates
    /// </summary>
    [HttpGet("top-rated")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopRated([FromQuery] int count = 10)
    {
        try
        {
            var templates = await _templateService.GetTopRatedTemplatesAsync(count);
            return Ok(new { success = true, data = templates, count = templates.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting top-rated templates: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Most used templates
    /// </summary>
    [HttpGet("most-used")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMostUsed([FromQuery] int count = 10)
    {
        try
        {
            var templates = await _templateService.GetMostUsedTemplatesAsync(count);
            return Ok(new { success = true, data = templates, count = templates.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting most-used templates: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Δημιουργία template από φόρμουλα
    /// </summary>
    [HttpPost("from-formula/{formulaId}")]
    [Authorize(Roles = "Admin,SuperUser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateFromFormula(int formulaId, 
        [FromBody] CreateTemplateRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            
            var template = await _templateService.CreateTemplateFromFormulaAsync(
                formulaId,
                request.TemplateName,
                request.TemplateCode,
                request.CategoryId,
                userId
            );

            return CreatedAtAction(nameof(GetTemplates), 
                new { id = template.Id }, 
                new { success = true, data = template });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating template from formula {formulaId}: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Εφαρμογή template σε φόρμουλα
    /// </summary>
    [HttpPost("{templateId}/apply")]
    [Authorize(Roles = "Admin,SuperUser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> ApplyTemplate(int templateId, 
        [FromBody] ApplyTemplateRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            
            var formula = await _templateService.ApplyTemplateAsync(
                templateId,
                request.NewFormulaCode,
                request.ClientId,
                userId
            );

            return CreatedAtAction("GetFormula", "Formulas", 
                new { id = formula.Id }, 
                new { success = true, data = formula });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error applying template {templateId}: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Βαθμολόγηση template
    /// </summary>
    [HttpPost("{templateId}/rate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RateTemplate(int templateId, 
        [FromBody] RateTemplateRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            var result = await _templateService.RateTemplateAsync(templateId, request.Rating, userId);

            if (result)
                return Ok(new { success = true, message = "Template rated successfully" });
            else
                return BadRequest(new { success = false, message = "Failed to rate template" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error rating template {templateId}: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Διαγραφή template
    /// </summary>
    [HttpDelete("{templateId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteTemplate(int templateId)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            var result = await _templateService.DeleteTemplateAsync(templateId, userId);

            if (result)
                return Ok(new { success = true, message = "Template deleted successfully" });
            else
                return NotFound(new { success = false, message = "Template not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting template {templateId}: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Λήψη δέντρου κατηγοριών
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var categories = await _templateService.GetCategoryTreeAsync();
            return Ok(new { success = true, data = categories });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting categories: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Δημιουργία κατηγορίας
    /// </summary>
    [HttpPost("categories")]
    [Authorize(Roles = "Admin,SuperUser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "System";
            
            var category = await _templateService.CreateCategoryAsync(
                request.Name,
                request.Description,
                request.ParentCategoryId,
                userId
            );

            return CreatedAtAction(nameof(GetCategories), 
                new { id = category.Id }, 
                new { success = true, data = category });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating category: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

/// <summary>
/// DTOs
/// </summary>
public record CreateTemplateRequest(
    string TemplateName,
    string TemplateCode,
    int? CategoryId
);

public record ApplyTemplateRequest(
    string NewFormulaCode,
    int? ClientId
);

public record RateTemplateRequest(int Rating);

public record CreateCategoryRequest(
    string Name,
    string? Description,
    int? ParentCategoryId
);
