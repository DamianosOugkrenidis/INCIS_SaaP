// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using System.Text;
using ClosedXML.Excel;
using System.IO;

namespace INCIS_SaaP.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly INCISDbContext _context;
    private readonly ICacheService _cache;

    public ReportsController(INCISDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    // Απλό DTO για τα dashboard counts (JSON-serializable για το distributed cache).
    private sealed class ReportCounts
    {
        public int Clients { get; set; }
        public int Suppliers { get; set; }
        public int INCIs { get; set; }
        public int RawMaterials { get; set; }
        public int Formulas { get; set; }
    }

    // GET: Reports
    public async Task<IActionResult> Index()
    {
        // Τα counts αλλάζουν σπάνια σε σχέση με τη συχνότητα προβολής του dashboard:
        // cache-aside με σύντομο TTL μειώνει 5 COUNT queries ανά request.
        var counts = await _cache.GetOrCreateAsync("reports:dashboard-counts", async ct => new ReportCounts
        {
            Clients = await _context.Clients.CountAsync(ct),
            Suppliers = await _context.Suppliers.CountAsync(ct),
            INCIs = await _context.INCIs.CountAsync(ct),
            RawMaterials = await _context.RawMaterials.CountAsync(ct),
            Formulas = await _context.Formulas.CountAsync(ct)
        }, TimeSpan.FromMinutes(2));

        ViewBag.ClientsCount = counts.Clients;
        ViewBag.SuppliersCount = counts.Suppliers;
        ViewBag.INCIsCount = counts.INCIs;
        ViewBag.RawMaterialsCount = counts.RawMaterials;
        ViewBag.FormulasCount = counts.Formulas;
        return View();
    }

    // GET: Reports/ExportClients
    public async Task<IActionResult> ExportClients()
    {
        var clients = await _context.Clients
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Clients");
        var currentRow = 1;

        // Headers
        worksheet.Cell(currentRow, 1).Value = "ID";
        worksheet.Cell(currentRow, 2).Value = "Όνομα";
        worksheet.Cell(currentRow, 3).Value = "Κωδικός";
        worksheet.Cell(currentRow, 4).Value = "Email";
        worksheet.Cell(currentRow, 5).Value = "Τηλέφωνo";
        worksheet.Cell(currentRow, 6).Value = "Διεύθυνση";
        worksheet.Cell(currentRow, 7).Value = "Επικοινωνία";
        worksheet.Cell(currentRow, 8).Value = "Email Επικοινωνίας";
        worksheet.Cell(currentRow, 9).Value = "Τηλέφωνο Επικοινωνίας";
        worksheet.Cell(currentRow, 10).Value = "Ημερομηνία Δημιουργίας";
        worksheet.Cell(currentRow, 11).Value = "Ενεργός";

        // Style headers
        var headerRange = worksheet.Range(currentRow, 1, currentRow, 11);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        currentRow++;

        // Data
        foreach (var client in clients)
        {
            worksheet.Cell(currentRow, 1).Value = client.Id;
            worksheet.Cell(currentRow, 2).Value = client.Name;
            worksheet.Cell(currentRow, 3).Value = client.Code ?? "";
            worksheet.Cell(currentRow, 4).Value = client.Email ?? "";
            worksheet.Cell(currentRow, 5).Value = client.Phone ?? "";
            worksheet.Cell(currentRow, 6).Value = client.Address ?? "";
            worksheet.Cell(currentRow, 7).Value = client.ContactPerson ?? "";
            worksheet.Cell(currentRow, 8).Value = client.ContactEmail ?? "";
            worksheet.Cell(currentRow, 9).Value = client.ContactPhone ?? "";
            worksheet.Cell(currentRow, 10).Value = client.CreatedAt.ToString("dd/MM/yyyy");
            worksheet.Cell(currentRow, 11).Value = client.IsActive ? "Ναι" : "Όχι";
            currentRow++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Clients_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }

    // GET: Reports/ExportSuppliers
    public async Task<IActionResult> ExportSuppliers()
    {
        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Suppliers");
        var currentRow = 1;

        // Headers
        worksheet.Cell(currentRow, 1).Value = "ID";
        worksheet.Cell(currentRow, 2).Value = "Όνομα";
        worksheet.Cell(currentRow, 3).Value = "Dieuthinsi";
        worksheet.Cell(currentRow, 4).Value = "Email";
        worksheet.Cell(currentRow, 5).Value = "Τηλέφωνo";
        worksheet.Cell(currentRow, 6).Value = "Ημερομηνία Δημιουργίας";
        worksheet.Cell(currentRow, 7).Value = "Ενεργός";

        // Style headers
        var headerRange = worksheet.Range(currentRow, 1, currentRow, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        currentRow++;

        // Data
        foreach (var supplier in suppliers)
        {
            worksheet.Cell(currentRow, 1).Value = supplier.Id;
            worksheet.Cell(currentRow, 2).Value = supplier.Name;
            worksheet.Cell(currentRow, 3).Value = supplier.Address ?? "";
            worksheet.Cell(currentRow, 4).Value = supplier.Email ?? "";
            worksheet.Cell(currentRow, 5).Value = supplier.Phone ?? "";
            worksheet.Cell(currentRow, 6).Value = supplier.CreatedAt.ToString("dd/MM/yyyy");
            worksheet.Cell(currentRow, 7).Value = supplier.IsActive ? "Ναι" : "Όχι";
            currentRow++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Suppliers_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }

    // GET: Reports/ExportINCIs
    public async Task<IActionResult> ExportINCIs()
    {
        var incis = await _context.INCIs
            .AsNoTracking()
            .OrderBy(i => i.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("INCIs");
        var currentRow = 1;

        // Headers
        worksheet.Cell(currentRow, 1).Value = "ID";
        worksheet.Cell(currentRow, 2).Value = "Όνομα INCI";
        worksheet.Cell(currentRow, 3).Value = "CAS Number";
        worksheet.Cell(currentRow, 4).Value = "EC Number";
        worksheet.Cell(currentRow, 5).Value = "Ημερομηνία Δημιουργίας";
        worksheet.Cell(currentRow, 6).Value = "Ενεργός";

        // Style headers
        var headerRange = worksheet.Range(currentRow, 1, currentRow, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        currentRow++;

        // Data
        foreach (var inci in incis)
        {
            worksheet.Cell(currentRow, 1).Value = inci.Id;
            worksheet.Cell(currentRow, 2).Value = inci.Name;
            worksheet.Cell(currentRow, 3).Value = inci.CASNumber ?? "";
            worksheet.Cell(currentRow, 4).Value = inci.ECNumber ?? "";
            worksheet.Cell(currentRow, 5).Value = inci.CreatedAt.ToString("dd/MM/yyyy");
            worksheet.Cell(currentRow, 6).Value = inci.IsActive ? "Ναι" : "Όχι";
            currentRow++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"INCIs_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }

    // GET: Reports/ExportFormulas
    public async Task<IActionResult> ExportFormulas()
    {
        var formulas = await _context.Formulas
            .AsNoTracking()
            .Include(f => f.Client)
            .Include(f => f.FormulaRawMaterials)
            .OrderBy(f => f.FormulaCode)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Formulas");
        var currentRow = 1;

        // Headers
        worksheet.Cell(currentRow, 1).Value = "ID";
        worksheet.Cell(currentRow, 2).Value = "Κωδικός Φόρμουλας";
        worksheet.Cell(currentRow, 3).Value = "Όνομα Προϊόντος";
        worksheet.Cell(currentRow, 4).Value = "Τύπος";
        worksheet.Cell(currentRow, 5).Value = "Πελάτης";
        worksheet.Cell(currentRow, 6).Value = "Τόπος Παραγωγής";
        worksheet.Cell(currentRow, 7).Value = "Μέθοδος Παραγωγής";
        worksheet.Cell(currentRow, 8).Value = "Αρ. Α' Υλών";
        worksheet.Cell(currentRow, 9).Value = "WIP";
        worksheet.Cell(currentRow, 10).Value = "Validated";
        worksheet.Cell(currentRow, 11).Value = "Ημ. Δημιουργίας";
        worksheet.Cell(currentRow, 12).Value = "Τελ. Τροποποίηση";

        var headerRange = worksheet.Range(currentRow, 1, currentRow, 12);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        currentRow++;

        foreach (var f in formulas)
        {
            worksheet.Cell(currentRow, 1).Value = f.Id;
            worksheet.Cell(currentRow, 2).Value = f.FormulaCode;
            worksheet.Cell(currentRow, 3).Value = f.ProductName ?? "";
            worksheet.Cell(currentRow, 4).Value = f.ProductType ?? "";
            worksheet.Cell(currentRow, 5).Value = f.Client?.Name ?? "";
            worksheet.Cell(currentRow, 6).Value = f.ManufacturingSite ?? "";
            worksheet.Cell(currentRow, 7).Value = f.ManufacturingMethod ?? "";
            worksheet.Cell(currentRow, 8).Value = f.FormulaRawMaterials.Count(frm => frm.IsActive);
            worksheet.Cell(currentRow, 9).Value = f.IsWIP ? "Ναι" : "Όχι";
            worksheet.Cell(currentRow, 10).Value = f.IsValidated ? "Ναι" : "Όχι";
            worksheet.Cell(currentRow, 11).Value = f.CreationDate?.ToString("dd/MM/yyyy") ?? "";
            worksheet.Cell(currentRow, 12).Value = f.LastModified?.ToString("dd/MM/yyyy") ?? "";
            currentRow++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Formulas_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    // GET: Reports/ExportUsedRawMaterials
    public async Task<IActionResult> ExportUsedRawMaterials()
    {
        var usedRawMaterialIds = await _context.FormulaRawMaterials
            .Where(frm => frm.IsActive)
            .Select(frm => frm.RawMaterialId)
            .Distinct()
            .ToListAsync();

        var rawMaterials = await _context.RawMaterials
            .AsNoTracking()
            .Include(rm => rm.Supplier)
            .Where(rm => usedRawMaterialIds.Contains(rm.Id))
            .OrderBy(rm => rm.ProductName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Used Raw Materials");
        var currentRow = 1;

        worksheet.Cell(currentRow, 1).Value = "ID";
        worksheet.Cell(currentRow, 2).Value = "Κωδικός";
        worksheet.Cell(currentRow, 3).Value = "Όνομα Προϊόντος";
        worksheet.Cell(currentRow, 4).Value = "Προμηθευτής";
        worksheet.Cell(currentRow, 5).Value = "Κατηγορία";
        worksheet.Cell(currentRow, 6).Value = "Τιμή";
        worksheet.Cell(currentRow, 7).Value = "Ενεργός";

        var headerRange = worksheet.Range(currentRow, 1, currentRow, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        currentRow++;

        foreach (var rm in rawMaterials)
        {
            worksheet.Cell(currentRow, 1).Value = rm.Id;
            worksheet.Cell(currentRow, 2).Value = rm.Code;
            worksheet.Cell(currentRow, 3).Value = rm.ProductName;
            worksheet.Cell(currentRow, 4).Value = rm.Supplier?.Name ?? "";
            worksheet.Cell(currentRow, 5).Value = rm.Category ?? "";
            worksheet.Cell(currentRow, 6).Value = rm.Price ?? 0;
            worksheet.Cell(currentRow, 7).Value = rm.IsActive ? "Ναι" : "Όχι";
            currentRow++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"UsedRawMaterials_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    // GET: Reports/ExportUnusedRawMaterials
    public async Task<IActionResult> ExportUnusedRawMaterials()
    {
        var usedRawMaterialIds = await _context.FormulaRawMaterials
            .Where(frm => frm.IsActive)
            .Select(frm => frm.RawMaterialId)
            .Distinct()
            .ToListAsync();

        var rawMaterials = await _context.RawMaterials
            .AsNoTracking()
            .Include(rm => rm.Supplier)
            .Where(rm => !usedRawMaterialIds.Contains(rm.Id))
            .OrderBy(rm => rm.ProductName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Unused Raw Materials");
        var currentRow = 1;

        worksheet.Cell(currentRow, 1).Value = "ID";
        worksheet.Cell(currentRow, 2).Value = "Κωδικός";
        worksheet.Cell(currentRow, 3).Value = "Όνομα Προϊόντος";
        worksheet.Cell(currentRow, 4).Value = "Προμηθευτής";
        worksheet.Cell(currentRow, 5).Value = "Κατηγορία";
        worksheet.Cell(currentRow, 6).Value = "Τιμή";
        worksheet.Cell(currentRow, 7).Value = "Ενεργός";

        var headerRange = worksheet.Range(currentRow, 1, currentRow, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        currentRow++;

        foreach (var rm in rawMaterials)
        {
            worksheet.Cell(currentRow, 1).Value = rm.Id;
            worksheet.Cell(currentRow, 2).Value = rm.Code;
            worksheet.Cell(currentRow, 3).Value = rm.ProductName;
            worksheet.Cell(currentRow, 4).Value = rm.Supplier?.Name ?? "";
            worksheet.Cell(currentRow, 5).Value = rm.Category ?? "";
            worksheet.Cell(currentRow, 6).Value = rm.Price ?? 0;
            worksheet.Cell(currentRow, 7).Value = rm.IsActive ? "Ναι" : "Όχι";
            currentRow++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"UnusedRawMaterials_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    // GET: Reports/ExportRawMaterials
    public async Task<IActionResult> ExportRawMaterials()
    {
        var rawMaterials = await _context.RawMaterials
            .AsNoTracking()
            .Include(rm => rm.Supplier)
            .Include(rm => rm.MainINCI)
            .OrderBy(rm => rm.ProductName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("RawMaterials");
        var currentRow = 1;

        // Headers
        worksheet.Cell(currentRow, 1).Value = "ID";
        worksheet.Cell(currentRow, 2).Value = "Κωδικός";
        worksheet.Cell(currentRow, 3).Value = "Όνομα Προϊόντος";
        worksheet.Cell(currentRow, 4).Value = "Προμηθευτής";
        worksheet.Cell(currentRow, 5).Value = "Κύρια Ουσία INCI";
        worksheet.Cell(currentRow, 6).Value = "Κατηγορία";
        worksheet.Cell(currentRow, 7).Value = "Τιμή";
        worksheet.Cell(currentRow, 8).Value = "Φυσικό Περιεχόμενο %";
        worksheet.Cell(currentRow, 9).Value = "Ημερομηνία Δημιουργίας";
        worksheet.Cell(currentRow, 10).Value = "Πλήρως Καταχωρημένο";
        worksheet.Cell(currentRow, 11).Value = "Αξιολογημένο";
        worksheet.Cell(currentRow, 12).Value = "Ενεργός";

        // Style headers
        var headerRange = worksheet.Range(currentRow, 1, currentRow, 12);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        currentRow++;

        // Data
        foreach (var rm in rawMaterials)
        {
            worksheet.Cell(currentRow, 1).Value = rm.Id;
            worksheet.Cell(currentRow, 2).Value = rm.Code;
            worksheet.Cell(currentRow, 3).Value = rm.ProductName;
            worksheet.Cell(currentRow, 4).Value = rm.Supplier?.Name ?? "";
            worksheet.Cell(currentRow, 5).Value = rm.MainINCI?.Name ?? "";
            worksheet.Cell(currentRow, 6).Value = rm.Category ?? "";
            worksheet.Cell(currentRow, 7).Value = rm.Price ?? 0;
            worksheet.Cell(currentRow, 8).Value = rm.NaturalOriginPercentage ?? 0;
            worksheet.Cell(currentRow, 9).Value = rm.CreatedAt.ToString("dd/MM/yyyy");
            worksheet.Cell(currentRow, 10).Value = rm.IsFullyRegistered ? "Ναι" : "Όχι";
            worksheet.Cell(currentRow, 11).Value = rm.IsEvaluated ? "Ναι" : "Όχι";
            worksheet.Cell(currentRow, 12).Value = rm.IsActive ? "Ναι" : "Όχι";
            currentRow++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"RawMaterials_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        );
    }
}