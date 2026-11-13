// Author: Damianos Pythagoras Ougkrenidis
// Feature Flags API Controller

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Services;
using INCIS_SaaP.Infrastructure.ApiEnvelope;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// API Controller for Feature Flags management (DB-persisted)
/// </summary>
[ApiController]
[Route("api/v1/feature-flags")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("Strict")]
public class ApiFeatureFlagsController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly INCISDbContext _context;
    private readonly ILogger<ApiFeatureFlagsController> _logger;

    public ApiFeatureFlagsController(
        IFeatureFlagService featureFlagService,
        INCISDbContext context,
        ILogger<ApiFeatureFlagsController> logger)
    {
        _featureFlagService = featureFlagService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all feature flags (cached values)
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        var flags = _featureFlagService.GetAll();
        return Ok(ApiResponse<Dictionary<string, bool>>.Ok(flags));
    }

    /// <summary>
    /// Get all feature flags from database with full details
    /// </summary>
    [HttpGet("details")]
    public async Task<IActionResult> GetDetails()
    {
        var flags = await _context.FeatureFlags
            .OrderBy(f => f.Key)
            .Select(f => new FeatureFlagDto
            {
                Id = f.Id,
                Key = f.Key,
                IsEnabled = f.IsEnabled,
                Description = f.Description,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<FeatureFlagDto>>.Ok(flags));
    }

    /// <summary>
    /// Check if a specific feature flag is enabled
    /// </summary>
    [HttpGet("{key}")]
    [AllowAnonymous]
    public IActionResult IsEnabled(string key)
    {
        var isEnabled = _featureFlagService.IsEnabled(key);
        return Ok(ApiResponse<object>.Ok(new { key, isEnabled }));
    }

    /// <summary>
    /// Set a feature flag value (creates if not exists)
    /// </summary>
    [HttpPut("{key}")]
    public IActionResult SetFlag(string key, [FromBody] SetFeatureFlagRequest request)
    {
        _featureFlagService.SetEnabled(key, request.IsEnabled);

        _logger.LogInformation("Feature flag '{Key}' set to {Value} by {User}",
            key, request.IsEnabled, User.Identity?.Name);

        return Ok(ApiResponse<object>.Ok(new { key, isEnabled = request.IsEnabled },
            $"Feature flag '{key}' updated successfully."));
    }

    /// <summary>
    /// Create a new feature flag
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateFlag([FromBody] CreateFeatureFlagRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
            return BadRequest(ApiResponse<object>.Fail("Key is required."));

        var exists = await _context.FeatureFlags.AnyAsync(f => f.Key == request.Key);
        if (exists)
            return Conflict(ApiResponse<object>.Fail($"Feature flag '{request.Key}' already exists."));

        var flag = new FeatureFlag
        {
            Key = request.Key,
            IsEnabled = request.IsEnabled,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.FeatureFlags.Add(flag);
        await _context.SaveChangesAsync();

        // Update in-memory cache
        _featureFlagService.SetEnabled(request.Key, request.IsEnabled);

        _logger.LogInformation("Feature flag '{Key}' created (enabled: {IsEnabled})", request.Key, request.IsEnabled);

        return CreatedAtAction(nameof(IsEnabled), new { key = request.Key },
            ApiResponse<object>.Ok(new { flag.Id, flag.Key, flag.IsEnabled }, "Feature flag created."));
    }

    /// <summary>
    /// Delete a feature flag
    /// </summary>
    [HttpDelete("{key}")]
    public async Task<IActionResult> DeleteFlag(string key)
    {
        var flag = await _context.FeatureFlags.FirstOrDefaultAsync(f => f.Key == key);
        if (flag == null)
            return NotFound(ApiResponse<object>.Fail($"Feature flag '{key}' not found."));

        _context.FeatureFlags.Remove(flag);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Feature flag '{Key}' deleted", key);

        return Ok(ApiResponse<object>.Ok(new { deleted = true }, $"Feature flag '{key}' deleted."));
    }

    /// <summary>
    /// Bulk update feature flags
    /// </summary>
    [HttpPut("bulk")]
    public IActionResult BulkUpdate([FromBody] Dictionary<string, bool> flags)
    {
        foreach (var kvp in flags)
        {
            _featureFlagService.SetEnabled(kvp.Key, kvp.Value);
        }

        _logger.LogInformation("Bulk update of {Count} feature flags by {User}",
            flags.Count, User.Identity?.Name);

        return Ok(ApiResponse<object>.Ok(new { updated = flags.Count },
            $"{flags.Count} feature flags updated."));
    }
}

// ============================================================
// DTOs
// ============================================================

/// <summary>
/// Feature flag detail DTO
/// </summary>
public class FeatureFlagDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request to set feature flag value
/// </summary>
public class SetFeatureFlagRequest
{
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Request to create feature flag
/// </summary>
public class CreateFeatureFlagRequest
{
    public string Key { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
}
