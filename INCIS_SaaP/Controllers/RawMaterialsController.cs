// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Controllers;

[Authorize]
public class RawMaterialsController : Controller
{
    private readonly INCISDbContext _context;

    public RawMaterialsController(INCISDbContext context)
    {
        _context = context;
    }

    // GET: RawMaterials
    public async Task<IActionResult> Index(string searchString, int pageNumber = 1)
    {
        ViewData["CurrentFilter"] = searchString;

        var rawMaterials = from rm in _context.RawMaterials
                           select rm;

        if (!String.IsNullOrEmpty(searchString))
        {
            rawMaterials = rawMaterials.Where(s => s.Code.Contains(searchString)
                                               || s.ProductName.Contains(searchString)
                                               || (s.ManufacturerName != null && s.ManufacturerName.Contains(searchString))
                                               || (s.Category != null && s.Category.Contains(searchString)));
        }

        var paginatedList = await PaginatedList<RawMaterial>.CreateAsync(rawMaterials.OrderBy(rm => rm.ProductName), pageNumber, 10);

        return View(paginatedList);
    }

    // GET: RawMaterials/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rawMaterial = await _context.RawMaterials
            .Include(rm => rm.Supplier)
            .Include(rm => rm.MainINCI)
            .Include(rm => rm.Composition)
                .ThenInclude(rc => rc.INCI)
            .Include(rm => rm.Samples)
            .Include(rm => rm.FormulaRawMaterials)
                .ThenInclude(frm => frm.Formula)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (rawMaterial == null)
        {
            return NotFound();
        }

        return View(rawMaterial);
    }

    // GET: RawMaterials/Create
    [Authorize(Roles = AppRoles.WriteAccess)]
    public IActionResult Create()
    {
        // Get suppliers and INCIs for dropdowns
        ViewBag.SupplierList = _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
        ViewBag.INCIList = _context.INCIs.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
        return View();
    }

    // POST: RawMaterials/Create
    [HttpPost]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Code,PrismaCode,ProductName,ManufacturerName,ManufacturerAddress,ManufacturerPhone,ManufacturerEmail,Allergens,NaturalOriginPercentage,ShelfLife,StorageConditions,Form,Color,Odor,Density,Solubility,Compatibilities,Price,HealthAndSafety,Comments,Category,SupplierId,INCIId,IsFullyRegistered,IsEvaluated,IsActive,HasTDS,HasMSDS,HasISO16128,HasBSE_TSE,HasVegan,HasRMI,HasAllergenCertificate,HasPhthalates_BHT_Statement,HasSolventVehicleStatement,HasMarketingInfo")] RawMaterial rawMaterial)
    {
        if (ModelState.IsValid)
        {
            rawMaterial.CreatedAt = DateTime.Now;
            _context.Add(rawMaterial);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // Get suppliers and INCIs for dropdowns
        ViewBag.SupplierList = _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
        ViewBag.INCIList = _context.INCIs.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
        return View(rawMaterial);
    }

    // GET: RawMaterials/Edit/5
    [Authorize(Roles = AppRoles.WriteAccess)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rawMaterial = await _context.RawMaterials
            .Include(rm => rm.Supplier)
            .Include(rm => rm.MainINCI)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (rawMaterial == null)
        {
            return NotFound();
        }
        // Get suppliers and INCIs for dropdowns
        ViewBag.SupplierList = _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
        ViewBag.INCIList = _context.INCIs.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
        return View(rawMaterial);
    }

    // POST: RawMaterials/Edit/5
    [HttpPost]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Code,PrismaCode,ProductName,ManufacturerName,ManufacturerAddress,ManufacturerPhone,ManufacturerEmail,Allergens,NaturalOriginPercentage,ShelfLife,StorageConditions,Form,Color,Odor,Density,Solubility,Compatibilities,Price,HealthAndSafety,Comments,Category,SupplierId,INCIId,IsFullyRegistered,IsEvaluated,IsActive")] RawMaterial rawMaterial)
    {
        if (id != rawMaterial.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(rawMaterial);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RawMaterialExists(rawMaterial.Id))
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
        // Get suppliers and INCIs for dropdowns
        ViewBag.SupplierList = _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
        ViewBag.INCIList = _context.INCIs.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
        return View(rawMaterial);
    }

    // GET: RawMaterials/Delete/5
    [Authorize(Roles = AppRoles.WriteAccess)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rawMaterial = await _context.RawMaterials
            .FirstOrDefaultAsync(m => m.Id == id);
        if (rawMaterial == null)
        {
            return NotFound();
        }

        return View(rawMaterial);
    }

    // POST: RawMaterials/Delete/5
    [HttpPost, ActionName("Delete")]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var rawMaterial = await _context.RawMaterials.FindAsync(id);
        if (rawMaterial != null)
        {
            _context.RawMaterials.Remove(rawMaterial);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: RawMaterials/AddSample/5
    [Authorize(Roles = AppRoles.WriteAccess)]
    public IActionResult AddSample(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rawMaterial = _context.RawMaterials.Find(id);
        if (rawMaterial == null)
        {
            return NotFound();
        }

        ViewBag.RawMaterial = rawMaterial;
        var sample = new RawMaterialSample
        {
            RawMaterialId = id.Value,
            ReceivedDate = DateTime.Today,
            ExpiryDate = DateTime.Today.AddYears(2)
        };
        return View(sample);
    }

    // POST: RawMaterials/AddSample/5
    [HttpPost]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSample(int id, [Bind("Id,RawMaterialId,LotNumber,ReceivedDate,ExpiryDate")] RawMaterialSample sample)
    {
        if (id != sample.RawMaterialId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            sample.CreatedAt = DateTime.Now;
            _context.Add(sample);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = id });
        }

        var rawMaterial = _context.RawMaterials.Find(id);
        if (rawMaterial == null)
        {
            return NotFound();
        }
        ViewBag.RawMaterial = rawMaterial;
        return View(sample);
    }

    // GET: RawMaterials/SearchFormulas/5
    [Authorize]
    public async Task<IActionResult> SearchFormulas(int? id, string searchString, int pageNumber = 1)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rawMaterial = await _context.RawMaterials.FindAsync(id);
        if (rawMaterial == null)
        {
            return NotFound();
        }

        ViewData["RawMaterialId"] = id;
        ViewData["RawMaterialName"] = rawMaterial.ProductName;
        ViewData["CurrentFilter"] = searchString;

        var formulas = from f in _context.Formulas
                       where f.FormulaRawMaterials.Any(frm => frm.RawMaterialId == id)
                       select f;

        if (!String.IsNullOrEmpty(searchString))
        {
            formulas = formulas.Where(s => s.FormulaCode.Contains(searchString)
                                          || (s.ProductName != null && s.ProductName.Contains(searchString))
                                          || (s.ProductType != null && s.ProductType.Contains(searchString)));
        }

        var paginatedList = await PaginatedList<Formula>.CreateAsync(formulas.OrderByDescending(f => f.LastModified), pageNumber, 10);

        return View(paginatedList);
    }

    // GET: RawMaterials/ExpiredSamples
    public async Task<IActionResult> ExpiredSamples(string searchString)
    {
        ViewData["CurrentFilter"] = searchString;

        var query = _context.RawMaterials
            .Include(rm => rm.Samples)
            .Include(rm => rm.Supplier)
            .Where(rm => rm.Samples.Any(s => s.IsActive && s.ExpiryDate < DateTime.Today));

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(rm => rm.ProductName.Contains(searchString)
                                   || rm.Code.Contains(searchString)
                                   || (rm.Supplier != null && rm.Supplier.Name.Contains(searchString)));
        }

        var rawMaterials = await query.OrderBy(rm => rm.ProductName).ToListAsync();

        // Build view model with only expired samples
        var expiredSamples = rawMaterials
            .SelectMany(rm => rm.Samples
                .Where(s => s.IsActive && s.ExpiryDate < DateTime.Today)
                .Select(s => new ExpiredSampleViewModel
                {
                    RawMaterialId = rm.Id,
                    RawMaterialCode = rm.Code,
                    RawMaterialName = rm.ProductName,
                    SupplierName = rm.Supplier?.Name,
                    LotNumber = s.LotNumber,
                    ReceivedDate = s.ReceivedDate,
                    ExpiryDate = s.ExpiryDate,
                    DaysExpired = (DateTime.Today - s.ExpiryDate).Days
                }))
            .OrderByDescending(s => s.DaysExpired)
            .ToList();

        return View(expiredSamples);
    }

    private bool RawMaterialExists(int id)
    {
        return _context.RawMaterials.Any(e => e.Id == id);
    }
}

/// <summary>
/// View model for expired samples consolidated view
/// </summary>
public class ExpiredSampleViewModel
{
    public int RawMaterialId { get; set; }
    public string RawMaterialCode { get; set; } = string.Empty;
    public string RawMaterialName { get; set; } = string.Empty;
    public string? SupplierName { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysExpired { get; set; }
}
