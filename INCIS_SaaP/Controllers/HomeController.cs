// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using Microsoft.EntityFrameworkCore;

namespace INCIS_SaaP.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly INCISDbContext _context;

    public HomeController(INCISDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Counts
        ViewBag.TotalFormulas = await _context.Formulas.CountAsync();
        ViewBag.TotalINCI = await _context.INCIs.CountAsync();
        ViewBag.TotalRawMaterials = await _context.RawMaterials.CountAsync();
        ViewBag.TotalAllergens = await _context.Allergens.CountAsync();
        ViewBag.TotalClients = await _context.Clients.CountAsync();
        ViewBag.TotalSuppliers = await _context.Suppliers.CountAsync();

        // Active states
        ViewBag.ActiveFormulas = await _context.Formulas.Where(f => f.ValidationDate.HasValue).CountAsync();
        ViewBag.WipFormulas = await _context.Formulas.Where(f => f.IsWIP).CountAsync();
        ViewBag.ActiveRawMaterials = await _context.RawMaterials.Where(rm => rm.IsActive).CountAsync();

        // Time-series last 6 months (formulas created/month)
        var sinceDate = DateTime.UtcNow.AddMonths(-5).Date;
        var monthly = await _context.Formulas
            .Where(f => f.CreationDate.HasValue && f.CreationDate >= sinceDate)
            .GroupBy(f => new { f.CreationDate!.Value.Year, f.CreationDate.Value.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count()
            })
            .ToListAsync();

        var labels = new List<string>();
        var values = new List<int>();
        var elMonths = new[] { "Ιαν", "Φεβ", "Μάρ", "Απρ", "Μάι", "Ιούν", "Ιούλ", "Αύγ", "Σεπ", "Οκτ", "Νοέ", "Δεκ" };
        for (var i = 5; i >= 0; i--)
        {
            var d = DateTime.UtcNow.AddMonths(-i);
            labels.Add(elMonths[d.Month - 1]);
            var item = monthly.FirstOrDefault(m => m.Year == d.Year && m.Month == d.Month);
            values.Add(item?.Count ?? 0);
        }
        ViewBag.MonthlyLabels = labels;
        ViewBag.MonthlyValues = values;

        // Recent formulas (top 5 modified)
        var recentFormulas = await _context.Formulas
            .OrderByDescending(f => f.LastModified ?? f.CreationDate)
            .Take(5)
            .ToListAsync();

        return View(recentFormulas);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = HttpContext.TraceIdentifier
    });
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
