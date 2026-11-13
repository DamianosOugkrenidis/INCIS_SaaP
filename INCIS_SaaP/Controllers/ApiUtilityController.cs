// Author: Damianos Pythagoras Ougkrenidis
// API Utility Controller για helper endpoints

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using System.Security.Claims;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// API Utility Controller με utility/helper endpoints
/// </summary>
[ApiController]
[Route("api/v1/utility")]
[Authorize]
[EnableRateLimiting("Strict")]
public class ApiUtilityController : ControllerBase
{
    private readonly INCISDbContext _context;
    private readonly ILogger<ApiUtilityController> _logger;

    public ApiUtilityController(INCISDbContext context, ILogger<ApiUtilityController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all product categories
    /// </summary>
    [HttpGet("product-categories")]
    public IActionResult GetProductCategories()
    {
        var categories = ProductCategoryHelper.GetAllCategories();
        
        return Ok(new
        {
            total = categories.Count,
            categories = categories.Select(c => new
            {
                value = c.Value,
                display = c.Display
            })
        });
    }

    /// <summary>
    /// Advanced INCI search - search INCIs and get related raw materials
    /// </summary>
    [HttpGet("inci-search")]
    public async Task<IActionResult> AdvancedINCISearch([FromQuery] string q, [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Search query is required" });

        if (limit < 1 || limit > 50) limit = 10;

        // Search INCIs
        var incis = await _context.INCIs
            .Where(i => i.IsActive && 
                   (i.Name.Contains(q) || 
                    (i.CASNumber != null && i.CASNumber.Contains(q)) ||
                    (i.ECNumber != null && i.ECNumber.Contains(q)) ||
                    (i.Synonyms != null && i.Synonyms.Contains(q))))
            .Take(limit)
            .ToListAsync();

        // For each INCI, get related raw materials
        var results = new List<object>();
        foreach (var inci in incis)
        {
            var rawMaterials = await _context.RawMaterials
                .Include(r => r.Supplier)
                .Where(r => r.IsActive && r.INCIId == inci.Id)
                .Select(r => new
                {
                    r.Id,
                    r.Code,
                    r.ProductName,
                    SupplierName = r.Supplier != null ? r.Supplier.Name : null,
                    r.Category,
                    r.Price
                })
                .ToListAsync();

            // Also search in composition
            var compositionMatches = await _context.Set<RawMaterialComposition>()
                .Include(c => c.RawMaterial)
                .ThenInclude(r => r!.Supplier)
                .Where(c => c.INCIId == inci.Id && c.RawMaterial != null && c.RawMaterial.IsActive)
                .Select(c => new
                {
                    c.RawMaterial!.Id,
                    c.RawMaterial.Code,
                    c.RawMaterial.ProductName,
                    SupplierName = c.RawMaterial.Supplier != null ? c.RawMaterial.Supplier.Name : null,
                    c.RawMaterial.Category,
                    c.RawMaterial.Price,
                    InComposition = true,
                    c.MinPercentage,
                    c.MaxPercentage
                })
                .ToListAsync();

            results.Add(new
            {
                inci = new
                {
                    inci.Id,
                    inci.Name,
                    inci.CASNumber,
                    inci.ECNumber,
                    inci.Function
                },
                rawMaterialsMain = rawMaterials,
                rawMaterialsInComposition = compositionMatches,
                totalRawMaterials = rawMaterials.Count + compositionMatches.Count
            });
        }

        return Ok(new
        {
            query = q,
            total = results.Count,
            results
        });
    }

    /// <summary>
    /// Get system statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var stats = new
        {
            clients = new
            {
                total = await _context.Clients.CountAsync(c => c.IsActive),
                withFormulas = await _context.Clients.CountAsync(c => c.IsActive && c.Formulas.Any())
            },
            suppliers = new
            {
                total = await _context.Suppliers.CountAsync(s => s.IsActive),
                withRawMaterials = await _context.Suppliers.CountAsync(s => s.IsActive && s.RawMaterials.Any())
            },
            incis = new
            {
                total = await _context.INCIs.CountAsync(i => i.IsActive),
                withRestrictions = await _context.INCIs.CountAsync(i => i.IsActive && i.Restrictions.Any())
            },
            rawMaterials = new
            {
                total = await _context.RawMaterials.CountAsync(r => r.IsActive),
                fullyRegistered = await _context.RawMaterials.CountAsync(r => r.IsActive && r.IsFullyRegistered),
                evaluated = await _context.RawMaterials.CountAsync(r => r.IsActive && r.IsEvaluated),
                withSamples = await _context.RawMaterials.CountAsync(r => r.IsActive && r.Samples.Any()),
                used = await _context.FormulaRawMaterials
                    .Where(frm => frm.IsActive)
                    .Select(frm => frm.RawMaterialId)
                    .Distinct()
                    .CountAsync()
            },
            formulas = new
            {
                total = await _context.Formulas.CountAsync(),
                validated = await _context.Formulas.CountAsync(f => f.ValidationDate.HasValue),
                wip = await _context.Formulas.CountAsync(f => f.IsWIP),
                leaveOn = await _context.Formulas.CountAsync(f => f.ProductType == "Leave on"),
                rinseOff = await _context.Formulas.CountAsync(f => f.ProductType == "Rinse off")
            },
            samples = new
            {
                total = await _context.Set<RawMaterialSample>().CountAsync(s => s.IsActive),
                expired = await _context.Set<RawMaterialSample>().CountAsync(s => s.IsActive && s.ExpiryDate < DateTime.Now),
                expiringSoon = await _context.Set<RawMaterialSample>().CountAsync(s => s.IsActive && s.ExpiryDate < DateTime.Now.AddDays(30) && s.ExpiryDate > DateTime.Now)
            },
            restrictions = new
            {
                total = await _context.Restrictions.CountAsync(r => r.IsActive),
                rinseOffOnly = await _context.Restrictions.CountAsync(r => r.IsActive && r.IsRinseOffOnly)
            }
        };

        return Ok(stats);
    }

    /// <summary>
    /// Validate password for protected sections
    /// </summary>
    [HttpPost("validate-password")]
    public async Task<IActionResult> ValidatePassword([FromBody] ValidatePasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var settings = await _context.SystemSettings.FirstOrDefaultAsync();
        if (settings == null)
            return NotFound(new { message = "System settings not found" });

        bool isValid = dto.Section.ToLower() switch
        {
            "rawmaterials" => dto.Password == settings.RawMaterialPassword,
            "formulas" => dto.Password == settings.FormulasPassword,
            _ => false
        };

        if (!isValid)
        {
            _logger.LogWarning("Invalid password attempt for section {Section} by user {UserId}", 
                dto.Section, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }

        return Ok(new { isValid });
    }

    /// <summary>
    /// Set password for protected sections (admin only)
    /// </summary>
    [HttpPost("set-password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var settings = await _context.SystemSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new SystemSettings();
            _context.SystemSettings.Add(settings);
        }

        switch (dto.Section.ToLower())
        {
            case "rawmaterials":
                settings.RawMaterialPassword = dto.Password;
                break;
            case "formulas":
                settings.FormulasPassword = dto.Password;
                break;
            default:
                return BadRequest(new { message = "Invalid section" });
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Password set for section {Section} by user {UserId}", 
            dto.Section, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { message = "Password set successfully" });
    }

    /// <summary>
    /// Get API version and info
    /// </summary>
    [HttpGet("info")]
    [AllowAnonymous]
    public IActionResult GetApiInfo()
    {
        return Ok(new
        {
            version = "1.0.0",
            name = "INCIS SaaP API",
            description = "Cosmetic Formula Management System API",
            endpoints = new
            {
                clients = "/api/v1/clients",
                suppliers = "/api/v1/suppliers",
                incis = "/api/v1/incis",
                rawMaterials = "/api/v1/rawmaterials",
                restrictions = "/api/v1/restrictions",
                formulas = "/api/v1/formulas",
                reports = "/api/v1/reports",
                utility = "/api/v1/utility"
            },
            features = new[]
            {
                "Full CRUD operations",
                "Formula validation workflow",
                "WIP management",
                "Sample management",
                "Supplier change tracking",
                "Natural % calculation (ISO 16128)",
                "Auto water calculation",
                "Advanced INCI search",
                "Excel reports export",
                "Password protection",
                "Product categories",
                "Audit logging"
            }
        });
    }
}

// DTOs for password management
public class ValidatePasswordDto
{
    public string Section { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SetPasswordDto
{
    public string Section { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
