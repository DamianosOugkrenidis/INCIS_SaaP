// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class ToolsController : Controller
{
    private readonly INCISDbContext _context;

    public ToolsController(INCISDbContext context)
    {
        _context = context;
    }

    // GET: Tools
    public async Task<IActionResult> Index()
    {
        var settings = await _context.SystemSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new SystemSettings();
            _context.SystemSettings.Add(settings);
            await _context.SaveChangesAsync();
        }
        return View(settings);
    }

    // POST: Tools/SetPasswords
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPasswords(SystemSettings model)
    {
        if (ModelState.IsValid)
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SystemSettings();
                _context.SystemSettings.Add(settings);
            }

            settings.RawMaterialPassword = model.RawMaterialPassword;
            settings.FormulasPassword = model.FormulasPassword;
            settings.LastModified = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Οι κωδικοί έχουν ενημερωθεί με επιτυχία.";
            return RedirectToAction(nameof(Index));
        }
        return View("Index", model);
    }
}