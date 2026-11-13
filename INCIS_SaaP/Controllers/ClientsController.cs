// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Controllers;

[Authorize]
public class ClientsController : Controller
{
    private readonly INCISDbContext _context;

    public ClientsController(INCISDbContext context)
    {
        _context = context;
    }

    // GET: Clients
    public async Task<IActionResult> Index(string searchString, int pageNumber = 1)
    {
        ViewData["CurrentFilter"] = searchString;

        var clients = from c in _context.Clients
                      select c;

        if (!String.IsNullOrEmpty(searchString))
        {
            clients = clients.Where(s => s.Name.Contains(searchString)
                                       || (s.Code != null && s.Code.Contains(searchString))
                                       || (s.Email != null && s.Email.Contains(searchString)));
        }

        var paginatedList = await PaginatedList<Client>.CreateAsync(clients.OrderBy(c => c.Name), pageNumber, 10);

        return View(paginatedList);
    }

    // GET: Clients/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var client = await _context.Clients
            .Include(c => c.Formulas)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (client == null)
        {
            return NotFound();
        }

        return View(client);
    }

    // GET: Clients/Create
    [Authorize(Roles = AppRoles.WriteAccess)]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Clients/Create
    [HttpPost]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,Code,Address,Email,Phone,ContactPerson,ContactPhone,ContactEmail")] Client client)
    {
        if (ModelState.IsValid)
        {
            client.CreatedAt = DateTime.Now;
            _context.Add(client);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(client);
    }

    // GET: Clients/Edit/5
    [Authorize(Roles = AppRoles.WriteAccess)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var client = await _context.Clients.FindAsync(id);
        if (client == null)
        {
            return NotFound();
        }
        return View(client);
    }

    // POST: Clients/Edit/5
    [HttpPost]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,Address,Email,Phone,ContactPerson,ContactPhone,ContactEmail,IsActive")] Client client)
    {
        if (id != client.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(client);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClientExists(client.Id))
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
        return View(client);
    }

    // GET: Clients/Delete/5
    [Authorize(Roles = AppRoles.WriteAccess)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var client = await _context.Clients
            .FirstOrDefaultAsync(m => m.Id == id);
        if (client == null)
        {
            return NotFound();
        }

        return View(client);
    }

    // POST: Clients/Delete/5
    [HttpPost, ActionName("Delete")]
        [Authorize(Roles = AppRoles.WriteAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client != null)
        {
            _context.Clients.Remove(client);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ClientExists(int id)
    {
        return _context.Clients.Any(e => e.Id == id);
    }
}
