// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Controllers;

[Authorize]
public class SuppliersController : Controller
{
    private readonly INCISDbContext _context;

    public SuppliersController(INCISDbContext context)
    {
        _context = context;
    }

    // GET: Suppliers
    public async Task<IActionResult> Index(string searchString, int pageNumber = 1)
    {
        ViewData["CurrentFilter"] = searchString;

        var suppliers = from s in _context.Suppliers
                        select s;

        if (!String.IsNullOrEmpty(searchString))
        {
            suppliers = suppliers.Where(s => s.Name.Contains(searchString)
                                           || (s.Address != null && s.Address.Contains(searchString))
                                           || (s.Email != null && s.Email.Contains(searchString)));
        }

        var paginatedList = await PaginatedList<Supplier>.CreateAsync(suppliers.OrderBy(s => s.Name), pageNumber, 10);

        return View(paginatedList);
    }

    // GET: Suppliers/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var supplier = await _context.Suppliers
            .Include(s => s.RawMaterials)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (supplier == null)
        {
            return NotFound();
        }

        return View(supplier);
    }

    // GET: Suppliers/Create
    [Authorize(Roles = AppRoles.WriteAccess)]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Suppliers/Create
    [HttpPost]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,Address,Email,Phone")] Supplier supplier)
    {
        if (ModelState.IsValid)
        {
            supplier.CreatedAt = DateTime.Now;
            _context.Add(supplier);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(supplier);
    }

    // GET: Suppliers/Edit/5
    [Authorize(Roles = AppRoles.WriteAccess)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
        {
            return NotFound();
        }
        return View(supplier);
    }

    // POST: Suppliers/Edit/5
    [HttpPost]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Address,Email,Phone,IsActive")] Supplier supplier)
    {
        if (id != supplier.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(supplier);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SupplierExists(supplier.Id))
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
        return View(supplier);
    }

    // GET: Suppliers/Delete/5
    [Authorize(Roles = AppRoles.WriteAccess)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(m => m.Id == id);
        if (supplier == null)
        {
            return NotFound();
        }

        return View(supplier);
    }

    // POST: Suppliers/Delete/5
    [HttpPost, ActionName("Delete")]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier != null)
        {
            _context.Suppliers.Remove(supplier);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool SupplierExists(int id)
    {
        return _context.Suppliers.Any(e => e.Id == id);
    }
}
