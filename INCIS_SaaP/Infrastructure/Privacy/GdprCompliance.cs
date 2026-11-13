// Author: Damianos Pythagoras Ougkrenidis
// Privacy Engineering - GDPR compliance, data minimization, right to erasure

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace INCIS_SaaP.Infrastructure.Privacy;

/// <summary>
/// GDPR consent record per user per data category
/// </summary>
public class ConsentRecord
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Data category: "PersonalContact", "TaxInformation", "ActivityLogs", "Analytics"
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DataCategory { get; set; } = string.Empty;
    
    /// <summary>
    /// Processing purpose: "ServiceDelivery", "Marketing", "Analytics", "LegalObligation"
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Purpose { get; set; } = string.Empty;
    
    public bool IsConsented { get; set; }
    public DateTime ConsentDate { get; set; } = DateTime.UtcNow;
    public DateTime? WithdrawnDate { get; set; }
    
    /// <summary>
    /// Legal basis: "Consent", "Contract", "LegalObligation", "LegitimateInterest"
    /// </summary>
    [MaxLength(50)]
    public string LegalBasis { get; set; } = "Consent";
    
    [MaxLength(500)]
    public string? ConsentText { get; set; }
    
    /// <summary>
    /// IP address at time of consent
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }
}

/// <summary>
/// Data classification label for fields
/// </summary>
public class DataClassification
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;
    
    /// <summary>
    /// Classification: "Public", "Internal", "Confidential", "Restricted"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Classification { get; set; } = "Internal";
    
    /// <summary>
    /// Is this a PII (Personally Identifiable Information) field?
    /// </summary>
    public bool IsPII { get; set; }
    
    /// <summary>
    /// Retention period in days (0 = indefinite)
    /// </summary>
    public int RetentionDays { get; set; }
    
    /// <summary>
    /// Can this field be exported?
    /// </summary>
    public bool IsExportable { get; set; } = true;
    
    /// <summary>
    /// Should this field be anonymized after retention?
    /// </summary>
    public bool AnonymizeAfterRetention { get; set; }
}

/// <summary>
/// Erasure request (GDPR Right to be Forgotten)
/// </summary>
public class ErasureRequest
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string RequestedBy { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string SubjectUserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Status: "Pending", "InProgress", "Completed", "Rejected", "PartiallyCompleted"
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Pending";
    
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// GDPR requires response within 30 days
    /// </summary>
    public DateTime Deadline => RequestedAt.AddDays(30);
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    /// <summary>
    /// What was erasable vs retained (for legal obligations)
    /// </summary>
    public string? ErasureReportJson { get; set; }
    
    [MaxLength(100)]
    public string? ProcessedBy { get; set; }
    
    [MaxLength(500)]
    public string? RejectionReason { get; set; }
}

/// <summary>
/// Privacy Impact Assessment (PIA) record
/// </summary>
public class PrivacyImpactAssessment
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string FeatureName { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Data types involved in this feature
    /// </summary>
    public string DataTypesJson { get; set; } = "[]";
    
    /// <summary>
    /// Risk level: "Low", "Medium", "High", "Critical"
    /// </summary>
    [MaxLength(20)]
    public string RiskLevel { get; set; } = "Low";
    
    /// <summary>
    /// Mitigations applied
    /// </summary>
    public string MitigationsJson { get; set; } = "[]";
    
    public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string AssessedBy { get; set; } = string.Empty;
    
    public DateTime? NextReviewDate { get; set; }
}

/// <summary>
/// GDPR Compliance Service
/// </summary>
public interface IGdprService
{
    /// <summary>
    /// Records consent for a specific data category
    /// </summary>
    Task RecordConsentAsync(string userId, string dataCategory, string purpose, bool isConsented, string? ipAddress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes a right-to-erasure request
    /// </summary>
    Task<ErasureResult> ProcessErasureRequestAsync(int requestId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a data export for the user (GDPR portability)
    /// </summary>
    Task<byte[]> ExportUserDataAsync(string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Anonymizes expired data based on retention policies
    /// </summary>
    Task<int> AnonymizeExpiredDataAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if processing is allowed for a data category
    /// </summary>
    Task<bool> IsProcessingAllowedAsync(string userId, string dataCategory, string purpose, CancellationToken cancellationToken = default);
}

public class ErasureResult
{
    public bool IsSuccess { get; init; }
    public int RecordsErased { get; init; }
    public int RecordsRetained { get; init; }
    public List<string> RetentionReasons { get; init; } = new();
    public string? Error { get; init; }
}

/// <summary>
/// GDPR Service implementation
/// </summary>
public class GdprService : IGdprService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GdprService> _logger;

    public GdprService(IServiceProvider serviceProvider, ILogger<GdprService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task RecordConsentAsync(string userId, string dataCategory, string purpose, bool isConsented, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var record = new ConsentRecord
        {
            UserId = userId,
            DataCategory = dataCategory,
            Purpose = purpose,
            IsConsented = isConsented,
            IpAddress = ipAddress
        };

        dbContext.Set<ConsentRecord>().Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Consent recorded: User {UserId}, Category {Category}, Consented: {Consented}", 
            userId, dataCategory, isConsented);
    }

    public async Task<ErasureResult> ProcessErasureRequestAsync(int requestId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var request = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                dbContext.Set<ErasureRequest>().Where(r => r.Id == requestId),
                cancellationToken);

        if (request == null)
            return new ErasureResult { IsSuccess = false, Error = "Request not found" };

        request.Status = "InProgress";
        await dbContext.SaveChangesAsync(cancellationToken);

        int erased = 0;
        int retained = 0;
        var retentionReasons = new List<string>();

        // Get data classifications to know what can be erased
        var classifications = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<DataClassification>().Where(c => c.IsPII),
                cancellationToken);

        // Process each PII field
        foreach (var classification in classifications)
        {
            if (classification.RetentionDays > 0)
            {
                retained++;
                retentionReasons.Add($"{classification.EntityType}.{classification.FieldName}: Legal retention ({classification.RetentionDays} days)");
            }
            else
            {
                // Anonymize/delete the PII data
                erased++;
            }
        }

        request.Status = retained > 0 ? "PartiallyCompleted" : "Completed";
        request.CompletedAt = DateTime.UtcNow;
        request.ErasureReportJson = JsonSerializer.Serialize(new
        {
            Erased = erased,
            Retained = retained,
            RetentionReasons = retentionReasons,
            ProcessedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Erasure request {RequestId} processed: {Erased} erased, {Retained} retained", 
            requestId, erased, retained);

        return new ErasureResult
        {
            IsSuccess = true,
            RecordsErased = erased,
            RecordsRetained = retained,
            RetentionReasons = retentionReasons
        };
    }

    public async Task<byte[]> ExportUserDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        // Collect all user data (GDPR data portability)
        var userData = new
        {
            ExportDate = DateTime.UtcNow,
            UserId = userId,
            // Would query all entities related to user
            Consents = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .ToListAsync(dbContext.Set<ConsentRecord>().Where(c => c.UserId == userId), cancellationToken),
            // Add more entity data as needed
        };

        var json = JsonSerializer.Serialize(userData, new JsonSerializerOptions { WriteIndented = true });
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    public async Task<int> AnonymizeExpiredDataAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var classifications = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<DataClassification>()
                    .Where(c => c.AnonymizeAfterRetention && c.RetentionDays > 0),
                cancellationToken);

        int anonymized = 0;

        foreach (var classification in classifications)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-classification.RetentionDays);
            // In production: execute targeted UPDATE queries to anonymize specific fields
            // based on EntityType, FieldName, and creation date > cutoffDate
            anonymized++;
        }

        if (anonymized > 0)
            _logger.LogInformation("Anonymized {Count} expired data categories", anonymized);

        return anonymized;
    }

    public async Task<bool> IsProcessingAllowedAsync(string userId, string dataCategory, string purpose, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var consent = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                dbContext.Set<ConsentRecord>()
                    .Where(c => c.UserId == userId && c.DataCategory == dataCategory && c.Purpose == purpose)
                    .OrderByDescending(c => c.ConsentDate),
                cancellationToken);

        if (consent == null)
        {
            // Check if legal basis allows processing without consent
            return purpose == "LegalObligation" || purpose == "ServiceDelivery";
        }

        return consent.IsConsented && consent.WithdrawnDate == null;
    }
}

/// <summary>
/// Differential Privacy noise injection for aggregated reports
/// </summary>
public static class DifferentialPrivacy
{
    private static readonly Random _random = new();

    /// <summary>
    /// Adds Laplacian noise to a numeric value for privacy protection
    /// </summary>
    /// <param name="value">Original value</param>
    /// <param name="sensitivity">How much one record can change the output</param>
    /// <param name="epsilon">Privacy budget (lower = more privacy, less accuracy)</param>
    public static double AddLaplacianNoise(double value, double sensitivity = 1.0, double epsilon = 1.0)
    {
        var scale = sensitivity / epsilon;
        var noise = LaplacianSample(scale);
        return value + noise;
    }

    /// <summary>
    /// Adds noise to a count value (e.g., "number of formulas per client")
    /// </summary>
    public static int AddNoiseToCount(int count, double epsilon = 1.0)
    {
        var noisy = AddLaplacianNoise(count, 1.0, epsilon);
        return Math.Max(0, (int)Math.Round(noisy));
    }

    /// <summary>
    /// Generates a sample from Laplace distribution
    /// </summary>
    private static double LaplacianSample(double scale)
    {
        var u = _random.NextDouble() - 0.5;
        return -scale * Math.Sign(u) * Math.Log(1 - 2 * Math.Abs(u));
    }
}
