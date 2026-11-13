// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Controllers;

[Authorize]
public class INCIsController : Controller
{
    private readonly INCISDbContext _context;

    public INCIsController(INCISDbContext context)
    {
        _context = context;
    }

    // GET: INCIs
    public async Task<IActionResult> Index(string searchString, int pageNumber = 1)
    {
        ViewData["CurrentFilter"] = searchString;

        var incis = from i in _context.INCIs
                    select i;

        if (!String.IsNullOrEmpty(searchString))
        {
            incis = incis.Where(s => s.Name.Contains(searchString)
                                   || (s.FullName != null && s.FullName.Contains(searchString))
                                   || (s.CASNumber != null && s.CASNumber.Contains(searchString))
                                   || (s.ECNumber != null && s.ECNumber.Contains(searchString)));
        }

        var paginatedList = await PaginatedList<INCI>.CreateAsync(incis.OrderBy(i => i.Name), pageNumber, 10);

        return View(paginatedList);
    }

    // GET: INCIs/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var inci = await _context.INCIs
            .Include(i => i.RawMaterials)
            .Include(i => i.Restrictions)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (inci == null)
        {
            return NotFound();
        }

        return View(inci);
    }

    // GET: INCIs/Create
    [Authorize(Roles = AppRoles.WriteAccess)]
    public IActionResult Create()
    {
        return View();
    }

    // POST: INCIs/Create
    [HttpPost]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,CASNumber,ECNumber,Description,Function,Synonyms,IsActive")] INCI inci)
    {
        if (ModelState.IsValid)
        {
            inci.CreatedAt = DateTime.Now;
            _context.Add(inci);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(inci);
    }

    // GET: INCIs/Edit/5
    [Authorize(Roles = AppRoles.WriteAccess)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var inci = await _context.INCIs.FindAsync(id);
        if (inci == null)
        {
            return NotFound();
        }
        return View(inci);
    }

    // POST: INCIs/Edit/5
    [HttpPost]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,CASNumber,ECNumber,IsActive")] INCI inci)
    {
        if (id != inci.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(inci);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!INCIExists(inci.Id))
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
        return View(inci);
    }

    // GET: INCIs/Delete/5
    [Authorize(Roles = AppRoles.WriteAccess)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var inci = await _context.INCIs
            .FirstOrDefaultAsync(m => m.Id == id);
        if (inci == null)
        {
            return NotFound();
        }

        return View(inci);
    }

    // POST: INCIs/Delete/5
    [HttpPost, ActionName("Delete")]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var inci = await _context.INCIs.FindAsync(id);
        if (inci != null)
        {
            _context.INCIs.Remove(inci);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: INCIs/AdvanceSearch
    [Authorize]
    public async Task<IActionResult> AdvanceSearch(string searchString, int pageNumber = 1)
    {
        ViewData["CurrentFilter"] = searchString;

        var incis = from i in _context.INCIs
                    select i;

        if (!String.IsNullOrEmpty(searchString))
        {
            incis = incis.Where(s => s.Name.Contains(searchString)
                                   || (s.FullName != null && s.FullName.Contains(searchString))
                                   || (s.CASNumber != null && s.CASNumber.Contains(searchString))
                                   || (s.ECNumber != null && s.ECNumber.Contains(searchString)));
        }

        // Include raw materials that use this INCI for advance search
        var incisWithRawMaterials = await incis
            .Include(i => i.RawMaterials)
            .OrderBy(i => i.Name)
            .Skip((pageNumber - 1) * 10)
            .Take(10)
            .ToListAsync();

        var totalItems = await incis.CountAsync();
        var viewModel = new PaginatedList<INCI>(incisWithRawMaterials, totalItems, pageNumber, 10);

        return View(viewModel);
    }

    private bool INCIExists(int id)
    {
        return _context.INCIs.Any(e => e.Id == id);
    }
}
