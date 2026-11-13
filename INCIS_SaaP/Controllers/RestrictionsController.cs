// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Controllers;

[Authorize]
public class RestrictionsController : Controller
{
    private readonly INCISDbContext _context;

    public RestrictionsController(INCISDbContext context)
    {
        _context = context;
    }

    // GET: Restrictions
    public async Task<IActionResult> Index(string searchString, int pageNumber = 1)
    {
        ViewData["CurrentFilter"] = searchString;

        var restrictions = from r in _context.Restrictions
                           select r;

        if (!String.IsNullOrEmpty(searchString))
        {
            restrictions = restrictions.Where(s => s.Regulation != null && s.Regulation.Contains(searchString)
                                               || (s.Description != null && s.Description.Contains(searchString)));
        }

        var paginatedList = await PaginatedList<Restriction>.CreateAsync(restrictions.OrderBy(r => r.CreatedAt), pageNumber, 10);

        return View(paginatedList);
    }

    // GET: Restrictions/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var restriction = await _context.Restrictions
            .Include(r => r.INCI)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (restriction == null)
        {
            return NotFound();
        }

        return View(restriction);
    }

    // GET: Restrictions/Create
    [Authorize(Roles = AppRoles.WriteAccess)]
    public IActionResult Create()
    {
        // Get list of INCIs for dropdown
        ViewBag.INCIList = _context.INCIs.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
        return View();
    }

    // POST: Restrictions/Create
    [HttpPost]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,INCIId,Regulation,Description,MaxPercentage,IsRinseOffOnly")] Restriction restriction)
    {
        if (ModelState.IsValid)
        {
            restriction.CreatedAt = DateTime.Now;
            _context.Add(restriction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // Get list of INCIs for dropdown
        ViewBag.INCIList = _context.INCIs.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
        return View(restriction);
    }

    // GET: Restrictions/Edit/5
    [Authorize(Roles = AppRoles.WriteAccess)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var restriction = await _context.Restrictions
            .Include(r => r.INCI)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (restriction == null)
        {
            return NotFound();
        }
        // Get list of INCIs for dropdown
        ViewBag.INCIList = _context.INCIs.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
        return View(restriction);
    }

    // POST: Restrictions/Edit/5
    [HttpPost]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,INCIId,Regulation,Description,MaxPercentage,IsRinseOffOnly,IsActive")] Restriction restriction)
    {
        if (id != restriction.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(restriction);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RestrictionExists(restriction.Id))
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
        // Get list of INCIs for dropdown
        ViewBag.INCIList = _context.INCIs.Where(i => i.IsActive).OrderBy(i => i.Name).ToList();
        return View(restriction);
    }

    // GET: Restrictions/Delete/5
    [Authorize(Roles = AppRoles.WriteAccess)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var restriction = await _context.Restrictions
            .Include(r => r.INCI)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (restriction == null)
        {
            return NotFound();
        }

        return View(restriction);
    }

    // POST: Restrictions/Delete/5
    [HttpPost, ActionName("Delete")]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var restriction = await _context.Restrictions.FindAsync(id);
        if (restriction != null)
        {
            _context.Restrictions.Remove(restriction);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool RestrictionExists(int id)
    {
        return _context.Restrictions.Any(e => e.Id == id);
    }
}
