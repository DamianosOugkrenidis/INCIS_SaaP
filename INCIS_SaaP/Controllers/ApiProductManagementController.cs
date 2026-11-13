// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Infrastructure.ApiEnvelope;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// REST API (write) για τη Διαχείριση Προϊόντος — CRUD στα per-formula δεδομένα
/// που τροφοδοτούν τα τεχνικά έγγραφα (FORM1–FORM14).
/// Reads παρέχονται από το ApiFormulaDocumentsController.
/// </summary>
[Route("api/v1/formulas/{formulaId:int}")]
[ApiController]
[Authorize]
public class ApiProductManagementController : ControllerBase
{
    private readonly INCISDbContext _context;
    private const string WriteRoles = AppRoles.WriteAccess;

    public ApiProductManagementController(INCISDbContext context)
    {
        _context = context;
    }

    private Task<bool> FormulaExists(int formulaId) =>
        _context.Formulas.AnyAsync(f => f.Id == formulaId);

    // ───────────────────────────────────────────────────────────────
    // INCI Ingredients (FORM5)
    // ───────────────────────────────────────────────────────────────

    [HttpGet("inci")]
    public async Task<IActionResult> GetInci(int formulaId)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        var items = await _context.INCIIngredients.Where(i => i.FormulaId == formulaId).ToListAsync();
        return Ok(ApiResponse<List<INCIIngredient>>.Ok(items));
    }

    [HttpPost("inci")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CreateInci(int formulaId, [FromBody] INCIIngredient input)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        input.Id = 0;
        input.FormulaId = formulaId;
        _context.INCIIngredients.Add(input);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<INCIIngredient>.Ok(input, "Created"));
    }

    [HttpPut("inci/{id:int}")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> UpdateInci(int formulaId, int id, [FromBody] INCIIngredient input)
    {
        var entity = await _context.INCIIngredients.FirstOrDefaultAsync(i => i.Id == id && i.FormulaId == formulaId);
        if (entity == null) return NotFound(ApiResponse<string>.Fail("Record not found"));
        entity.INCIName = input.INCIName;
        entity.Function = input.Function;
        entity.Percentage = input.Percentage;
        entity.CASNumber = input.CASNumber;
        entity.ECNumber = input.ECNumber;
        entity.Origin = input.Origin;
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<INCIIngredient>.Ok(entity, "Updated"));
    }

    [HttpDelete("inci/{id:int}")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> DeleteInci(int formulaId, int id)
    {
        var entity = await _context.INCIIngredients.FirstOrDefaultAsync(i => i.Id == id && i.FormulaId == formulaId);
        if (entity == null) return NotFound(ApiResponse<string>.Fail("Record not found"));
        _context.INCIIngredients.Remove(entity);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("deleted", "Deleted"));
    }

    // ───────────────────────────────────────────────────────────────
    // Allergens (FORM8)
    // ───────────────────────────────────────────────────────────────

    [HttpGet("allergens")]
    public async Task<IActionResult> GetAllergens(int formulaId)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        var items = await _context.Allergens.Where(a => a.FormulaId == formulaId).ToListAsync();
        return Ok(ApiResponse<List<Allergen>>.Ok(items));
    }

    [HttpPost("allergens")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CreateAllergen(int formulaId, [FromBody] Allergen input)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        input.Id = 0;
        input.FormulaId = formulaId;
        _context.Allergens.Add(input);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<Allergen>.Ok(input, "Created"));
    }

    [HttpPut("allergens/{id:int}")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> UpdateAllergen(int formulaId, int id, [FromBody] Allergen input)
    {
        var entity = await _context.Allergens.FirstOrDefaultAsync(a => a.Id == id && a.FormulaId == formulaId);
        if (entity == null) return NotFound(ApiResponse<string>.Fail("Record not found"));
        entity.AllergenName = input.AllergenName;
        entity.AllergenType = input.AllergenType;
        entity.Concentration = input.Concentration;
        entity.Unit = input.Unit;
        entity.Source = input.Source;
        entity.IsDeclared = input.IsDeclared;
        entity.Notes = input.Notes;
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<Allergen>.Ok(entity, "Updated"));
    }

    [HttpDelete("allergens/{id:int}")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> DeleteAllergen(int formulaId, int id)
    {
        var entity = await _context.Allergens.FirstOrDefaultAsync(a => a.Id == id && a.FormulaId == formulaId);
        if (entity == null) return NotFound(ApiResponse<string>.Fail("Record not found"));
        _context.Allergens.Remove(entity);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("deleted", "Deleted"));
    }

    // ───────────────────────────────────────────────────────────────
    // Heavy Metals (FORM3)
    // ───────────────────────────────────────────────────────────────

    [HttpGet("heavy-metals")]
    public async Task<IActionResult> GetHeavyMetals(int formulaId)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        var items = await _context.HeavyMetals.Where(h => h.FormulaId == formulaId).ToListAsync();
        return Ok(ApiResponse<List<HeavyMetal>>.Ok(items));
    }

    [HttpPost("heavy-metals")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CreateHeavyMetal(int formulaId, [FromBody] HeavyMetal input)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        input.Id = 0;
        input.FormulaId = formulaId;
        _context.HeavyMetals.Add(input);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<HeavyMetal>.Ok(input, "Created"));
    }

    [HttpPut("heavy-metals/{id:int}")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> UpdateHeavyMetal(int formulaId, int id, [FromBody] HeavyMetal input)
    {
        var entity = await _context.HeavyMetals.FirstOrDefaultAsync(h => h.Id == id && h.FormulaId == formulaId);
        if (entity == null) return NotFound(ApiResponse<string>.Fail("Record not found"));
        entity.MetalName = input.MetalName;
        entity.LimitValue = input.LimitValue;
        entity.DetectedValue = input.DetectedValue;
        entity.Unit = input.Unit;
        entity.TestMethod = input.TestMethod;
        entity.IsCompliant = input.IsCompliant;
        entity.TestDate = input.TestDate;
        entity.Notes = input.Notes;
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<HeavyMetal>.Ok(entity, "Updated"));
    }

    [HttpDelete("heavy-metals/{id:int}")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> DeleteHeavyMetal(int formulaId, int id)
    {
        var entity = await _context.HeavyMetals.FirstOrDefaultAsync(h => h.Id == id && h.FormulaId == formulaId);
        if (entity == null) return NotFound(ApiResponse<string>.Fail("Record not found"));
        _context.HeavyMetals.Remove(entity);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("deleted", "Deleted"));
    }

    // ───────────────────────────────────────────────────────────────
    // Claims (FORM10)
    // ───────────────────────────────────────────────────────────────

    [HttpGet("claims")]
    public async Task<IActionResult> GetClaims(int formulaId)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        var items = await _context.Claims.Where(c => c.FormulaId == formulaId).ToListAsync();
        return Ok(ApiResponse<List<Claim>>.Ok(items));
    }

    [HttpPost("claims")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CreateClaim(int formulaId, [FromBody] Claim input)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        input.Id = 0;
        input.FormulaId = formulaId;
        _context.Claims.Add(input);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<Claim>.Ok(input, "Created"));
    }

    [HttpPut("claims/{id:int}")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> UpdateClaim(int formulaId, int id, [FromBody] Claim input)
    {
        var entity = await _context.Claims.FirstOrDefaultAsync(c => c.Id == id && c.FormulaId == formulaId);
        if (entity == null) return NotFound(ApiResponse<string>.Fail("Record not found"));
        entity.ClaimText = input.ClaimText;
        entity.ClaimCategory = input.ClaimCategory;
        entity.ClaimType = input.ClaimType;
        entity.IsVerified = input.IsVerified;
        entity.SupportingEvidence = input.SupportingEvidence;
        entity.RegulatoryStatus = input.RegulatoryStatus;
        entity.Notes = input.Notes;
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<Claim>.Ok(entity, "Updated"));
    }

    [HttpDelete("claims/{id:int}")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> DeleteClaim(int formulaId, int id)
    {
        var entity = await _context.Claims.FirstOrDefaultAsync(c => c.Id == id && c.FormulaId == formulaId);
        if (entity == null) return NotFound(ApiResponse<string>.Fail("Record not found"));
        _context.Claims.Remove(entity);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("deleted", "Deleted"));
    }

    // ───────────────────────────────────────────────────────────────
    // Toxicological Data (FORM11)
    // ───────────────────────────────────────────────────────────────

    [HttpGet("toxicology")]
    public async Task<IActionResult> GetToxicology(int formulaId)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        var items = await _context.ToxicologicalData.Where(t => t.FormulaId == formulaId).ToListAsync();
        return Ok(ApiResponse<List<ToxicologicalData>>.Ok(items));
    }

    [HttpPost("toxicology")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CreateToxicology(int formulaId, [FromBody] ToxicologicalData input)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        input.Id = 0;
        input.FormulaId = formulaId;
        _context.ToxicologicalData.Add(input);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<ToxicologicalData>.Ok(input, "Created"));
    }

    [HttpPut("toxicology/{id:int}")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> UpdateToxicology(int formulaId, int id, [FromBody] ToxicologicalData input)
    {
        var entity = await _context.ToxicologicalData.FirstOrDefaultAsync(t => t.Id == id && t.FormulaId == formulaId);
        if (entity == null) return NotFound(ApiResponse<string>.Fail("Record not found"));
        entity.InciName = input.InciName;
        entity.CASNumber = input.CASNumber;
        entity.Function = input.Function;
        entity.Percentage = input.Percentage;
        entity.NOAEL = input.NOAEL;
        entity.SED = input.SED;
        entity.MoS = input.MoS;
        entity.ToxicologicalCategory = input.ToxicologicalCategory;
        entity.IsSafe = input.IsSafe;
        entity.Notes = input.Notes;
        entity.DataSource = input.DataSource;
        entity.AssessmentDate = input.AssessmentDate;
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<ToxicologicalData>.Ok(entity, "Updated"));
    }

    [HttpDelete("toxicology/{id:int}")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> DeleteToxicology(int formulaId, int id)
    {
        var entity = await _context.ToxicologicalData.FirstOrDefaultAsync(t => t.Id == id && t.FormulaId == formulaId);
        if (entity == null) return NotFound(ApiResponse<string>.Fail("Record not found"));
        _context.ToxicologicalData.Remove(entity);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("deleted", "Deleted"));
    }

    // ───────────────────────────────────────────────────────────────
    // Single-record (1:1) — upsert via PUT
    // ───────────────────────────────────────────────────────────────

    // Marketing Info (FORM6)
    [HttpGet("marketing")]
    public async Task<IActionResult> GetMarketing(int formulaId)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        var entity = await _context.MarketingInfos.FirstOrDefaultAsync(m => m.FormulaId == formulaId);
        return Ok(ApiResponse<MarketingInfo?>.Ok(entity));
    }

    [HttpPut("marketing")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> UpsertMarketing(int formulaId, [FromBody] MarketingInfo input)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        var entity = await _context.MarketingInfos.FirstOrDefaultAsync(m => m.FormulaId == formulaId);
        if (entity == null)
        {
            input.Id = 0;
            input.FormulaId = formulaId;
            _context.MarketingInfos.Add(input);
            entity = input;
        }
        else
        {
            entity.ProductName = input.ProductName;
            entity.ProductDescription = input.ProductDescription;
            entity.TargetAudience = input.TargetAudience;
            entity.AgeRange = input.AgeRange;
            entity.GenderTarget = input.GenderTarget;
            entity.KeyBenefits = input.KeyBenefits;
            entity.UsageInstructions = input.UsageInstructions;
            entity.PackagingType = input.PackagingType;
            entity.DistributionChannel = input.DistributionChannel;
            entity.LaunchDate = input.LaunchDate;
        }
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<MarketingInfo>.Ok(entity, "Saved"));
    }

    // Price Info (FORM1)
    [HttpGet("price")]
    public async Task<IActionResult> GetPrice(int formulaId)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        var entity = await _context.PriceInfos.FirstOrDefaultAsync(p => p.FormulaId == formulaId);
        return Ok(ApiResponse<PriceInfo?>.Ok(entity));
    }

    [HttpPut("price")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> UpsertPrice(int formulaId, [FromBody] PriceInfo input)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        var entity = await _context.PriceInfos.FirstOrDefaultAsync(p => p.FormulaId == formulaId);
        if (entity == null)
        {
            input.Id = 0;
            input.FormulaId = formulaId;
            input.LastPriceUpdate = DateTime.Now;
            _context.PriceInfos.Add(input);
            entity = input;
        }
        else
        {
            entity.UnitPrice = input.UnitPrice;
            entity.Currency = input.Currency;
            entity.CostPerUnit = input.CostPerUnit;
            entity.ProfitMargin = input.ProfitMargin;
            entity.RecommendedRetailPrice = input.RecommendedRetailPrice;
            entity.PriceCategory = input.PriceCategory;
            entity.LastPriceUpdate = DateTime.Now;
        }
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<PriceInfo>.Ok(entity, "Saved"));
    }

    // Natural Certification (FORM2)
    [HttpGet("natural")]
    public async Task<IActionResult> GetNatural(int formulaId)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        var entity = await _context.NaturalCertifications.FirstOrDefaultAsync(n => n.FormulaId == formulaId);
        return Ok(ApiResponse<NaturalCertification?>.Ok(entity));
    }

    [HttpPut("natural")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> UpsertNatural(int formulaId, [FromBody] NaturalCertification input)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        var entity = await _context.NaturalCertifications.FirstOrDefaultAsync(n => n.FormulaId == formulaId);
        if (entity == null)
        {
            input.Id = 0;
            input.FormulaId = formulaId;
            _context.NaturalCertifications.Add(input);
            entity = input;
        }
        else
        {
            entity.CertificationType = input.CertificationType;
            entity.CertifyingBody = input.CertifyingBody;
            entity.CertificationLevel = input.CertificationLevel;
            entity.NaturalPercentage = input.NaturalPercentage;
            entity.OrganicPercentage = input.OrganicPercentage;
            entity.CertifiedIngredients = input.CertifiedIngredients;
            entity.CertificationDate = input.CertificationDate;
            entity.ExpiryDate = input.ExpiryDate;
            entity.Notes = input.Notes;
        }
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<NaturalCertification>.Ok(entity, "Saved"));
    }

    // Safety Data (FORM7)
    [HttpGet("safety")]
    public async Task<IActionResult> GetSafety(int formulaId)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        var entity = await _context.SafetyData.FirstOrDefaultAsync(s => s.FormulaId == formulaId);
        return Ok(ApiResponse<SafetyData?>.Ok(entity));
    }

    [HttpPut("safety")]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> UpsertSafety(int formulaId, [FromBody] SafetyData input)
    {
        if (!await FormulaExists(formulaId)) return NotFound(ApiResponse<string>.Fail("Formula not found"));
        var entity = await _context.SafetyData.FirstOrDefaultAsync(s => s.FormulaId == formulaId);
        if (entity == null)
        {
            input.Id = 0;
            input.FormulaId = formulaId;
            _context.SafetyData.Add(input);
            entity = input;
        }
        else
        {
            entity.IrritationLevel = input.IrritationLevel;
            entity.SkinCompatibility = input.SkinCompatibility;
            entity.EyeIrritation = input.EyeIrritation;
            entity.IsSafeForPregnancy = input.IsSafeForPregnancy;
            entity.Phototoxicity = input.Phototoxicity;
            entity.Sensitization = input.Sensitization;
            entity.Warnings = input.Warnings;
            entity.Precautions = input.Precautions;
            entity.SafetyAssessmentDate = input.SafetyAssessmentDate;
        }
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<SafetyData>.Ok(entity, "Saved"));
    }
}
