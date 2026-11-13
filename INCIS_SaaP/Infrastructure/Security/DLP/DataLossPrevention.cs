// Author: Damianos Pythagoras Ougkrenidis
// Data Loss Prevention (DLP) - Prevent unauthorized export of trade secrets

using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace INCIS_SaaP.Infrastructure.Security.DLP;

/// <summary>
/// DLP Policy - defines rules for data export protection
/// </summary>
public class DlpPolicy
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Resource type: "Formula", "RawMaterial", "Report"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ResourceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Action type: "Export", "Print", "Copy", "Email"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON rules configuration
    /// </summary>
    public string RulesJson { get; set; } = "{}";
    
    public bool RequireApproval { get; set; }
    public bool AddWatermark { get; set; } = true;
    public bool LogAccess { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DLP Export audit record
/// </summary>
public class DlpExportRecord
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ResourceType { get; set; } = string.Empty;
    
    public int? ResourceId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? ExportFormat { get; set; } // "PDF", "Excel", "Print"
    
    /// <summary>
    /// Unique watermark ID embedded in the exported document
    /// </summary>
    [MaxLength(64)]
    public string? WatermarkId { get; set; }
    
    /// <summary>
    /// SHA-256 hash of the exported content (for forensic analysis)
    /// </summary>
    [MaxLength(64)]
    public string? ContentHash { get; set; }
    
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    public bool WasApproved { get; set; }
    
    [MaxLength(100)]
    public string? ApprovedBy { get; set; }
}

/// <summary>
/// DLP Service - enforces data loss prevention policies
/// </summary>
public interface IDlpService
{
    /// <summary>
    /// Checks if an export action is allowed
    /// </summary>
    Task<DlpDecision> CanExportAsync(string userId, string resourceType, int? resourceId, string format, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a unique watermark for exported documents
    /// </summary>
    Task<WatermarkInfo> GenerateWatermarkAsync(string userId, string resourceType, int? resourceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Records an export action for audit
    /// </summary>
    Task RecordExportAsync(DlpExportRecord record, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Computes content hash for forensic tracking
    /// </summary>
    string ComputeContentHash(byte[] content);
}

public class DlpDecision
{
    public bool IsAllowed { get; init; }
    public bool RequiresApproval { get; init; }
    public bool RequiresWatermark { get; init; }
    public string? DenialReason { get; init; }
    public string? PolicyName { get; init; }
    
    public static DlpDecision Allow(bool watermark = true) => new() { IsAllowed = true, RequiresWatermark = watermark };
    public static DlpDecision RequireApproval(string policy) => new() { IsAllowed = false, RequiresApproval = true, PolicyName = policy };
    public static DlpDecision Deny(string reason) => new() { IsAllowed = false, DenialReason = reason };
}

public class WatermarkInfo
{
    public string WatermarkId { get; init; } = string.Empty;
    public string WatermarkText { get; init; } = string.Empty;
    public string ExportedBy { get; init; } = string.Empty;
    public DateTime ExportedAt { get; init; }
    public string TraceCode { get; init; } = string.Empty;
}

/// <summary>
/// DLP Service implementation
/// </summary>
public class DlpService : IDlpService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DlpService> _logger;

    public DlpService(IServiceProvider serviceProvider, ILogger<DlpService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<DlpDecision> CanExportAsync(string userId, string resourceType, int? resourceId, string format, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var policies = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<DlpPolicy>()
                    .Where(p => p.IsActive && p.ResourceType == resourceType && p.ActionType == "Export"),
                cancellationToken);

        if (!policies.Any())
            return DlpDecision.Allow(watermark: true);

        foreach (var policy in policies)
        {
            if (policy.RequireApproval)
            {
                _logger.LogWarning("DLP: Export requires approval for {ResourceType} (user: {UserId})", resourceType, userId);
                return DlpDecision.RequireApproval(policy.Name);
            }
        }

        return DlpDecision.Allow(watermark: policies.Any(p => p.AddWatermark));
    }

    public async Task<WatermarkInfo> GenerateWatermarkAsync(string userId, string resourceType, int? resourceId, CancellationToken cancellationToken = default)
    {
        var watermarkId = Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();
        var traceCode = $"INCIS-{DateTime.UtcNow:yyyyMMdd}-{watermarkId}";

        var watermark = new WatermarkInfo
        {
            WatermarkId = watermarkId,
            WatermarkText = $"CONFIDENTIAL - {userId} - {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC - Trace: {traceCode}",
            ExportedBy = userId,
            ExportedAt = DateTime.UtcNow,
            TraceCode = traceCode
        };

        _logger.LogInformation("DLP: Generated watermark {WatermarkId} for {ResourceType}:{ResourceId} (user: {UserId})",
            watermarkId, resourceType, resourceId, userId);

        return watermark;
    }

    public async Task RecordExportAsync(DlpExportRecord record, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();
        
        dbContext.Set<DlpExportRecord>().Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("DLP: Export recorded - {ResourceType}:{ResourceId} by {UserId} ({Format})",
            record.ResourceType, record.ResourceId, record.UserId, record.ExportFormat);
    }

    public string ComputeContentHash(byte[] content)
    {
        var hash = SHA256.HashData(content);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
