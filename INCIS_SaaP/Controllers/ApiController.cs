// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using System.Security.Claims;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// Secure API Controller with JWT-like token authentication
/// All endpoints require authentication and rate limiting
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[EnableRateLimiting("Strict")]
public class ApiController : ControllerBase
{
    private readonly INCISDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiController> _logger;

    public ApiController(
        INCISDbContext context, 
        IConfiguration configuration,
        ILogger<ApiController> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint (public)
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [EnableRateLimiting("Login")]
    public IActionResult Health()
    {
        return Ok(new 
        { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    /// <summary>
    /// Get current user info
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        // Mask sensitive data
        return Ok(new
        {
            user.Id,
            user.Email,
            user.FullName,
            user.IsActive,
            Phone = EncryptionService.MaskPhone(user.PhoneNumber ?? ""),
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>
    /// Get all formulas (paginated)
    /// </summary>
    [HttpGet("formulas")]
    public async Task<IActionResult> GetFormulas([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _context.Formulas.AsQueryable();
        var total = await query.CountAsync();
        
        var formulas = await query
            .OrderByDescending(f => f.CreationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new
            {
                f.Id,
                f.FormulaCode,
                f.FormulaName,
                f.Version,
                f.ProductType,
                f.CreationDate,
                f.LastModified
            })
            .ToListAsync();

        return Ok(new
        {
            data = formulas,
            pagination = new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    /// <summary>
    /// Get formula by ID
    /// </summary>
    [HttpGet("formulas/{id}")]
    public async Task<IActionResult> GetFormula(int id)
    {
        var formula = await _context.Formulas
            .Include(f => f.INCIIngredients)
            .Include(f => f.SafetyData)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (formula == null)
            return NotFound(new { message = "Formula not found" });

        // Log access for audit
        _logger.LogInformation("Formula {Id} accessed by user {UserId} at {Time}", 
            id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value, DateTime.UtcNow);

        return Ok(formula);
    }

    /// <summary>
    /// Search formulas by name or code
    /// </summary>
    [HttpGet("formulas/search")]
    public async Task<IActionResult> SearchFormulas([FromQuery] string q, [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Search query is required" });

        if (limit < 1 || limit > 50) limit = 10;

        var results = await _context.Formulas
            .Where(f => f.FormulaName != null && f.FormulaName.Contains(q) || f.FormulaCode.Contains(q))
            .Take(limit)
            .Select(f => new
            {
                f.Id,
                f.FormulaCode,
                f.FormulaName,
                f.ProductType
            })
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>
    /// Get INCI ingredients for a formula
    /// </summary>
    [HttpGet("formulas/{id}/ingredients")]
    public async Task<IActionResult> GetFormulaIngredients(int id)
    {
        var formula = await _context.Formulas
            .Include(f => f.INCIIngredients)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (formula == null)
            return NotFound();

        return Ok(formula.INCIIngredients);
    }

    /// <summary>
    /// Get allergens for a formula
    /// </summary>
    [HttpGet("formulas/{id}/allergens")]
    public async Task<IActionResult> GetFormulaAllergens(int id)
    {
        var allergens = await _context.Allergens
            .Where(a => a.FormulaId == id)
            .ToListAsync();

        return Ok(allergens);
    }

    /// <summary>
    /// Get safety data for a formula
    /// </summary>
    [HttpGet("formulas/{id}/safety")]
    public async Task<IActionResult> GetFormulaSafety(int id)
    {
        var safetyData = await _context.SafetyData
            .Where(s => s.FormulaId == id)
            .ToListAsync();

        return Ok(safetyData);
    }

    /// <summary>
    /// Get natural certifications
    /// </summary>
    [HttpGet("certifications")]
    public async Task<IActionResult> GetCertifications()
    {
        var certifications = await _context.NaturalCertifications
            .OrderByDescending(c => c.CertificationDate)
            .ToListAsync();

        return Ok(certifications);
    }

    /// <summary>
    /// Encrypt sensitive data (for API consumers)
    /// </summary>
    [HttpPost("encrypt")]
    public IActionResult EncryptData([FromBody] EncryptRequest request)
    {
        if (string.IsNullOrEmpty(request.Data))
            return BadRequest(new { message = "Data is required" });

        var encrypted = EncryptionService.Encrypt(request.Data);
        
        return Ok(new 
        { 
            encrypted,
            algorithm = "AES-256-GCM",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Decrypt sensitive data (for API consumers)
    /// </summary>
    [HttpPost("decrypt")]
    public IActionResult DecryptData([FromBody] DecryptRequest request)
    {
        if (string.IsNullOrEmpty(request.Data))
            return BadRequest(new { message = "Data is required" });

        var decrypted = EncryptionService.Decrypt(request.Data);
        
        if (string.IsNullOrEmpty(decrypted))
            return BadRequest(new { message = "Decryption failed" });

        return Ok(new { decrypted });
    }

    /// <summary>
    /// Generate API access token (requires login first)
    /// </summary>
    [HttpPost("token")]
    [EnableRateLimiting("Login")]
    public async Task<IActionResult> GenerateToken([FromBody] TokenRequest request)
    {
        // Ασφάλεια: το endpoint είναι DEMO — ΔΕΝ εκδίδει/επικυρώνει πραγματικά tokens.
        // Προεπιλογή: απενεργοποιημένο (501), ώστε να μην εκλαμβάνεται ως πραγματικός μηχανισμός.
        if (!_configuration.GetValue<bool>("Api:DemoToken:Enabled"))
        {
            _logger.LogWarning("Demo token endpoint invoked but disabled (Api:DemoToken:Enabled=false).");
            return StatusCode(StatusCodes.Status501NotImplemented, new
            {
                error = "NotImplemented",
                message = "Το demo token endpoint είναι απενεργοποιημένο. ΔΕΝ αποτελεί πραγματικό μηχανισμό έκδοσης/επικύρωσης token."
            });
        }
        _logger.LogWarning("Demo token endpoint invoked — DEMO ONLY, not a real auth mechanism.");

        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { message = "Email and password are required" });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !user.IsActive)
            return Unauthorized(new { message = "Invalid credentials" });

        // Note: In production, use proper JWT token generation
        // This is a simplified version for demonstration
        var token = EncryptionService.GenerateSecureToken();
        
        _logger.LogInformation("API token generated for user {Email}", request.Email);

        return Ok(new
        {
            token,
            expiresIn = 3600, // 1 hour
            tokenType = "Bearer"
        });
    }

    /// <summary>
    /// Get security info
    /// </summary>
    [HttpGet("security")]
    [AllowAnonymous]
    public IActionResult GetSecurityInfo()
    {
        return Ok(new
        {
            encryption = "AES-256-GCM",
            keyStorage = "DPAPI (Windows Data Protection)",
            passwordHashing = "PBKDF2-SHA512 (600,000 iterations)",
            rateLimiting = "Enabled",
            cors = "Restricted to same origin",
            hsts = "Enabled in production"
        });
    }
}

// Request/Response DTOs
public class EncryptRequest
{
    public string Data { get; set; } = string.Empty;
}

public class DecryptRequest
{
    public string Data { get; set; } = string.Empty;
}

public class TokenRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
