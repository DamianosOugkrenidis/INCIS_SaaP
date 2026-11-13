// Author: Damianos Pythagoras Ougkrenidis
// Document Storage API Controller

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Infrastructure.ApiEnvelope;
using INCIS_SaaP.Infrastructure.Tenancy;
using System.Security.Cryptography;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// API Controller for Document management (upload/download TDS, MSDS, certificates, etc.)
/// </summary>
[ApiController]
[Route("api/v1/documents")]
[Authorize]
[EnableRateLimiting("Strict")]
public class ApiDocumentsController : ControllerBase
{
    private readonly INCISDbContext _context;
    private readonly ILogger<ApiDocumentsController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly ITenantService _tenantService;
    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg" };
    private const long MaxFileSize = 20 * 1024 * 1024; // 20MB

    public ApiDocumentsController(INCISDbContext context, ILogger<ApiDocumentsController> logger, IWebHostEnvironment env, ITenantService tenantService)
    {
        _context = context;
        _logger = logger;
        _env = env;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Upload a document for a raw material, formula, or other entity
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    // Εξαίρεση από το Swagger doc: ο συνδυασμός [FromForm]+IFormFile με πολλαπλά
    // form πεδία δεν υποστηρίζεται από τον SwaggerGen (το endpoint λειτουργεί κανονικά).
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string documentType, [FromForm] string? entityType, [FromForm] int? entityId, [FromForm] string? description)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file provided."));

        if (file.Length > MaxFileSize)
            return BadRequest(ApiResponse<object>.Fail("File size exceeds 20MB limit."));

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest(ApiResponse<object>.Fail($"File type '{extension}' not allowed. Allowed: {string.Join(", ", AllowedExtensions)}"));

        // Tenant context + storage quota (checked BEFORE writing the file)
        var tenantId = _tenantService.GetCurrentTenantId();
        if (tenantId == null)
            return BadRequest(ApiResponse<object>.Fail("Tenant context is required to upload documents."));

        var tenant = _tenantService.GetCurrentTenant();
        var maxMb = tenant?.MaxStorageMB ?? 1024;
        var usedBytes = await _context.Documents
            .Where(d => d.TenantId == tenantId.Value)
            .SumAsync(d => (long?)d.FileSize) ?? 0;
        var maxBytes = maxMb * 1024L * 1024L;
        if (usedBytes + file.Length > maxBytes)
            return BadRequest(ApiResponse<object>.Fail($"Το όριο αποθήκευσης ({maxMb} MB) έχει εξαντληθεί."));

        // Create storage directory
        var storageRoot = Path.Combine(_env.ContentRootPath, "Storage", "Documents");
        Directory.CreateDirectory(storageRoot);

        // Generate unique filename
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(DateTime.UtcNow.ToString("yyyy/MM"), uniqueFileName);
        var fullPath = Path.Combine(storageRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // Save file
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Calculate hash
        string fileHash;
        using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream);
            fileHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        var document = new Document
        {
            TenantId = tenantId,
            FileName = file.FileName,
            StoragePath = relativePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            DocumentType = documentType,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            FileHash = fileHash,
            UploadedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document uploaded: {FileName} ({Size} bytes) for {EntityType}:{EntityId}",
            file.FileName, file.Length, entityType, entityId);

        return Ok(ApiResponse<object>.Ok(new
        {
            document.Id,
            document.FileName,
            document.DocumentType,
            document.FileSize,
            document.ContentType,
            document.UploadedAt
        }, "Document uploaded successfully."));
    }

    /// <summary>
    /// Download a document by ID
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id && d.IsActive);
        if (document == null)
            return NotFound(ApiResponse<object>.Fail("Document not found."));

        var storageRoot = Path.Combine(_env.ContentRootPath, "Storage", "Documents");
        var fullPath = Path.Combine(storageRoot, document.StoragePath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound(ApiResponse<object>.Fail("File not found on disk."));

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return File(stream, document.ContentType, document.FileName);
    }

    /// <summary>
    /// Get all documents for a specific entity
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDocuments([FromQuery] string? entityType, [FromQuery] int? entityId, [FromQuery] string? documentType)
    {
        var query = _context.Documents.Where(d => d.IsActive);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(d => d.EntityType == entityType);
        if (entityId.HasValue)
            query = query.Where(d => d.EntityId == entityId);
        if (!string.IsNullOrEmpty(documentType))
            query = query.Where(d => d.DocumentType == documentType);

        var documents = await query
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new
            {
                d.Id,
                d.FileName,
                d.DocumentType,
                d.FileSize,
                d.ContentType,
                d.EntityType,
                d.EntityId,
                d.Description,
                d.Version,
                d.UploadedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(documents));
    }

    /// <summary>
    /// Delete a document (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);
        if (document == null)
            return NotFound(ApiResponse<object>.Fail("Document not found."));

        document.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document soft-deleted: {Id} ({FileName})", id, document.FileName);
        return Ok(ApiResponse<object>.Ok(new { deleted = true }, "Document deleted."));
    }
}
