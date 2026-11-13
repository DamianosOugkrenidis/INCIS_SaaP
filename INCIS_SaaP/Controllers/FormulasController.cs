// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Controllers;

[Authorize]
public class FormulasController : Controller
{
    private readonly INCISDbContext _context;
    private readonly IMemoryCache _cache;
    private const int PageSize = 10;

    public FormulasController(INCISDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // GET: Formulas
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> Index(string searchString, int pageNumber = 1)
    {
        ViewData["CurrentFilter"] = searchString;

        // Create a cache key based on search parameters
        string cacheKey = $"Formulas_Index_{searchString ?? "null"}_{pageNumber}";
        
        // Try to get cached data
        if (!_cache.TryGetValue(cacheKey, out PaginatedList<Formula>? viewModel))
        {
            // Not in cache, fetch from database
            var formulas = from f in _context.Formulas
                          select f;

            if (!String.IsNullOrEmpty(searchString))
            {
                formulas = formulas.Where(s => s.FormulaCode.Contains(searchString)
                                                || (s.FormulaName != null && s.FormulaName.Contains(searchString))
                                                || (s.ProductType != null && s.ProductType.Contains(searchString)));
            }

            var totalItems = await formulas.CountAsync();
            var pagedFormulas = await formulas
                .OrderByDescending(f => f.LastModified)
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            viewModel = new PaginatedList<Formula>(pagedFormulas, totalItems, pageNumber, PageSize);
            
            // Cache the result for 5 minutes
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetSlidingExpiration(TimeSpan.FromMinutes(2));
                
            _cache.Set(cacheKey, viewModel, cacheEntryOptions);
        }
        
        return View(viewModel);
    }

    // GET: Formulas/Details/5
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var formula = await _context.Formulas
            .Include(f => f.INCIIngredients)
            .Include(f => f.SafetyData)
            .Include(f => f.PriceInfo)
            .Include(f => f.Allergens)
            .Include(f => f.HeavyMetals)
            .Include(f => f.MarketingInfo)
            .Include(f => f.FormulaRawMaterials)
                .ThenInclude(frm => frm.RawMaterial)
                    .ThenInclude(rm => rm!.Supplier)
            .Include(f => f.FormulaRawMaterials)
                .ThenInclude(frm => frm.RawMaterial)
                    .ThenInclude(rm => rm!.MainINCI)
            .Include(f => f.NaturalCertification)
            .Include(f => f.Claims)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (formula == null)
        {
            return NotFound();
        }

        return View(formula);
    }

    // GET: Formulas/Create
    [Authorize(Roles = AppRoles.WriteAccess)]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Formulas/Create
    [HttpPost]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,FormulaCode,FormulaName,ProductType,Version,CreationDate,Description")] Formula formula)
    {
        if (ModelState.IsValid)
        {
            formula.CreationDate = DateTime.Now;
            formula.LastModified = DateTime.Now;
            _context.Add(formula);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(formula);
    }

    // GET: Formulas/Edit/5
    [Authorize(Roles = AppRoles.WriteAccess)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var formula = await _context.Formulas.FindAsync(id);
        if (formula == null)
        {
            return NotFound();
        }
        return View(formula);
    }

    // POST: Formulas/Edit/5
    [HttpPost]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FormulaCode,FormulaName,ProductType,Version,CreationDate,Description")] Formula formula)
    {
        if (id != formula.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                formula.LastModified = DateTime.Now;
                _context.Update(formula);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FormulaExists(formula.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(formula);
    }

    // GET: Formulas/Delete/5
    [Authorize(Roles = AppRoles.WriteAccess)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var formula = await _context.Formulas
            .FirstOrDefaultAsync(m => m.Id == id);
        if (formula == null)
        {
            return NotFound();
        }

        return View(formula);
    }

    // POST: Formulas/Delete/5
    [HttpPost, ActionName("Delete")]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var formula = await _context.Formulas.FindAsync(id);
        if (formula != null)
        {
            _context.Formulas.Remove(formula);
        }
        
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: Formulas/Produce/5
    public async Task<IActionResult> Produce(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var formula = await _context.Formulas
            .Include(f => f.FormulaRawMaterials)
                .ThenInclude(frm => frm.RawMaterial)
            .Include(f => f.Client)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (formula == null)
        {
            return NotFound();
        }

        // Get production history for this formula
        var productionHistory = await _context.ProductionBatches
            .Where(pb => pb.FormulaId == id && pb.IsActive)
            .OrderByDescending(pb => pb.ProductionDate)
            .ToListAsync();

        ViewBag.ProductionHistory = productionHistory;
        return View(formula);
    }

    // POST: Formulas/Produce/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Produce(int id, decimal batchSize, string unit, string? notes, string? lotNumber, string? operator_name, string? manufacturingSite)
    {
        var formula = await _context.Formulas
            .Include(f => f.FormulaRawMaterials)
                .ThenInclude(frm => frm.RawMaterial)
            .Include(f => f.Client)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (formula == null)
        {
            return NotFound();
        }

        if (batchSize <= 0)
        {
            ModelState.AddModelError("", "Ξ¤ΞΏ ΞΌΞ­Ξ³ΞµΞΈΞΏΟ‚ Ο€Ξ±ΟΟ„Ξ―Ξ΄Ξ±Ο‚ Ο€ΟΞ­Ο€ΞµΞΉ Ξ½Ξ± ΞµΞ―Ξ½Ξ±ΞΉ ΞΌΞµΞ³Ξ±Ξ»ΟΟ„ΞµΟΞΏ Ξ±Ο€Ο 0.");
            ViewBag.ProductionHistory = await _context.ProductionBatches
                .Where(pb => pb.FormulaId == id && pb.IsActive)
                .OrderByDescending(pb => pb.ProductionDate)
                .ToListAsync();
            return View(formula);
        }

        // Generate batch code
        var batchCount = await _context.ProductionBatches.CountAsync(pb => pb.FormulaId == id) + 1;
        var batchCode = $"{formula.FormulaCode}-PROD-{batchCount:D4}";

        var batch = new ProductionBatch
        {
            BatchCode = batchCode,
            FormulaId = formula.Id,
            BatchSize = batchSize,
            Unit = unit ?? "kg",
            ProductionDate = DateTime.Now,
            Status = "Planned",
            Notes = notes,
            LotNumber = lotNumber,
            Operator = operator_name,
            ManufacturingSite = manufacturingSite ?? formula.ManufacturingSite
        };

        // Calculate quantities for each raw material
        var activeRawMaterials = formula.FormulaRawMaterials
            .Where(frm => frm.IsActive && frm.RawMaterial != null && frm.RawMaterial.IsActive)
            .OrderBy(frm => frm.Phase)
            .ThenBy(frm => frm.RawMaterial!.ProductName)
            .ToList();

        int orderIndex = 1;
        foreach (var frm in activeRawMaterials)
        {
            var percentage = frm.Percentage ?? 0;
            var calculatedQuantity = Math.Round((percentage / 100) * batchSize, 4);

            batch.Lines.Add(new ProductionBatchLine
            {
                RawMaterialId = frm.RawMaterialId,
                Percentage = percentage,
                CalculatedQuantity = calculatedQuantity,
                Phase = frm.Phase,
                OrderIndex = orderIndex++
            });
        }

        // Add water line if needed
        var waterPercentage = formula.CalculateRequiredWaterPercentage();
        if (waterPercentage > 0)
        {
            // Water is calculated as the remainder to reach 100%
            batch.Lines.Add(new ProductionBatchLine
            {
                RawMaterialId = null, // Water has no raw material record
                Percentage = waterPercentage,
                CalculatedQuantity = Math.Round((waterPercentage / 100) * batchSize, 4),
                Phase = "A", // Water typically goes in phase A
                OrderIndex = 0, // First in order
                Notes = "AQUA (Water)"
            });
        }

        _context.ProductionBatches.Add(batch);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(ProduceDetails), new { id = batch.Id });
    }

    // GET: Formulas/ProduceDetails/5
    public async Task<IActionResult> ProduceDetails(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var batch = await _context.ProductionBatches
            .Include(pb => pb.Formula)
                .ThenInclude(f => f!.Client)
            .Include(pb => pb.Lines.OrderBy(l => l.Phase).ThenBy(l => l.OrderIndex))
                .ThenInclude(l => l.RawMaterial)
            .FirstOrDefaultAsync(pb => pb.Id == id);

        if (batch == null)
        {
            return NotFound();
        }

        return View(batch);
    }

    // POST: Formulas/UpdateBatchStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBatchStatus(int id, string status)
    {
        var batch = await _context.ProductionBatches.FindAsync(id);
        if (batch == null)
        {
            return NotFound();
        }

        batch.Status = status;
        if (status == "Completed")
        {
            batch.CompletedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(ProduceDetails), new { id = batch.Id });
    }

    // GET: Formulas/ProductionHistory
    public async Task<IActionResult> ProductionHistory(string searchString, int pageNumber = 1)
    {
        ViewData["CurrentFilter"] = searchString;

        var batches = _context.ProductionBatches
            .Include(pb => pb.Formula)
            .Where(pb => pb.IsActive);

        if (!string.IsNullOrEmpty(searchString))
        {
            batches = batches.Where(pb => pb.BatchCode.Contains(searchString)
                                       || (pb.Formula != null && pb.Formula.FormulaCode.Contains(searchString))
                                       || (pb.LotNumber != null && pb.LotNumber.Contains(searchString)));
        }

        var paginatedList = await PaginatedList<ProductionBatch>.CreateAsync(
            batches.OrderByDescending(pb => pb.ProductionDate), pageNumber, 10);

        return View(paginatedList);
    }

    private bool FormulaExists(int id)
    {
        return _context.Formulas.Any(e => e.Id == id);
    }
}
