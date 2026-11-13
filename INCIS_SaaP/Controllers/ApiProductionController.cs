// Author: Damianos Pythagoras Ougkrenidis
// Production Batch API Controller

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Infrastructure.ApiEnvelope;
using INCIS_SaaP.Infrastructure.Exceptions;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// API Controller for Production Batch management (Produce Formulas)
/// </summary>
[ApiController]
[Route("api/v1/production")]
[Authorize]
[EnableRateLimiting("Strict")]
public class ApiProductionController : ControllerBase
{
    private readonly INCISDbContext _context;
    private readonly ILogger<ApiProductionController> _logger;

    public ApiProductionController(INCISDbContext context, ILogger<ApiProductionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all production batches (paginated)
    /// </summary>
    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _context.ProductionBatches
            .Include(pb => pb.Formula)
            .Where(pb => pb.IsActive);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(pb => pb.Status == status);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(pb => pb.BatchCode.Contains(search)
                || (pb.Formula != null && pb.Formula.FormulaCode.Contains(search))
                || (pb.LotNumber != null && pb.LotNumber.Contains(search)));

        var total = await query.CountAsync();

        var batches = await query
            .OrderByDescending(pb => pb.ProductionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(pb => new ProductionBatchDto
            {
                Id = pb.Id,
                BatchCode = pb.BatchCode,
                FormulaId = pb.FormulaId,
                FormulaCode = pb.Formula != null ? pb.Formula.FormulaCode : null,
                FormulaName = pb.Formula != null ? pb.Formula.ProductName : null,
                BatchSize = pb.BatchSize,
                Unit = pb.Unit,
                Status = pb.Status,
                ProductionDate = pb.ProductionDate,
                CompletedAt = pb.CompletedAt,
                Operator = pb.Operator,
                ManufacturingSite = pb.ManufacturingSite,
                LotNumber = pb.LotNumber,
                Notes = pb.Notes
            })
            .ToListAsync();

        return Ok(ApiResponse<List<ProductionBatchDto>>.Paginated(batches, total, page, pageSize));
    }

    /// <summary>
    /// Get production batch details by ID (including calculated quantities)
    /// </summary>
    [HttpGet("batches/{id}")]
    public async Task<IActionResult> GetBatch(int id)
    {
        var batch = await _context.ProductionBatches
            .Include(pb => pb.Formula)
                .ThenInclude(f => f!.Client)
            .Include(pb => pb.Lines)
                .ThenInclude(l => l.RawMaterial)
            .FirstOrDefaultAsync(pb => pb.Id == id && pb.IsActive);

        if (batch == null)
            return NotFound(ApiResponse<object>.Fail("Production batch not found."));

        var dto = new ProductionBatchDetailDto
        {
            Id = batch.Id,
            BatchCode = batch.BatchCode,
            FormulaId = batch.FormulaId,
            FormulaCode = batch.Formula?.FormulaCode,
            FormulaName = batch.Formula?.ProductName,
            ClientName = batch.Formula?.Client?.Name,
            BatchSize = batch.BatchSize,
            Unit = batch.Unit,
            Status = batch.Status,
            ProductionDate = batch.ProductionDate,
            CompletedAt = batch.CompletedAt,
            Operator = batch.Operator,
            ManufacturingSite = batch.ManufacturingSite,
            LotNumber = batch.LotNumber,
            Notes = batch.Notes,
            Lines = batch.Lines.OrderBy(l => l.Phase).ThenBy(l => l.OrderIndex).Select(l => new ProductionBatchLineDto
            {
                Id = l.Id,
                RawMaterialId = l.RawMaterialId,
                RawMaterialName = l.RawMaterial?.ProductName ?? "AQUA (Water)",
                RawMaterialCode = l.RawMaterial?.Code,
                Percentage = l.Percentage,
                CalculatedQuantity = l.CalculatedQuantity,
                ActualQuantity = l.ActualQuantity,
                Phase = l.Phase,
                OrderIndex = l.OrderIndex,
                Notes = l.Notes
            }).ToList()
        };

        return Ok(ApiResponse<ProductionBatchDetailDto>.Ok(dto));
    }

    /// <summary>
    /// Create a new production batch from a formula
    /// </summary>
    [HttpPost("batches")]
    public async Task<IActionResult> CreateBatch([FromBody] CreateProductionBatchRequest request)
    {
        if (request.BatchSize <= 0)
            return BadRequest(ApiResponse<object>.Fail("Batch size must be greater than 0."));

        var formula = await _context.Formulas
            .Include(f => f.FormulaRawMaterials)
                .ThenInclude(frm => frm.RawMaterial)
            .FirstOrDefaultAsync(f => f.Id == request.FormulaId);

        if (formula == null)
            return NotFound(ApiResponse<object>.Fail($"Formula with ID {request.FormulaId} not found."));

        // Generate batch code
        var batchCount = await _context.ProductionBatches.CountAsync(pb => pb.FormulaId == request.FormulaId) + 1;
        var batchCode = $"{formula.FormulaCode}-PROD-{batchCount:D4}";

        var batch = new ProductionBatch
        {
            BatchCode = batchCode,
            FormulaId = formula.Id,
            BatchSize = request.BatchSize,
            Unit = request.Unit ?? "kg",
            ProductionDate = DateTime.Now,
            Status = "Planned",
            Notes = request.Notes,
            LotNumber = request.LotNumber,
            Operator = request.Operator,
            ManufacturingSite = request.ManufacturingSite ?? formula.ManufacturingSite
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
            var calculatedQuantity = Math.Round((percentage / 100) * request.BatchSize, 4);

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
            batch.Lines.Add(new ProductionBatchLine
            {
                RawMaterialId = null,
                Percentage = waterPercentage,
                CalculatedQuantity = Math.Round((waterPercentage / 100) * request.BatchSize, 4),
                Phase = "A",
                OrderIndex = 0,
                Notes = "AQUA (Water)"
            });
        }

        _context.ProductionBatches.Add(batch);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Production batch {BatchCode} created for formula {FormulaCode} ({BatchSize} {Unit})",
            batchCode, formula.FormulaCode, request.BatchSize, request.Unit);

        return CreatedAtAction(nameof(GetBatch), new { id = batch.Id },
            ApiResponse<object>.Ok(new { batch.Id, batch.BatchCode }, "Production batch created successfully."));
    }

    /// <summary>
    /// Update production batch status
    /// </summary>
    [HttpPut("batches/{id}/status")]
    public async Task<IActionResult> UpdateBatchStatus(int id, [FromBody] UpdateBatchStatusRequest request)
    {
        var batch = await _context.ProductionBatches.FindAsync(id);
        if (batch == null || !batch.IsActive)
            return NotFound(ApiResponse<object>.Fail("Production batch not found."));

        var validStatuses = new[] { "Planned", "InProgress", "Completed", "Cancelled" };
        if (!validStatuses.Contains(request.Status))
            return BadRequest(ApiResponse<object>.Fail($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}"));

        batch.Status = request.Status;
        if (request.Status == "Completed")
            batch.CompletedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Production batch {BatchCode} status changed to {Status}", batch.BatchCode, request.Status);

        return Ok(ApiResponse<object>.Ok(new { batch.Id, batch.BatchCode, batch.Status }, "Status updated."));
    }

    /// <summary>
    /// Update actual quantities weighed for a batch line
    /// </summary>
    [HttpPut("batches/{batchId}/lines/{lineId}/actual")]
    public async Task<IActionResult> UpdateActualQuantity(int batchId, int lineId, [FromBody] UpdateActualQuantityRequest request)
    {
        var line = await _context.ProductionBatchLines
            .FirstOrDefaultAsync(l => l.Id == lineId && l.ProductionBatchId == batchId);

        if (line == null)
            return NotFound(ApiResponse<object>.Fail("Batch line not found."));

        line.ActualQuantity = request.ActualQuantity;
        if (!string.IsNullOrEmpty(request.Notes))
            line.Notes = request.Notes;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            line.Id,
            line.CalculatedQuantity,
            line.ActualQuantity,
            Deviation = line.ActualQuantity.HasValue ? line.ActualQuantity.Value - line.CalculatedQuantity : 0
        }, "Actual quantity updated."));
    }

    /// <summary>
    /// Delete (soft) a production batch
    /// </summary>
    [HttpDelete("batches/{id}")]
    public async Task<IActionResult> DeleteBatch(int id)
    {
        var batch = await _context.ProductionBatches.FindAsync(id);
        if (batch == null)
            return NotFound(ApiResponse<object>.Fail("Production batch not found."));

        batch.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Production batch {BatchCode} deleted (soft)", batch.BatchCode);

        return Ok(ApiResponse<object>.Ok(new { deleted = true }, "Production batch deleted."));
    }

    /// <summary>
    /// Get production statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var stats = new
        {
            TotalBatches = await _context.ProductionBatches.CountAsync(pb => pb.IsActive),
            PlannedBatches = await _context.ProductionBatches.CountAsync(pb => pb.IsActive && pb.Status == "Planned"),
            InProgressBatches = await _context.ProductionBatches.CountAsync(pb => pb.IsActive && pb.Status == "InProgress"),
            CompletedBatches = await _context.ProductionBatches.CountAsync(pb => pb.IsActive && pb.Status == "Completed"),
            CancelledBatches = await _context.ProductionBatches.CountAsync(pb => pb.IsActive && pb.Status == "Cancelled"),
            TotalFormulasProduced = await _context.ProductionBatches
                .Where(pb => pb.IsActive && pb.Status == "Completed")
                .Select(pb => pb.FormulaId)
                .Distinct()
                .CountAsync(),
            Last30Days = await _context.ProductionBatches
                .CountAsync(pb => pb.IsActive && pb.ProductionDate >= DateTime.Now.AddDays(-30))
        };

        return Ok(ApiResponse<object>.Ok(stats));
    }

    /// <summary>
    /// Get production history for a specific formula
    /// </summary>
    [HttpGet("formulas/{formulaId}/batches")]
    public async Task<IActionResult> GetFormulaBatches(int formulaId)
    {
        var batches = await _context.ProductionBatches
            .Where(pb => pb.FormulaId == formulaId && pb.IsActive)
            .OrderByDescending(pb => pb.ProductionDate)
            .Select(pb => new ProductionBatchDto
            {
                Id = pb.Id,
                BatchCode = pb.BatchCode,
                FormulaId = pb.FormulaId,
                BatchSize = pb.BatchSize,
                Unit = pb.Unit,
                Status = pb.Status,
                ProductionDate = pb.ProductionDate,
                CompletedAt = pb.CompletedAt,
                Operator = pb.Operator,
                LotNumber = pb.LotNumber
            })
            .ToListAsync();

        return Ok(ApiResponse<List<ProductionBatchDto>>.Ok(batches));
    }
}

// ============================================================
// DTOs
// ============================================================

/// <summary>
/// Production batch list DTO
/// </summary>
public class ProductionBatchDto
{
    public int Id { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public int FormulaId { get; set; }
    public string? FormulaCode { get; set; }
    public string? FormulaName { get; set; }
    public decimal BatchSize { get; set; }
    public string Unit { get; set; } = "kg";
    public string Status { get; set; } = "Planned";
    public DateTime ProductionDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Operator { get; set; }
    public string? ManufacturingSite { get; set; }
    public string? LotNumber { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Production batch detail DTO with lines
/// </summary>
public class ProductionBatchDetailDto : ProductionBatchDto
{
    public string? ClientName { get; set; }
    public List<ProductionBatchLineDto> Lines { get; set; } = new();
}

/// <summary>
/// Production batch line DTO
/// </summary>
public class ProductionBatchLineDto
{
    public int Id { get; set; }
    public int? RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public string? RawMaterialCode { get; set; }
    public decimal Percentage { get; set; }
    public decimal CalculatedQuantity { get; set; }
    public decimal? ActualQuantity { get; set; }
    public string? Phase { get; set; }
    public int OrderIndex { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request to create a production batch
/// </summary>
public class CreateProductionBatchRequest
{
    public int FormulaId { get; set; }
    public decimal BatchSize { get; set; }
    public string? Unit { get; set; }
    public string? LotNumber { get; set; }
    public string? Operator { get; set; }
    public string? ManufacturingSite { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request to update batch status
/// </summary>
public class UpdateBatchStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Request to update actual quantity for a batch line
/// </summary>
public class UpdateActualQuantityRequest
{
    public decimal ActualQuantity { get; set; }
    public string? Notes { get; set; }
}
