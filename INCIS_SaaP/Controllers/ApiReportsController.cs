// Author: Damianos Pythagoras Ougkrenidis
// API Reports Controller για export functionality

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using ClosedXML.Excel;
using System.Security.Claims;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// API Reports Controller με export endpoints
/// </summary>
[ApiController]
[Route("api/v1/reports")]
[Authorize]
[EnableRateLimiting("Strict")]
public class ApiReportsController : ControllerBase
{
    private readonly INCISDbContext _context;
    private readonly ILogger<ApiReportsController> _logger;

    public ApiReportsController(INCISDbContext context, ILogger<ApiReportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Export all clients to Excel
    /// </summary>
    [HttpGet("all-clients")]
    public async Task<IActionResult> ExportAllClients()
    {
        var clients = await _context.Clients
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Clients");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "Code";
        worksheet.Cell(1, 4).Value = "Address";
        worksheet.Cell(1, 5).Value = "Email";
        worksheet.Cell(1, 6).Value = "Phone";
        worksheet.Cell(1, 7).Value = "Contact Person";
        worksheet.Cell(1, 8).Value = "Contact Phone";
        worksheet.Cell(1, 9).Value = "Contact Email";
        worksheet.Cell(1, 10).Value = "Created At";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 10);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        // Data
        int row = 2;
        foreach (var client in clients)
        {
            worksheet.Cell(row, 1).Value = client.Id;
            worksheet.Cell(row, 2).Value = client.Name;
            worksheet.Cell(row, 3).Value = client.Code;
            worksheet.Cell(row, 4).Value = client.Address;
            worksheet.Cell(row, 5).Value = client.Email;
            worksheet.Cell(row, 6).Value = client.Phone;
            worksheet.Cell(row, 7).Value = client.ContactPerson;
            worksheet.Cell(row, 8).Value = client.ContactPhone;
            worksheet.Cell(row, 9).Value = client.ContactEmail;
            worksheet.Cell(row, 10).Value = client.CreatedAt;
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        _logger.LogInformation("All clients exported by user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Clients_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    /// <summary>
    /// Export all suppliers to Excel
    /// </summary>
    [HttpGet("all-suppliers")]
    public async Task<IActionResult> ExportAllSuppliers()
    {
        var suppliers = await _context.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Suppliers");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "Address";
        worksheet.Cell(1, 4).Value = "Email";
        worksheet.Cell(1, 5).Value = "Phone";
        worksheet.Cell(1, 6).Value = "Created At";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        // Data
        int row = 2;
        foreach (var supplier in suppliers)
        {
            worksheet.Cell(row, 1).Value = supplier.Id;
            worksheet.Cell(row, 2).Value = supplier.Name;
            worksheet.Cell(row, 3).Value = supplier.Address;
            worksheet.Cell(row, 4).Value = supplier.Email;
            worksheet.Cell(row, 5).Value = supplier.Phone;
            worksheet.Cell(row, 6).Value = supplier.CreatedAt;
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        _logger.LogInformation("All suppliers exported by user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Suppliers_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    /// <summary>
    /// Export all INCIS to Excel
    /// </summary>
    [HttpGet("all-incis")]
    public async Task<IActionResult> ExportAllINCIS()
    {
        var incis = await _context.INCIs
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("INCIS");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "INCI Name";
        worksheet.Cell(1, 3).Value = "CAS Number";
        worksheet.Cell(1, 4).Value = "EC Number";
        worksheet.Cell(1, 5).Value = "Description";
        worksheet.Cell(1, 6).Value = "Function";
        worksheet.Cell(1, 7).Value = "Synonyms";
        worksheet.Cell(1, 8).Value = "Created At";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 8);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        // Data
        int row = 2;
        foreach (var inci in incis)
        {
            worksheet.Cell(row, 1).Value = inci.Id;
            worksheet.Cell(row, 2).Value = inci.Name;
            worksheet.Cell(row, 3).Value = inci.CASNumber;
            worksheet.Cell(row, 4).Value = inci.ECNumber;
            worksheet.Cell(row, 5).Value = inci.Description;
            worksheet.Cell(row, 6).Value = inci.Function;
            worksheet.Cell(row, 7).Value = inci.Synonyms;
            worksheet.Cell(row, 8).Value = inci.CreatedAt;
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        _logger.LogInformation("All INCIS exported by user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"INCIS_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    /// <summary>
    /// Export all raw materials to Excel
    /// </summary>
    [HttpGet("all-raw-materials")]
    public async Task<IActionResult> ExportAllRawMaterials()
    {
        var rawMaterials = await _context.RawMaterials
            .Include(r => r.Supplier)
            .Include(r => r.MainINCI)
            .Where(r => r.IsActive)
            .OrderBy(r => r.ProductName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Raw Materials");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Code";
        worksheet.Cell(1, 3).Value = "Product Name";
        worksheet.Cell(1, 4).Value = "Manufacturer";
        worksheet.Cell(1, 5).Value = "Category";
        worksheet.Cell(1, 6).Value = "Supplier";
        worksheet.Cell(1, 7).Value = "Main INCI";
        worksheet.Cell(1, 8).Value = "Price";
        worksheet.Cell(1, 9).Value = "Natural %";
        worksheet.Cell(1, 10).Value = "Shelf Life";
        worksheet.Cell(1, 11).Value = "Storage Conditions";
        worksheet.Cell(1, 12).Value = "Form";
        worksheet.Cell(1, 13).Value = "Color";
        worksheet.Cell(1, 14).Value = "Fully Registered";
        worksheet.Cell(1, 15).Value = "Evaluated";
        worksheet.Cell(1, 16).Value = "Created At";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 16);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        // Data
        int row = 2;
        foreach (var rm in rawMaterials)
        {
            worksheet.Cell(row, 1).Value = rm.Id;
            worksheet.Cell(row, 2).Value = rm.Code;
            worksheet.Cell(row, 3).Value = rm.ProductName;
            worksheet.Cell(row, 4).Value = rm.ManufacturerName;
            worksheet.Cell(row, 5).Value = rm.Category;
            worksheet.Cell(row, 6).Value = rm.Supplier?.Name;
            worksheet.Cell(row, 7).Value = rm.MainINCI?.Name;
            worksheet.Cell(row, 8).Value = rm.Price;
            worksheet.Cell(row, 9).Value = rm.NaturalOriginPercentage;
            worksheet.Cell(row, 10).Value = rm.ShelfLife;
            worksheet.Cell(row, 11).Value = rm.StorageConditions;
            worksheet.Cell(row, 12).Value = rm.Form;
            worksheet.Cell(row, 13).Value = rm.Color;
            worksheet.Cell(row, 14).Value = rm.IsFullyRegistered ? "Yes" : "No";
            worksheet.Cell(row, 15).Value = rm.IsEvaluated ? "Yes" : "No";
            worksheet.Cell(row, 16).Value = rm.CreatedAt;
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        _logger.LogInformation("All raw materials exported by user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"RawMaterials_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    /// <summary>
    /// Export used raw materials (materials that appear in at least one formula)
    /// </summary>
    [HttpGet("used-raw-materials")]
    public async Task<IActionResult> ExportUsedRawMaterials()
    {
        var usedRawMaterials = await _context.FormulaRawMaterials
            .Where(frm => frm.IsActive)
            .Include(frm => frm.RawMaterial)
            .ThenInclude(rm => rm!.Supplier)
            .Select(frm => frm.RawMaterial)
            .Distinct()
            .Where(rm => rm != null && rm.IsActive)
            .OrderBy(rm => rm!.ProductName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Used Raw Materials");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Code";
        worksheet.Cell(1, 3).Value = "Product Name";
        worksheet.Cell(1, 4).Value = "Manufacturer";
        worksheet.Cell(1, 5).Value = "Category";
        worksheet.Cell(1, 6).Value = "Supplier";
        worksheet.Cell(1, 7).Value = "Price";
        worksheet.Cell(1, 8).Value = "Usage Count";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 8);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        // Data
        int row = 2;
        foreach (var rm in usedRawMaterials)
        {
            if (rm == null) continue;

            var usageCount = await _context.FormulaRawMaterials
                .Where(frm => frm.RawMaterialId == rm.Id && frm.IsActive)
                .CountAsync();

            worksheet.Cell(row, 1).Value = rm.Id;
            worksheet.Cell(row, 2).Value = rm.Code;
            worksheet.Cell(row, 3).Value = rm.ProductName;
            worksheet.Cell(row, 4).Value = rm.ManufacturerName;
            worksheet.Cell(row, 5).Value = rm.Category;
            worksheet.Cell(row, 6).Value = rm.Supplier?.Name;
            worksheet.Cell(row, 7).Value = rm.Price;
            worksheet.Cell(row, 8).Value = usageCount;
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        _logger.LogInformation("Used raw materials exported by user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"UsedRawMaterials_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    /// <summary>
    /// Export unused raw materials (materials that don't appear in any formula)
    /// </summary>
    [HttpGet("unused-raw-materials")]
    public async Task<IActionResult> ExportUnusedRawMaterials()
    {
        var usedRawMaterialIds = await _context.FormulaRawMaterials
            .Where(frm => frm.IsActive)
            .Select(frm => frm.RawMaterialId)
            .Distinct()
            .ToListAsync();

        var unusedRawMaterials = await _context.RawMaterials
            .Include(r => r.Supplier)
            .Where(r => r.IsActive && !usedRawMaterialIds.Contains(r.Id))
            .OrderBy(r => r.ProductName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Unused Raw Materials");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Code";
        worksheet.Cell(1, 3).Value = "Product Name";
        worksheet.Cell(1, 4).Value = "Manufacturer";
        worksheet.Cell(1, 5).Value = "Category";
        worksheet.Cell(1, 6).Value = "Supplier";
        worksheet.Cell(1, 7).Value = "Price";
        worksheet.Cell(1, 8).Value = "Created At";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 8);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        // Data
        int row = 2;
        foreach (var rm in unusedRawMaterials)
        {
            worksheet.Cell(row, 1).Value = rm.Id;
            worksheet.Cell(row, 2).Value = rm.Code;
            worksheet.Cell(row, 3).Value = rm.ProductName;
            worksheet.Cell(row, 4).Value = rm.ManufacturerName;
            worksheet.Cell(row, 5).Value = rm.Category;
            worksheet.Cell(row, 6).Value = rm.Supplier?.Name;
            worksheet.Cell(row, 7).Value = rm.Price;
            worksheet.Cell(row, 8).Value = rm.CreatedAt;
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        _logger.LogInformation("Unused raw materials exported by user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"UnusedRawMaterials_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    /// <summary>
    /// Export expired samples
    /// </summary>
    [HttpGet("expired-samples")]
    public async Task<IActionResult> ExportExpiredSamples()
    {
        var today = DateTime.Now;
        var expiredSamples = await _context.Set<RawMaterialSample>()
            .Include(s => s.RawMaterial)
            .Where(s => s.IsActive && s.ExpiryDate < today)
            .OrderBy(s => s.ExpiryDate)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Expired Samples");

        // Headers
        worksheet.Cell(1, 1).Value = "Sample ID";
        worksheet.Cell(1, 2).Value = "Raw Material Code";
        worksheet.Cell(1, 3).Value = "Raw Material Name";
        worksheet.Cell(1, 4).Value = "Lot Number";
        worksheet.Cell(1, 5).Value = "Received Date";
        worksheet.Cell(1, 6).Value = "Expiry Date";
        worksheet.Cell(1, 7).Value = "Days Expired";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.Red;
        headerRange.Style.Font.FontColor = XLColor.White;

        // Data
        int row = 2;
        foreach (var sample in expiredSamples)
        {
            var daysExpired = (today - sample.ExpiryDate).Days;

            worksheet.Cell(row, 1).Value = sample.Id;
            worksheet.Cell(row, 2).Value = sample.RawMaterial?.Code;
            worksheet.Cell(row, 3).Value = sample.RawMaterial?.ProductName;
            worksheet.Cell(row, 4).Value = sample.LotNumber;
            worksheet.Cell(row, 5).Value = sample.ReceivedDate;
            worksheet.Cell(row, 6).Value = sample.ExpiryDate;
            worksheet.Cell(row, 7).Value = daysExpired;

            // Highlight rows that are expired more than 30 days
            if (daysExpired > 30)
            {
                worksheet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.LightPink;
            }

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        _logger.LogInformation("Expired samples exported by user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ExpiredSamples_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    /// <summary>
    /// Get expired samples (JSON format for viewing)
    /// </summary>
    [HttpGet("expired-samples/view")]
    public async Task<IActionResult> GetExpiredSamples()
    {
        var today = DateTime.Now;
        var expiredSamples = await _context.Set<RawMaterialSample>()
            .Include(s => s.RawMaterial)
            .Where(s => s.IsActive && s.ExpiryDate < today)
            .OrderBy(s => s.ExpiryDate)
            .Select(s => new
            {
                s.Id,
                RawMaterialCode = s.RawMaterial!.Code,
                RawMaterialName = s.RawMaterial.ProductName,
                s.LotNumber,
                s.ReceivedDate,
                s.ExpiryDate,
                DaysExpired = (today - s.ExpiryDate).Days
            })
            .ToListAsync();

        return Ok(new
        {
            total = expiredSamples.Count,
            data = expiredSamples
        });
    }
}
