// Author: Damianos Pythagoras Ougkrenidis
//
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Dtos;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// Διαχείριση Προϊόντος — κεντρικό hub για τη διαχείριση (CRUD) όλων των
/// δεδομένων μιας φόρμουλας που τροφοδοτούν τα τεχνικά έγγραφα (FORM1–FORM14).
/// </summary>
[Authorize]
public class ProductManagementController : Controller
{
    private readonly INCISDbContext _context;
    private const string WriteRoles = AppRoles.WriteAccess;

    public ProductManagementController(INCISDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════════
    // Index — λίστα φόρμουλών προς διαχείριση
    // ═══════════════════════════════════════════════════════════════
    public async Task<IActionResult> Index(string? searchString, int pageNumber = 1)
    {
        ViewData["CurrentFilter"] = searchString;

        var query = _context.Formulas
            .Include(f => f.Client)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            query = query.Where(f =>
                f.FormulaCode.Contains(searchString)
                || (f.ProductName != null && f.ProductName.Contains(searchString))
                || (f.Client != null && f.Client.Name.Contains(searchString)));
        }

        var projected = query
            .OrderByDescending(f => f.LastModified ?? f.CreationDate)
            .Select(f => new ProductFormulaListItem
            {
                Id = f.Id,
                FormulaCode = f.FormulaCode,
                ProductName = f.ProductName,
                ProductType = f.ProductType,
                ClientName = f.Client != null ? f.Client.Name : null,
                Version = f.Version,
                LastModified = f.LastModified ?? f.CreationDate,
                DataPointsCount =
                    f.Allergens.Count
                    + f.HeavyMetals.Count
                    + f.INCIIngredients.Count
                    + f.Claims.Count
                    + f.ToxicologicalData.Count
                    + (f.MarketingInfo != null ? 1 : 0)
                    + (f.PriceInfo != null ? 1 : 0)
                    + (f.NaturalCertification != null ? 1 : 0)
                    + (f.SafetyData != null ? 1 : 0),
                // 10 βαθμολογούμενες κατηγορίες δεδομένων
                CompletenessPercent = (int)Math.Round(100.0 * (
                    (f.FormulaRawMaterials.Any(frm => frm.IsActive) ? 1 : 0)
                    + (f.INCIIngredients.Any() ? 1 : 0)
                    + (f.Allergens.Any() ? 1 : 0)
                    + (f.HeavyMetals.Any() ? 1 : 0)
                    + (f.Claims.Any() ? 1 : 0)
                    + (f.ToxicologicalData.Any() ? 1 : 0)
                    + (f.MarketingInfo != null ? 1 : 0)
                    + (f.PriceInfo != null ? 1 : 0)
                    + (f.NaturalCertification != null ? 1 : 0)
                    + (f.SafetyData != null ? 1 : 0)
                ) / 10.0)
            });

        var paginated = await PaginatedList<ProductFormulaListItem>.CreateAsync(projected, pageNumber, 12);
        return View(paginated);
    }

    // ═══════════════════════════════════════════════════════════════
    // Manage — hub μιας φόρμουλας με κάρτες ανά κατηγορία δεδομένων
    // ═══════════════════════════════════════════════════════════════
    public async Task<IActionResult> Manage(int id)
    {
        var formula = await _context.Formulas
            .Include(f => f.Client)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (formula == null) return NotFound();

        var model = new ProductManageViewModel
        {
            FormulaId = formula.Id,
            FormulaCode = formula.FormulaCode,
            ProductName = formula.ProductName,
            ProductType = formula.ProductType,
            ClientName = formula.Client?.Name,
            Version = formula.Version,
            Categories = new List<ProductDataCategory>
            {
                new()
                {
                    Key = "composition", FormNumber = "FORM12/14", Title = "Σύνθεση Α' Υλών",
                    Description = "Πρώτες ύλες της φόρμουλας με ποσοστό (% w/w) και φάση παραγωγής.",
                    Icon = "fa-cubes", BadgeColor = "primary",
                    ActionName = nameof(Composition),
                    Count = await _context.FormulaRawMaterials.CountAsync(frm => frm.FormulaId == id && frm.IsActive)
                },
                new()
                {
                    Key = "masterinfo", FormNumber = "FORM12", Title = "Στοιχεία Master Formula",
                    Description = "Τόπος/μέθοδος παραγωγής, φυσικοχημικά, μικροβιολογικά, συσκευασία.",
                    Icon = "fa-file-medical", BadgeColor = "secondary",
                    ActionName = nameof(MasterInfo), IsSingleRecord = true,
                    Count = 1
                },
                new()
                {
                    Key = "inci", FormNumber = "FORM5", Title = "Συστατικά INCI",
                    Description = "Ποσοτική σύνθεση INCI (όνομα, λειτουργία, %, CAS/EC).",
                    Icon = "fa-atom", BadgeColor = "info",
                    ActionName = nameof(Inci),
                    Count = await _context.INCIIngredients.CountAsync(i => i.FormulaId == id)
                },
                new()
                {
                    Key = "allergens", FormNumber = "FORM8", Title = "Αλλεργιογόνα",
                    Description = "Αλλεργιογόνα συστατικά και δηλώσεις στη φόρμουλα.",
                    Icon = "fa-diamond-exclamation", BadgeColor = "danger",
                    ActionName = nameof(Allergens),
                    Count = await _context.Allergens.CountAsync(a => a.FormulaId == id)
                },
                new()
                {
                    Key = "heavymetals", FormNumber = "FORM3", Title = "Βαρέα Μέταλλα",
                    Description = "Βαρέα μέταλλα και προσμίξεις με όρια συμμόρφωσης.",
                    Icon = "fa-shield-halved", BadgeColor = "warning",
                    ActionName = nameof(HeavyMetals),
                    Count = await _context.HeavyMetals.CountAsync(h => h.FormulaId == id)
                },
                new()
                {
                    Key = "claims", FormNumber = "FORM10", Title = "Ισχυρισμοί (Claims)",
                    Description = "Δηλώσεις/ισχυρισμοί προϊόντος και τεκμηρίωση.",
                    Icon = "fa-clipboard-check", BadgeColor = "info",
                    ActionName = nameof(Claims),
                    Count = await _context.Claims.CountAsync(c => c.FormulaId == id)
                },
                new()
                {
                    Key = "toxicology", FormNumber = "FORM11", Title = "Τοξικολογικά",
                    Description = "Τοξικολογικό προφίλ ανά συστατικό (NOAEL, SED, MoS).",
                    Icon = "fa-skull-crossbones", BadgeColor = "warning",
                    ActionName = nameof(Toxicology),
                    Count = await _context.ToxicologicalData.CountAsync(t => t.FormulaId == id)
                },
                new()
                {
                    Key = "marketing", FormNumber = "FORM6", Title = "Πληροφορίες Marketing",
                    Description = "Εμπορική παρουσίαση, κοινό-στόχος, οφέλη, συσκευασία.",
                    Icon = "fa-bullhorn", BadgeColor = "primary",
                    ActionName = nameof(Marketing), IsSingleRecord = true,
                    Count = await _context.MarketingInfos.CountAsync(m => m.FormulaId == id)
                },
                new()
                {
                    Key = "price", FormNumber = "FORM1", Title = "Τιμολόγηση",
                    Description = "Κόστος, τιμή μονάδας, περιθώριο κέρδους, ΠΛΤ.",
                    Icon = "fa-euro-sign", BadgeColor = "success",
                    ActionName = nameof(Price), IsSingleRecord = true,
                    Count = await _context.PriceInfos.CountAsync(p => p.FormulaId == id)
                },
                new()
                {
                    Key = "natural", FormNumber = "FORM2", Title = "Φυσική Πιστοποίηση",
                    Description = "Φυσικά/βιολογικά ποσοστά και πιστοποιήσεις (ISO 16128).",
                    Icon = "fa-leaf", BadgeColor = "success",
                    ActionName = nameof(Natural), IsSingleRecord = true,
                    Count = await _context.NaturalCertifications.CountAsync(n => n.FormulaId == id)
                },
                new()
                {
                    Key = "safety", FormNumber = "SAFETY", Title = "Δεδομένα Ασφαλείας",
                    Description = "Ερεθισμός, συμβατότητα, προειδοποιήσεις, προφυλάξεις.",
                    Icon = "fa-kit-medical", BadgeColor = "danger",
                    ActionName = nameof(Safety), IsSingleRecord = true,
                    Count = await _context.SafetyData.CountAsync(s => s.FormulaId == id)
                }
            }
        };

        return View(model);
    }

    private async Task<Formula?> GetFormulaHeader(int formulaId) =>
        await _context.Formulas.Include(f => f.Client).FirstOrDefaultAsync(f => f.Id == formulaId);

    /// <summary>
    /// Επαληθεύει ότι η φόρμουλα είναι προσβάσιμη από τον τρέχοντα tenant
    /// (χρησιμοποιεί το φιλτραρισμένο DbSet — αποτρέπει IDOR σε υπο-δεδομένα άλλου tenant).
    /// </summary>
    private Task<bool> CanAccessFormula(int formulaId) =>
        _context.Formulas.AnyAsync(f => f.Id == formulaId);

    private void SetFormulaContext(Formula formula)
    {
        ViewData["FormulaId"] = formula.Id;
        ViewData["FormulaCode"] = formula.FormulaCode;
        ViewData["ProductName"] = formula.ProductName;
        ViewData["ProductType"] = formula.ProductType;
    }

    /// <summary>
    /// Επιχειρησιακός κανόνας: αυτόματος υπολογισμός MoS = NOAEL/SED (αν δεν δόθηκε)
    /// και IsSafe = MoS ≥ 100 (SCCS Notes of Guidance).
    /// </summary>
    private static void ApplyToxCalculations(ToxicologicalData tox)
    {
        if ((tox.MoS == null || tox.MoS == 0) && tox.NOAEL.HasValue && tox.SED.HasValue && tox.SED.Value != 0)
        {
            tox.MoS = Math.Round(tox.NOAEL.Value / tox.SED.Value, 2);
        }
        if (tox.MoS.HasValue)
        {
            tox.IsSafe = tox.MoS.Value >= 100m;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM8 — Αλλεργιογόνα (Allergens)  [ΠΡΟΤΥΠΟ CRUD]
    // ═══════════════════════════════════════════════════════════════

    public async Task<IActionResult> Allergens(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);

        var items = await _context.Allergens
            .Where(a => a.FormulaId == formulaId)
            .OrderBy(a => a.AllergenName)
            .ToListAsync();

        return View(items);
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> AllergenCreate(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);
        return View(new Allergen { FormulaId = formulaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> AllergenCreate([Bind("FormulaId,AllergenName,AllergenType,Concentration,Unit,Source,IsDeclared,Notes")] Allergen allergen)
    {
        if (!await _context.Formulas.AnyAsync(f => f.Id == allergen.FormulaId)) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Allergens.Add(allergen);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Το αλλεργιογόνο καταχωρήθηκε.";
            return RedirectToAction(nameof(Allergens), new { formulaId = allergen.FormulaId });
        }

        var formula = await GetFormulaHeader(allergen.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(allergen);
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> AllergenEdit(int id)
    {
        var allergen = await _context.Allergens.FirstOrDefaultAsync(a => a.Id == id);
        if (allergen == null) return NotFound();

        var formula = await GetFormulaHeader(allergen.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(allergen);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> AllergenEdit(int id, [Bind("Id,FormulaId,AllergenName,AllergenType,Concentration,Unit,Source,IsDeclared,Notes")] Allergen allergen)
    {
        if (id != allergen.Id) return NotFound();
        if (!await _context.Allergens.AnyAsync(a => a.Id == id)) return NotFound(); // tenant ownership

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(allergen);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Το αλλεργιογόνο ενημερώθηκε.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Allergens.Any(a => a.Id == allergen.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Allergens), new { formulaId = allergen.FormulaId });
        }

        var formula = await GetFormulaHeader(allergen.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(allergen);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> AllergenDelete(int id)
    {
        var allergen = await _context.Allergens.FirstOrDefaultAsync(a => a.Id == id);
        if (allergen == null) return NotFound();

        var formulaId = allergen.FormulaId;
        _context.Allergens.Remove(allergen);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Το αλλεργιογόνο διαγράφηκε.";
        return RedirectToAction(nameof(Allergens), new { formulaId });
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM3 — Βαρέα Μέταλλα (Heavy Metals)
    // ═══════════════════════════════════════════════════════════════

    public async Task<IActionResult> HeavyMetals(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);

        var items = await _context.HeavyMetals
            .Where(h => h.FormulaId == formulaId)
            .OrderBy(h => h.MetalName)
            .ToListAsync();

        return View(items);
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> HeavyMetalCreate(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);
        return View(new HeavyMetal { FormulaId = formulaId, Unit = "ppm" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> HeavyMetalCreate([Bind("FormulaId,MetalName,LimitValue,DetectedValue,Unit,TestMethod,IsCompliant,TestDate,Notes")] HeavyMetal metal)
    {
        if (!await _context.Formulas.AnyAsync(f => f.Id == metal.FormulaId)) return NotFound();

        if (ModelState.IsValid)
        {
            _context.HeavyMetals.Add(metal);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Το βαρύ μέταλλο καταχωρήθηκε.";
            return RedirectToAction(nameof(HeavyMetals), new { formulaId = metal.FormulaId });
        }

        var formula = await GetFormulaHeader(metal.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(metal);
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> HeavyMetalEdit(int id)
    {
        var metal = await _context.HeavyMetals.FirstOrDefaultAsync(h => h.Id == id);
        if (metal == null) return NotFound();

        var formula = await GetFormulaHeader(metal.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(metal);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> HeavyMetalEdit(int id, [Bind("Id,FormulaId,MetalName,LimitValue,DetectedValue,Unit,TestMethod,IsCompliant,TestDate,Notes")] HeavyMetal metal)
    {
        if (id != metal.Id) return NotFound();
        if (!await _context.HeavyMetals.AnyAsync(h => h.Id == id)) return NotFound(); // tenant ownership

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(metal);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Το βαρύ μέταλλο ενημερώθηκε.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.HeavyMetals.Any(h => h.Id == metal.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(HeavyMetals), new { formulaId = metal.FormulaId });
        }

        var formula = await GetFormulaHeader(metal.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(metal);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> HeavyMetalDelete(int id)
    {
        var metal = await _context.HeavyMetals.FirstOrDefaultAsync(h => h.Id == id);
        if (metal == null) return NotFound();

        var formulaId = metal.FormulaId;
        _context.HeavyMetals.Remove(metal);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Το βαρύ μέταλλο διαγράφηκε.";
        return RedirectToAction(nameof(HeavyMetals), new { formulaId });
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM5 — Συστατικά INCI (INCIIngredient)
    // ═══════════════════════════════════════════════════════════════

    public async Task<IActionResult> Inci(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);

        var items = await _context.INCIIngredients
            .Where(i => i.FormulaId == formulaId)
            .OrderByDescending(i => i.Percentage)
            .ToListAsync();

        return View(items);
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> InciCreate(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);
        return View(new INCIIngredient { FormulaId = formulaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> InciCreate([Bind("FormulaId,INCIName,Function,Percentage,CASNumber,ECNumber,Origin")] INCIIngredient ingredient)
    {
        if (!await _context.Formulas.AnyAsync(f => f.Id == ingredient.FormulaId)) return NotFound();

        if (ModelState.IsValid)
        {
            _context.INCIIngredients.Add(ingredient);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Το συστατικό INCI καταχωρήθηκε.";
            return RedirectToAction(nameof(Inci), new { formulaId = ingredient.FormulaId });
        }

        var formula = await GetFormulaHeader(ingredient.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(ingredient);
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> InciEdit(int id)
    {
        var ingredient = await _context.INCIIngredients.FirstOrDefaultAsync(i => i.Id == id);
        if (ingredient == null) return NotFound();

        var formula = await GetFormulaHeader(ingredient.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(ingredient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> InciEdit(int id, [Bind("Id,FormulaId,INCIName,Function,Percentage,CASNumber,ECNumber,Origin")] INCIIngredient ingredient)
    {
        if (id != ingredient.Id) return NotFound();
        if (!await _context.INCIIngredients.AnyAsync(i => i.Id == id)) return NotFound(); // tenant ownership

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(ingredient);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Το συστατικό INCI ενημερώθηκε.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.INCIIngredients.Any(i => i.Id == ingredient.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Inci), new { formulaId = ingredient.FormulaId });
        }

        var formula = await GetFormulaHeader(ingredient.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(ingredient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> InciDelete(int id)
    {
        var ingredient = await _context.INCIIngredients.FirstOrDefaultAsync(i => i.Id == id);
        if (ingredient == null) return NotFound();

        var formulaId = ingredient.FormulaId;
        _context.INCIIngredients.Remove(ingredient);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Το συστατικό INCI διαγράφηκε.";
        return RedirectToAction(nameof(Inci), new { formulaId });
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM10 — Ισχυρισμοί (Claims)
    // ═══════════════════════════════════════════════════════════════

    public async Task<IActionResult> Claims(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);

        var items = await _context.Claims
            .Where(c => c.FormulaId == formulaId)
            .OrderBy(c => c.ClaimCategory)
            .ToListAsync();

        return View(items);
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> ClaimCreate(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);
        return View(new Claim { FormulaId = formulaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> ClaimCreate([Bind("FormulaId,ClaimText,ClaimCategory,ClaimType,IsVerified,SupportingEvidence,RegulatoryStatus,Notes")] Claim claim)
    {
        if (!await _context.Formulas.AnyAsync(f => f.Id == claim.FormulaId)) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Ο ισχυρισμός καταχωρήθηκε.";
            return RedirectToAction(nameof(Claims), new { formulaId = claim.FormulaId });
        }

        var formula = await GetFormulaHeader(claim.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(claim);
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> ClaimEdit(int id)
    {
        var claim = await _context.Claims.FirstOrDefaultAsync(c => c.Id == id);
        if (claim == null) return NotFound();

        var formula = await GetFormulaHeader(claim.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(claim);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> ClaimEdit(int id, [Bind("Id,FormulaId,ClaimText,ClaimCategory,ClaimType,IsVerified,SupportingEvidence,RegulatoryStatus,Notes")] Claim claim)
    {
        if (id != claim.Id) return NotFound();
        if (!await _context.Claims.AnyAsync(c => c.Id == id)) return NotFound(); // tenant ownership

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(claim);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Ο ισχυρισμός ενημερώθηκε.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Claims.Any(c => c.Id == claim.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Claims), new { formulaId = claim.FormulaId });
        }

        var formula = await GetFormulaHeader(claim.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(claim);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> ClaimDelete(int id)
    {
        var claim = await _context.Claims.FirstOrDefaultAsync(c => c.Id == id);
        if (claim == null) return NotFound();

        var formulaId = claim.FormulaId;
        _context.Claims.Remove(claim);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Ο ισχυρισμός διαγράφηκε.";
        return RedirectToAction(nameof(Claims), new { formulaId });
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM11 — Τοξικολογικά (ToxicologicalData)
    // ═══════════════════════════════════════════════════════════════

    public async Task<IActionResult> Toxicology(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);

        var items = await _context.ToxicologicalData
            .Where(t => t.FormulaId == formulaId)
            .OrderBy(t => t.InciName)
            .ToListAsync();

        return View(items);
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> ToxCreate(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);
        return View(new ToxicologicalData { FormulaId = formulaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> ToxCreate([Bind("FormulaId,InciName,CASNumber,Function,Percentage,NOAEL,SED,MoS,ToxicologicalCategory,IsSafe,Notes,DataSource,AssessmentDate")] ToxicologicalData tox)
    {
        if (!await _context.Formulas.AnyAsync(f => f.Id == tox.FormulaId)) return NotFound();

        ApplyToxCalculations(tox);
        if (ModelState.IsValid)
        {
            _context.ToxicologicalData.Add(tox);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Η τοξικολογική εγγραφή καταχωρήθηκε.";
            return RedirectToAction(nameof(Toxicology), new { formulaId = tox.FormulaId });
        }

        var formula = await GetFormulaHeader(tox.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(tox);
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> ToxEdit(int id)
    {
        var tox = await _context.ToxicologicalData.FirstOrDefaultAsync(t => t.Id == id);
        if (tox == null) return NotFound();

        var formula = await GetFormulaHeader(tox.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(tox);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> ToxEdit(int id, [Bind("Id,FormulaId,InciName,CASNumber,Function,Percentage,NOAEL,SED,MoS,ToxicologicalCategory,IsSafe,Notes,DataSource,AssessmentDate")] ToxicologicalData tox)
    {
        if (id != tox.Id) return NotFound();
        if (!await _context.ToxicologicalData.AnyAsync(t => t.Id == id)) return NotFound(); // tenant ownership

        ApplyToxCalculations(tox);
        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(tox);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Η τοξικολογική εγγραφή ενημερώθηκε.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.ToxicologicalData.Any(t => t.Id == tox.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Toxicology), new { formulaId = tox.FormulaId });
        }

        var formula = await GetFormulaHeader(tox.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(tox);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> ToxDelete(int id)
    {
        var tox = await _context.ToxicologicalData.FirstOrDefaultAsync(t => t.Id == id);
        if (tox == null) return NotFound();

        var formulaId = tox.FormulaId;
        _context.ToxicologicalData.Remove(tox);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Η τοξικολογική εγγραφή διαγράφηκε.";
        return RedirectToAction(nameof(Toxicology), new { formulaId });
    }

    // ═══════════════════════════════════════════════════════════════
    // Single-record modules (1:1 με τη φόρμουλα)
    // FORM6 Marketing · FORM1 Price · FORM2 Natural · FORM7 Safety
    // ═══════════════════════════════════════════════════════════════

    // ── FORM6 Marketing Info ──
    public async Task<IActionResult> Marketing(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);

        var entity = await _context.MarketingInfos.FirstOrDefaultAsync(m => m.FormulaId == formulaId)
                     ?? new MarketingInfo { FormulaId = formulaId, ProductName = formula.ProductName };
        return View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> MarketingSave([Bind("Id,FormulaId,ProductName,ProductDescription,TargetAudience,AgeRange,GenderTarget,KeyBenefits,UsageInstructions,PackagingType,DistributionChannel,LaunchDate")] MarketingInfo model)
    {
        if (!await _context.Formulas.AnyAsync(f => f.Id == model.FormulaId)) return NotFound();

        if (ModelState.IsValid)
        {
            if (model.Id == 0) _context.MarketingInfos.Add(model);
            else _context.MarketingInfos.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Οι πληροφορίες marketing αποθηκεύτηκαν.";
            return RedirectToAction(nameof(Manage), new { id = model.FormulaId });
        }

        var formula = await GetFormulaHeader(model.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(nameof(Marketing), model);
    }

    // ── FORM1 Price Info ──
    public async Task<IActionResult> Price(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);

        var entity = await _context.PriceInfos.FirstOrDefaultAsync(p => p.FormulaId == formulaId)
                     ?? new PriceInfo { FormulaId = formulaId, Currency = "EUR" };
        return View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> PriceSave([Bind("Id,FormulaId,UnitPrice,Currency,CostPerUnit,ProfitMargin,RecommendedRetailPrice,PriceCategory,LastPriceUpdate")] PriceInfo model)
    {
        if (!await _context.Formulas.AnyAsync(f => f.Id == model.FormulaId)) return NotFound();

        if (ModelState.IsValid)
        {
            model.LastPriceUpdate = DateTime.Now;
            if (model.Id == 0) _context.PriceInfos.Add(model);
            else _context.PriceInfos.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Η τιμολόγηση αποθηκεύτηκε.";
            return RedirectToAction(nameof(Manage), new { id = model.FormulaId });
        }

        var formula = await GetFormulaHeader(model.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(nameof(Price), model);
    }

    // ── FORM2 Natural Certification ──
    public async Task<IActionResult> Natural(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);

        var entity = await _context.NaturalCertifications.FirstOrDefaultAsync(n => n.FormulaId == formulaId)
                     ?? new NaturalCertification { FormulaId = formulaId };
        return View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> NaturalSave([Bind("Id,FormulaId,CertificationType,CertifyingBody,CertificationLevel,NaturalPercentage,OrganicPercentage,CertifiedIngredients,CertificationDate,ExpiryDate,Notes")] NaturalCertification model)
    {
        if (!await _context.Formulas.AnyAsync(f => f.Id == model.FormulaId)) return NotFound();

        if (ModelState.IsValid)
        {
            if (model.Id == 0) _context.NaturalCertifications.Add(model);
            else _context.NaturalCertifications.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Η φυσική πιστοποίηση αποθηκεύτηκε.";
            return RedirectToAction(nameof(Manage), new { id = model.FormulaId });
        }

        var formula = await GetFormulaHeader(model.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(nameof(Natural), model);
    }

    // ── FORM7 Safety Data ──
    public async Task<IActionResult> Safety(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);

        var entity = await _context.SafetyData.FirstOrDefaultAsync(s => s.FormulaId == formulaId)
                     ?? new SafetyData { FormulaId = formulaId };
        return View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> SafetySave([Bind("Id,FormulaId,IrritationLevel,SkinCompatibility,EyeIrritation,IsSafeForPregnancy,Phototoxicity,Sensitization,Warnings,Precautions,SafetyAssessmentDate")] SafetyData model)
    {
        if (!await _context.Formulas.AnyAsync(f => f.Id == model.FormulaId)) return NotFound();

        if (ModelState.IsValid)
        {
            if (model.Id == 0) _context.SafetyData.Add(model);
            else _context.SafetyData.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Τα δεδομένα ασφαλείας αποθηκεύτηκαν.";
            return RedirectToAction(nameof(Manage), new { id = model.FormulaId });
        }

        var formula = await GetFormulaHeader(model.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        return View(nameof(Safety), model);
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM12/14 — Σύνθεση Α' Υλών (FormulaRawMaterials)
    // ═══════════════════════════════════════════════════════════════

    public async Task<IActionResult> Composition(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);

        var items = await _context.FormulaRawMaterials
            .Where(frm => frm.FormulaId == formulaId && frm.IsActive)
            .Include(frm => frm.RawMaterial!)
                .ThenInclude(rm => rm.Supplier)
            .OrderBy(frm => frm.Phase)
            .ThenBy(frm => frm.Id)
            .ToListAsync();

        ViewData["TotalPercentage"] = items.Sum(i => i.Percentage ?? 0);
        return View(items);
    }

    private async Task PopulateRawMaterialsDropdown()
    {
        ViewBag.RawMaterialList = await _context.RawMaterials
            .Where(rm => rm.IsActive)
            .OrderBy(rm => rm.ProductName)
            .Select(rm => new { rm.Id, Label = rm.Code + " — " + rm.ProductName })
            .ToListAsync();
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CompositionAdd(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);
        await PopulateRawMaterialsDropdown();
        return View(new FormulaRawMaterial { FormulaId = formulaId, IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CompositionAdd([Bind("FormulaId,RawMaterialId,Percentage,Phase")] FormulaRawMaterial line)
    {
        if (!await _context.Formulas.AnyAsync(f => f.Id == line.FormulaId)) return NotFound();
        if (!await _context.RawMaterials.AnyAsync(rm => rm.Id == line.RawMaterialId))
            ModelState.AddModelError(nameof(line.RawMaterialId), "Επιλέξτε έγκυρη α' ύλη.");

        if (ModelState.IsValid)
        {
            line.IsActive = true;
            line.CreatedAt = DateTime.Now;
            _context.FormulaRawMaterials.Add(line);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Η α' ύλη προστέθηκε στη φόρμουλα.";
            return RedirectToAction(nameof(Composition), new { formulaId = line.FormulaId });
        }

        var formula = await GetFormulaHeader(line.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        await PopulateRawMaterialsDropdown();
        return View(line);
    }

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CompositionEdit(int id)
    {
        var line = await _context.FormulaRawMaterials.FirstOrDefaultAsync(l => l.Id == id);
        if (line == null) return NotFound();

        var formula = await GetFormulaHeader(line.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        await PopulateRawMaterialsDropdown();
        return View(line);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CompositionEdit(int id, [Bind("Id,FormulaId,RawMaterialId,Percentage,Phase,IsActive")] FormulaRawMaterial line)
    {
        if (id != line.Id) return NotFound();
        if (!await _context.RawMaterials.AnyAsync(rm => rm.Id == line.RawMaterialId))
            ModelState.AddModelError(nameof(line.RawMaterialId), "Επιλέξτε έγκυρη α' ύλη.");

        if (ModelState.IsValid)
        {
            var entity = await _context.FormulaRawMaterials.FirstOrDefaultAsync(l => l.Id == id);
            if (entity == null) return NotFound();
            entity.RawMaterialId = line.RawMaterialId;
            entity.Percentage = line.Percentage;
            entity.Phase = line.Phase;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Η εγγραφή σύνθεσης ενημερώθηκε.";
            return RedirectToAction(nameof(Composition), new { formulaId = entity.FormulaId });
        }

        var formula = await GetFormulaHeader(line.FormulaId);
        if (formula != null) SetFormulaContext(formula);
        await PopulateRawMaterialsDropdown();
        return View(line);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CompositionDelete(int id)
    {
        var line = await _context.FormulaRawMaterials.FirstOrDefaultAsync(l => l.Id == id);
        if (line == null) return NotFound();

        var formulaId = line.FormulaId;
        _context.FormulaRawMaterials.Remove(line);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Η α' ύλη αφαιρέθηκε από τη φόρμουλα.";
        return RedirectToAction(nameof(Composition), new { formulaId });
    }

    // ═══════════════════════════════════════════════════════════════
    // FORM12 — Στοιχεία Master Formula (Formula header fields)
    // ═══════════════════════════════════════════════════════════════

    public async Task<IActionResult> MasterInfo(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);
        return View(formula);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> MasterInfoSave([Bind("Id,ManufacturingSite,ManufacturingMethod,PhysicochemicalProperties,MicrobiologicalProperties,PackagingInfo,Comments")] Formula input)
    {
        var formula = await _context.Formulas.FindAsync(input.Id);
        if (formula == null) return NotFound();

        if (ModelState.IsValid)
        {
            formula.ManufacturingSite = input.ManufacturingSite;
            formula.ManufacturingMethod = input.ManufacturingMethod;
            formula.PhysicochemicalProperties = input.PhysicochemicalProperties;
            formula.MicrobiologicalProperties = input.MicrobiologicalProperties;
            formula.PackagingInfo = input.PackagingInfo;
            formula.Comments = input.Comments;
            formula.LastModified = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Τα στοιχεία Master Formula αποθηκεύτηκαν.";
            return RedirectToAction(nameof(Manage), new { id = formula.Id });
        }

        var ctx = await GetFormulaHeader(formula.Id);
        if (ctx != null) SetFormulaContext(ctx);
        return View(nameof(MasterInfo), formula);
    }

    // ═══════════════════════════════════════════════════════════════
    // B — Αυτόματη παραγωγή INCI List από τη Σύνθεση Α' Υλών
    // ═══════════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> GenerateInci(int formulaId)
    {
        if (!await _context.Formulas.AnyAsync(f => f.Id == formulaId)) return NotFound();

        var rawData = await _context.FormulaRawMaterials
            .Where(frm => frm.FormulaId == formulaId && frm.IsActive)
            .Include(frm => frm.RawMaterial!)
                .ThenInclude(rm => rm.Composition.Where(c => !c.IsImpurity))
                    .ThenInclude(rc => rc.INCI)
            .AsSplitQuery()
            .ToListAsync();

        // Συγκέντρωση ανά INCI (σταθμισμένη με το % της α' ύλης)
        var agg = new Dictionary<string, (decimal Pct, INCI? Inci)>(StringComparer.OrdinalIgnoreCase);
        foreach (var frm in rawData)
        {
            var rm = frm.RawMaterial;
            if (rm?.Composition == null) continue;
            var frmPct = frm.Percentage ?? 0m;
            foreach (var comp in rm.Composition.Where(c => !c.IsImpurity))
            {
                var inci = comp.INCI;
                if (inci == null || string.IsNullOrWhiteSpace(inci.Name)) continue;
                var avg = ((comp.MinPercentage ?? 0m) + (comp.MaxPercentage ?? comp.MinPercentage ?? 100m)) / 2m;
                var contribution = frmPct * avg / 100m;
                if (agg.TryGetValue(inci.Name, out var existing))
                    agg[inci.Name] = (existing.Pct + contribution, inci);
                else
                    agg[inci.Name] = (contribution, inci);
            }
        }

        if (agg.Count == 0)
        {
            TempData["Error"] = "Δεν βρέθηκε σύνθεση α' υλών για υπολογισμό INCI. Προσθέστε πρώτα α' ύλες με συστατικά.";
            return RedirectToAction(nameof(Inci), new { formulaId });
        }

        // Αντικατάσταση υπαρχόντων INCIIngredients
        var existingItems = _context.INCIIngredients.Where(i => i.FormulaId == formulaId);
        _context.INCIIngredients.RemoveRange(existingItems);

        foreach (var kvp in agg.OrderByDescending(x => x.Value.Pct))
        {
            _context.INCIIngredients.Add(new INCIIngredient
            {
                FormulaId = formulaId,
                INCIName = kvp.Key,
                Percentage = Math.Round(kvp.Value.Pct, 4),
                CASNumber = kvp.Value.Inci?.CASNumber,
                ECNumber = kvp.Value.Inci?.ECNumber,
                Function = kvp.Value.Inci?.Function
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Υπολογίστηκαν {agg.Count} συστατικά INCI από τη σύνθεση α' υλών.";
        return RedirectToAction(nameof(Inci), new { formulaId });
    }

    // ═══════════════════════════════════════════════════════════════
    // G — Αντιγραφή δεδομένων από άλλη φόρμουλα
    // ═══════════════════════════════════════════════════════════════

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CopyFrom(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);

        ViewBag.SourceFormulas = await _context.Formulas
            .Where(f => f.Id != formulaId)
            .OrderBy(f => f.FormulaCode)
            .Select(f => new { f.Id, Label = f.FormulaCode + " — " + (f.ProductName ?? "") })
            .ToListAsync();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> CopyFrom(int formulaId, int sourceFormulaId, string[] categories)
    {
        if (!await _context.Formulas.AnyAsync(f => f.Id == formulaId)) return NotFound();
        if (sourceFormulaId == formulaId || !await _context.Formulas.AnyAsync(f => f.Id == sourceFormulaId))
        {
            TempData["Error"] = "Επιλέξτε έγκυρη φόρμουλα-πηγή.";
            return RedirectToAction(nameof(CopyFrom), new { formulaId });
        }

        var sel = new HashSet<string>(categories ?? Array.Empty<string>());
        int copied = 0;

        if (sel.Contains("composition"))
        {
            _context.FormulaRawMaterials.RemoveRange(_context.FormulaRawMaterials.Where(x => x.FormulaId == formulaId));
            var src = await _context.FormulaRawMaterials.Where(x => x.FormulaId == sourceFormulaId && x.IsActive).ToListAsync();
            foreach (var s in src)
                _context.FormulaRawMaterials.Add(new FormulaRawMaterial { FormulaId = formulaId, RawMaterialId = s.RawMaterialId, Percentage = s.Percentage, Phase = s.Phase, IsActive = true, CreatedAt = DateTime.Now });
            copied += src.Count;
        }
        if (sel.Contains("inci"))
        {
            _context.INCIIngredients.RemoveRange(_context.INCIIngredients.Where(x => x.FormulaId == formulaId));
            var src = await _context.INCIIngredients.Where(x => x.FormulaId == sourceFormulaId).ToListAsync();
            foreach (var s in src)
                _context.INCIIngredients.Add(new INCIIngredient { FormulaId = formulaId, INCIName = s.INCIName, Function = s.Function, Percentage = s.Percentage, CASNumber = s.CASNumber, ECNumber = s.ECNumber, Origin = s.Origin });
            copied += src.Count;
        }
        if (sel.Contains("allergens"))
        {
            _context.Allergens.RemoveRange(_context.Allergens.Where(x => x.FormulaId == formulaId));
            var src = await _context.Allergens.Where(x => x.FormulaId == sourceFormulaId).ToListAsync();
            foreach (var s in src)
                _context.Allergens.Add(new Allergen { FormulaId = formulaId, AllergenName = s.AllergenName, AllergenType = s.AllergenType, Concentration = s.Concentration, Unit = s.Unit, Source = s.Source, IsDeclared = s.IsDeclared, Notes = s.Notes });
            copied += src.Count;
        }
        if (sel.Contains("heavymetals"))
        {
            _context.HeavyMetals.RemoveRange(_context.HeavyMetals.Where(x => x.FormulaId == formulaId));
            var src = await _context.HeavyMetals.Where(x => x.FormulaId == sourceFormulaId).ToListAsync();
            foreach (var s in src)
                _context.HeavyMetals.Add(new HeavyMetal { FormulaId = formulaId, MetalName = s.MetalName, LimitValue = s.LimitValue, DetectedValue = s.DetectedValue, Unit = s.Unit, TestMethod = s.TestMethod, IsCompliant = s.IsCompliant, TestDate = s.TestDate, Notes = s.Notes });
            copied += src.Count;
        }
        if (sel.Contains("claims"))
        {
            _context.Claims.RemoveRange(_context.Claims.Where(x => x.FormulaId == formulaId));
            var src = await _context.Claims.Where(x => x.FormulaId == sourceFormulaId).ToListAsync();
            foreach (var s in src)
                _context.Claims.Add(new Claim { FormulaId = formulaId, ClaimText = s.ClaimText, ClaimCategory = s.ClaimCategory, ClaimType = s.ClaimType, IsVerified = s.IsVerified, SupportingEvidence = s.SupportingEvidence, RegulatoryStatus = s.RegulatoryStatus, Notes = s.Notes });
            copied += src.Count;
        }
        if (sel.Contains("toxicology"))
        {
            _context.ToxicologicalData.RemoveRange(_context.ToxicologicalData.Where(x => x.FormulaId == formulaId));
            var src = await _context.ToxicologicalData.Where(x => x.FormulaId == sourceFormulaId).ToListAsync();
            foreach (var s in src)
                _context.ToxicologicalData.Add(new ToxicologicalData { FormulaId = formulaId, InciName = s.InciName, CASNumber = s.CASNumber, Function = s.Function, Percentage = s.Percentage, NOAEL = s.NOAEL, SED = s.SED, MoS = s.MoS, ToxicologicalCategory = s.ToxicologicalCategory, IsSafe = s.IsSafe, Notes = s.Notes, DataSource = s.DataSource, AssessmentDate = s.AssessmentDate });
            copied += src.Count;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Αντιγράφηκαν {copied} εγγραφές από τη φόρμουλα-πηγή.";
        return RedirectToAction(nameof(Manage), new { id = formulaId });
    }

    // ═══════════════════════════════════════════════════════════════
    // H — Εισαγωγή Σύνθεσης από Excel (.xlsx)
    // ═══════════════════════════════════════════════════════════════

    [Authorize(Roles = WriteRoles)]
    public async Task<IActionResult> ImportComposition(int formulaId)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();
        SetFormulaContext(formula);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = WriteRoles)]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ImportComposition(int formulaId, IFormFile? file, bool replaceExisting = false)
    {
        var formula = await GetFormulaHeader(formulaId);
        if (formula == null) return NotFound();

        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Επιλέξτε αρχείο Excel (.xlsx).";
            return RedirectToAction(nameof(ImportComposition), new { formulaId });
        }

        int added = 0, skipped = 0;
        var skippedCodes = new List<string>();

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();

            // Εντοπισμός στηλών από την κεφαλίδα (γραμμή 1)
            var headerRow = ws.Row(1);
            int colCode = 0, colPct = 0, colPhase = 0;
            foreach (var cell in headerRow.CellsUsed())
            {
                var h = (cell.GetString() ?? "").Trim().ToLowerInvariant();
                if (colCode == 0 && (h.Contains("code") || h == "id" || h.Contains("κωδικ") || h.Contains("prisma"))) colCode = cell.Address.ColumnNumber;
                else if (colPct == 0 && (h.Contains("%") || h.Contains("w/w") || h.Contains("perc") || h.Contains("ποσοστ"))) colPct = cell.Address.ColumnNumber;
                else if (colPhase == 0 && (h.Contains("phase") || h.Contains("φάσ") || h.Contains("φασ"))) colPhase = cell.Address.ColumnNumber;
            }

            if (colCode == 0 || colPct == 0)
            {
                TempData["Error"] = "Το αρχείο πρέπει να έχει στήλες με κεφαλίδες για Κωδικό (Code/ID/Prisma) και Ποσοστό (% w/w).";
                return RedirectToAction(nameof(ImportComposition), new { formulaId });
            }

            if (replaceExisting)
                _context.FormulaRawMaterials.RemoveRange(_context.FormulaRawMaterials.Where(x => x.FormulaId == formulaId));

            var lastRow = ws.LastRowUsed()!.RowNumber();
            for (int r = 2; r <= lastRow; r++)
            {
                var code = ws.Cell(r, colCode).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(code)) continue;

                var rm = await _context.RawMaterials.FirstOrDefaultAsync(x =>
                    x.Code == code || x.PrismaCode == code);
                if (rm == null) { skipped++; if (skippedCodes.Count < 20) skippedCodes.Add(code); continue; }

                decimal? pct = null;
                var pctCell = ws.Cell(r, colPct);
                if (pctCell.TryGetValue<double>(out var pv)) pct = (decimal)pv;
                else if (decimal.TryParse(pctCell.GetString()?.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var pd)) pct = pd;

                string? phase = colPhase > 0 ? ws.Cell(r, colPhase).GetString()?.Trim() : null;

                _context.FormulaRawMaterials.Add(new FormulaRawMaterial
                {
                    FormulaId = formulaId,
                    RawMaterialId = rm.Id,
                    Percentage = pct,
                    Phase = phase,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });
                added++;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Σφάλμα ανάγνωσης αρχείου: " + ex.Message;
            return RedirectToAction(nameof(ImportComposition), new { formulaId });
        }

        var msg = $"Εισήχθησαν {added} α' ύλες.";
        if (skipped > 0) msg += $" Παραλείφθηκαν {skipped} (δεν βρέθηκε κωδικός: {string.Join(", ", skippedCodes)}).";
        TempData[skipped > 0 ? "Info" : "Success"] = msg;
        return RedirectToAction(nameof(Composition), new { formulaId });
    }
}
