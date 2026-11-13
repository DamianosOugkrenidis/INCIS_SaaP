// Author: Damianos Pythagoras Ougkrenidis
//
// CosIng API Controller - Παρέχει δεδομένα CosIng σε όλες τις φόρμες του συστήματος
// Χρησιμοποιείται για autocomplete, validation, και regulatory checks
// Database: INCIS_SaaP - Tables: CosIngEntries, CosIngIngredients

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// API Controller για CosIng Database (EU Regulation 1223/2009)
/// Endpoints για αναζήτηση, autocomplete, και regulatory compliance checks
/// Χρησιμοποιείται από όλες τις φόρμες δημιουργίας/επεξεργασίας φόρμουλας
/// </summary>
[ApiController]
[Route("api/cosing")]
[Authorize]
public class ApiCosIngController : ControllerBase
{
    private readonly INCISDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ApiCosIngController> _logger;

    public ApiCosIngController(INCISDbContext context, IMemoryCache cache, ILogger<ApiCosIngController> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    // ============================================================
    // AUTOCOMPLETE ENDPOINTS - Για χρήση σε όλες τις φόρμες
    // ============================================================

    /// <summary>
    /// Αναζήτηση INCI Ingredients με autocomplete
    /// Χρήση: Κατά τη δημιουργία φόρμουλας, προσθήκη συστατικών
    /// </summary>
    [HttpGet("ingredients/search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchIngredients([FromQuery] string q, [FromQuery] int limit = 15)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new List<object>());

        if (limit < 1 || limit > 100) limit = 15;

        var cacheKey = $"cosing_ing_search_{q.ToLower()}_{limit}";
        if (_cache.TryGetValue(cacheKey, out object? cached))
            return Ok(cached);

        var results = await _context.CosIngIngredients
            .Where(i => i.InciName.Contains(q) 
                     || (i.CASNumber != null && i.CASNumber.Contains(q))
                     || (i.ECNumber != null && i.ECNumber.Contains(q)))
            .OrderBy(i => i.InciName)
            .Take(limit)
            .Select(i => new
            {
                i.Id,
                i.InciName,
                i.CASNumber,
                i.ECNumber,
                i.Function,
                i.Restriction,
                i.CosIngRefNo
            })
            .ToListAsync();

        _cache.Set(cacheKey, results, TimeSpan.FromMinutes(10));
        return Ok(results);
    }

    /// <summary>
    /// Λήψη λεπτομερειών ενός INCI ingredient
    /// </summary>
    [HttpGet("ingredients/{id}")]
    public async Task<IActionResult> GetIngredient(int id)
    {
        var ingredient = await _context.CosIngIngredients.FindAsync(id);
        if (ingredient == null) return NotFound();
        return Ok(ingredient);
    }

    /// <summary>
    /// Λήψη λίστας functions (λειτουργιών) - για dropdown/filter
    /// </summary>
    [HttpGet("functions")]
    public async Task<IActionResult> GetFunctions()
    {
        var cacheKey = "cosing_functions_list";
        if (_cache.TryGetValue(cacheKey, out object? cached))
            return Ok(cached);

        var functions = await _context.CosIngIngredients
            .Where(i => i.Function != null && i.Function != "")
            .Select(i => i.Function!)
            .Distinct()
            .OrderBy(f => f)
            .Take(200)
            .ToListAsync();

        // Split compound functions (e.g. "SKIN CONDITIONING, EMOLLIENT")
        var allFunctions = functions
            .SelectMany(f => f.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(f => f.Trim())
            .Distinct()
            .OrderBy(f => f)
            .ToList();

        _cache.Set(cacheKey, allFunctions, TimeSpan.FromHours(1));
        return Ok(allFunctions);
    }

    /// <summary>
    /// Αναζήτηση ingredients κατά function
    /// </summary>
    [HttpGet("ingredients/by-function")]
    public async Task<IActionResult> GetByFunction([FromQuery] string function, [FromQuery] int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(function)) return BadRequest("function parameter required");

        var results = await _context.CosIngIngredients
            .Where(i => i.Function != null && i.Function.Contains(function))
            .OrderBy(i => i.InciName)
            .Take(limit)
            .Select(i => new
            {
                i.Id,
                i.InciName,
                i.CASNumber,
                i.Function
            })
            .ToListAsync();

        return Ok(results);
    }

    // ============================================================
    // REGULATORY CHECK ENDPOINTS - Έλεγχος συμμόρφωσης
    // ============================================================

    /// <summary>
    /// Έλεγχος αν μια ουσία είναι ΑΠΑΓΟΡΕΥΜΕΝΗ (Annex II)
    /// Χρήση: Real-time validation κατά την προσθήκη ingredient σε φόρμουλα
    /// </summary>
    [HttpGet("check/prohibited")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckProhibited([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("q parameter required");

        var prohibited = await _context.CosIngEntries
            .Where(e => e.Annex == "II" && (
                (e.ChemicalName != null && e.ChemicalName.Contains(q)) ||
                (e.InciName != null && e.InciName.Contains(q)) ||
                (e.CASNumber != null && e.CASNumber.Contains(q))
            ))
            .Select(e => new
            {
                e.Id,
                e.EntryNumber,
                e.ChemicalName,
                e.InciName,
                e.CASNumber,
                e.CMR,
                e.UpdateDate,
                IsProhibited = true,
                Annex = "II",
                Message = "ΑΠΑΓΟΡΕΥΜΕΝΗ ΟΥΣΙΑ - Annex II, Regulation (EC) 1223/2009"
            })
            .ToListAsync();

        return Ok(new
        {
            query = q,
            isProhibited = prohibited.Any(),
            count = prohibited.Count,
            results = prohibited
        });
    }

    /// <summary>
    /// Έλεγχος αν μια ουσία έχει ΠΕΡΙΟΡΙΣΜΟΥΣ (Annex III)
    /// Επιστρέφει μέγιστη συγκέντρωση, προϋποθέσεις χρήσης, κ.λπ.
    /// </summary>
    [HttpGet("check/restricted")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckRestricted([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("q parameter required");

        var restricted = await _context.CosIngEntries
            .Where(e => e.Annex == "III" && (
                (e.ChemicalName != null && e.ChemicalName.Contains(q)) ||
                (e.InciName != null && e.InciName.Contains(q)) ||
                (e.CASNumber != null && e.CASNumber.Contains(q))
            ))
            .Select(e => new
            {
                e.Id,
                e.EntryNumber,
                e.ChemicalName,
                e.InciName,
                e.CASNumber,
                e.ProductType,
                e.MaxConcentration,
                e.Warnings,
                e.OtherRestrictions,
                e.CMR,
                e.UpdateDate,
                IsRestricted = true,
                Annex = "III",
                Message = "ΟΥΣΙΑ ΜΕ ΠΕΡΙΟΡΙΣΜΟΥΣ - Annex III, Regulation (EC) 1223/2009"
            })
            .ToListAsync();

        return Ok(new
        {
            query = q,
            isRestricted = restricted.Any(),
            count = restricted.Count,
            results = restricted
        });
    }

    /// <summary>
    /// Πλήρης ρυθμιστικός έλεγχος μιας ουσίας (Annexes II-VI)
    /// Χρήση: Κατά την αποθήκευση φόρμουλας - comprehensive check
    /// </summary>
    [HttpGet("check/full")]
    [AllowAnonymous]
    public async Task<IActionResult> FullRegulatoryCheck([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("q parameter required");

        var allEntries = await _context.CosIngEntries
            .Where(e => 
                (e.ChemicalName != null && e.ChemicalName.Contains(q)) ||
                (e.InciName != null && e.InciName.Contains(q)) ||
                (e.CASNumber != null && e.CASNumber.Contains(q)))
            .Select(e => new
            {
                e.Id,
                e.Annex,
                e.EntryNumber,
                e.ChemicalName,
                e.InciName,
                e.CASNumber,
                e.ProductType,
                e.MaxConcentration,
                e.Warnings,
                e.OtherRestrictions,
                e.CMR,
                e.ColourIndex,
                e.Colour,
                e.UpdateDate
            })
            .ToListAsync();

        var result = new
        {
            query = q,
            prohibited = allEntries.Where(e => e.Annex == "II").ToList(),
            restricted = allEntries.Where(e => e.Annex == "III").ToList(),
            colorants = allEntries.Where(e => e.Annex == "IV").ToList(),
            preservatives = allEntries.Where(e => e.Annex == "V").ToList(),
            uvFilters = allEntries.Where(e => e.Annex == "VI").ToList(),
            summary = new
            {
                isProhibited = allEntries.Any(e => e.Annex == "II"),
                isRestricted = allEntries.Any(e => e.Annex == "III"),
                isAllowedColorant = allEntries.Any(e => e.Annex == "IV"),
                isAllowedPreservative = allEntries.Any(e => e.Annex == "V"),
                isAllowedUVFilter = allEntries.Any(e => e.Annex == "VI"),
                hasCMR = allEntries.Any(e => !string.IsNullOrEmpty(e.CMR)),
                totalFindings = allEntries.Count
            }
        };

        return Ok(result);
    }

    /// <summary>
    /// Batch regulatory check - Έλεγχος πολλαπλών ουσιών ταυτόχρονα
    /// Χρήση: Κατά την αποθήκευση ολόκληρης φόρμουλας
    /// </summary>
    [HttpPost("check/batch")]
    [AllowAnonymous]
    public async Task<IActionResult> BatchRegulatoryCheck([FromBody] BatchCheckRequest request)
    {
        if (request?.InciNames == null || !request.InciNames.Any())
            return BadRequest("inciNames list is required");

        var results = new List<object>();

        foreach (var name in request.InciNames.Take(100)) // Max 100 per request
        {
            var prohibited = await _context.CosIngEntries
                .AnyAsync(e => e.Annex == "II" && e.InciName != null && e.InciName.Contains(name));

            var restriction = await _context.CosIngEntries
                .Where(e => e.Annex == "III" && e.InciName != null && e.InciName.Contains(name))
                .Select(e => new { e.MaxConcentration, e.Warnings, e.ProductType })
                .FirstOrDefaultAsync();

            results.Add(new
            {
                inciName = name,
                isProhibited = prohibited,
                isRestricted = restriction != null,
                maxConcentration = restriction?.MaxConcentration,
                warnings = restriction?.Warnings,
                productType = restriction?.ProductType
            });
        }

        return Ok(new
        {
            checkedAt = DateTime.UtcNow,
            totalChecked = results.Count,
            hasProhibited = results.Any(r => ((dynamic)r).isProhibited),
            hasRestricted = results.Any(r => ((dynamic)r).isRestricted),
            results
        });
    }

    // ============================================================
    // ANNEX BROWSING ENDPOINTS - Περιήγηση παραρτημάτων
    // ============================================================

    /// <summary>
    /// Λήψη εγγραφών ανά Annex (paginated)
    /// </summary>
    [HttpGet("annex/{annexCode}")]
    public async Task<IActionResult> GetAnnexEntries(string annexCode, [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        annexCode = annexCode.ToUpper();
        var validAnnexes = new[] { "II", "III", "IV", "V", "VI" };
        if (!validAnnexes.Contains(annexCode))
            return BadRequest($"Invalid annex code. Valid: {string.Join(", ", validAnnexes)}");

        var query = _context.CosIngEntries.Where(e => e.Annex == annexCode);

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(e =>
                (e.ChemicalName != null && e.ChemicalName.Contains(q)) ||
                (e.InciName != null && e.InciName.Contains(q)) ||
                (e.CASNumber != null && e.CASNumber.Contains(q)));
        }

        var total = await query.CountAsync();
        var entries = await query
            .OrderBy(e => e.EntryNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            annex = annexCode,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            results = entries
        });
    }

    /// <summary>
    /// Στατιστικά CosIng Database
    /// </summary>
    [HttpGet("stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStats()
    {
        var cacheKey = "cosing_stats";
        if (_cache.TryGetValue(cacheKey, out object? cached))
            return Ok(cached);

        var stats = new
        {
            totalIngredients = await _context.CosIngIngredients.CountAsync(),
            annexII_Prohibited = await _context.CosIngEntries.CountAsync(e => e.Annex == "II"),
            annexIII_Restricted = await _context.CosIngEntries.CountAsync(e => e.Annex == "III"),
            annexIV_Colorants = await _context.CosIngEntries.CountAsync(e => e.Annex == "IV"),
            annexV_Preservatives = await _context.CosIngEntries.CountAsync(e => e.Annex == "V"),
            annexVI_UVFilters = await _context.CosIngEntries.CountAsync(e => e.Annex == "VI"),
            dataSource = "EU CosIng Database - Regulation (EC) 1223/2009",
            lastUpdated = "March 2026"
        };

        _cache.Set(cacheKey, stats, TimeSpan.FromHours(24));
        return Ok(stats);
    }
}

/// <summary>
/// Request model για batch regulatory check
/// </summary>
public class BatchCheckRequest
{
    public List<string> InciNames { get; set; } = new();
}
