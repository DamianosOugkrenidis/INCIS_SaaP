// Author: Damianos Pythagoras Ougkrenidis
// Zero Trust Architecture - mTLS, service identity, continuous verification

using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace INCIS_SaaP.Infrastructure.Security.ZeroTrust;

/// <summary>
/// Service identity for Zero Trust - each service/module has a verified identity
/// </summary>
public class ServiceIdentity
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string ServiceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Certificate thumbprint for mTLS verification
    /// </summary>
    [MaxLength(64)]
    public string? CertificateThumbprint { get; set; }
    
    /// <summary>
    /// Allowed operations for this service
    /// </summary>
    public string AllowedOperationsJson { get; set; } = "[]";
    
    /// <summary>
    /// Allowed resource types this service can access
    /// </summary>
    public string AllowedResourcesJson { get; set; } = "[]";
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastVerifiedAt { get; set; }
}

/// <summary>
/// Zero Trust verification record
/// </summary>
public class TrustVerificationRecord
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string RequestorId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string VerificationType { get; set; } = string.Empty; // "mTLS", "JWT", "APIKey", "Session"
    
    public bool IsVerified { get; set; }
    
    [MaxLength(500)]
    public string? VerificationDetails { get; set; }
    
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    [MaxLength(200)]
    public string? UserAgent { get; set; }
    
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Risk score (0-100): 0 = no risk, 100 = maximum risk
    /// </summary>
    public int RiskScore { get; set; }
}

/// <summary>
/// Zero Trust policy engine
/// </summary>
public interface IZeroTrustEngine
{
    /// <summary>
    /// Continuously verifies a request (never trust, always verify)
    /// </summary>
    Task<TrustDecision> VerifyRequestAsync(TrustVerificationContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates service-to-service communication
    /// </summary>
    Task<bool> ValidateServiceIdentityAsync(string serviceId, string certificateThumbprint, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Computes risk score for a request
    /// </summary>
    int ComputeRiskScore(TrustVerificationContext context);
}

public class TrustVerificationContext
{
    public string? UserId { get; set; }
    public string? ServiceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CertificateThumbprint { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime RequestTime { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> AdditionalClaims { get; set; } = new();
    public bool IsInternalNetwork { get; set; }
}

public class TrustDecision
{
    public bool IsTrusted { get; init; }
    public int RiskScore { get; init; }
    public string? Reason { get; init; }
    public bool RequiresStepUp { get; init; } // Require additional authentication
    public string? StepUpMethod { get; init; } // "MFA", "Re-authentication", "Manager approval"
    
    public static TrustDecision Trusted(int riskScore = 0) => new() { IsTrusted = true, RiskScore = riskScore };
    public static TrustDecision Untrusted(string reason, int riskScore = 100) => new() { IsTrusted = false, Reason = reason, RiskScore = riskScore };
    public static TrustDecision StepUpRequired(string method, int riskScore) => new() { IsTrusted = false, RequiresStepUp = true, StepUpMethod = method, RiskScore = riskScore };
}

/// <summary>
/// Zero Trust engine implementation
/// </summary>
public class ZeroTrustEngine : IZeroTrustEngine
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ZeroTrustEngine> _logger;

    public ZeroTrustEngine(IServiceProvider serviceProvider, ILogger<ZeroTrustEngine> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<TrustDecision> VerifyRequestAsync(TrustVerificationContext context, CancellationToken cancellationToken = default)
    {
        var riskScore = ComputeRiskScore(context);

        // Record verification
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();
        
        var record = new TrustVerificationRecord
        {
            RequestorId = context.UserId ?? context.ServiceId ?? "unknown",
            VerificationType = context.CertificateThumbprint != null ? "mTLS" : "Session",
            IsVerified = riskScore < 70,
            IpAddress = context.IpAddress,
            UserAgent = context.UserAgent,
            RiskScore = riskScore
        };
        
        dbContext.Set<TrustVerificationRecord>().Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Decision based on risk score
        if (riskScore < 30)
        {
            return TrustDecision.Trusted(riskScore);
        }
        else if (riskScore < 50)
        {
            _logger.LogWarning("Elevated risk score {Score} for {Requestor}", riskScore, context.UserId);
            return TrustDecision.Trusted(riskScore); // Allow but monitor closely
        }
        else if (riskScore < 70)
        {
            return TrustDecision.StepUpRequired("MFA", riskScore);
        }
        else
        {
            _logger.LogWarning("High risk request denied: {Requestor} (score: {Score})", context.UserId, riskScore);
            return TrustDecision.Untrusted("Risk score too high", riskScore);
        }
    }

    public async Task<bool> ValidateServiceIdentityAsync(string serviceId, string certificateThumbprint, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var identity = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                dbContext.Set<ServiceIdentity>()
                    .Where(s => s.ServiceId == serviceId && s.IsActive),
                cancellationToken);

        if (identity == null)
        {
            _logger.LogWarning("Unknown service identity: {ServiceId}", serviceId);
            return false;
        }

        if (identity.CertificateThumbprint != certificateThumbprint)
        {
            _logger.LogWarning("Certificate mismatch for service: {ServiceId}", serviceId);
            return false;
        }

        identity.LastVerifiedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public int ComputeRiskScore(TrustVerificationContext context)
    {
        int score = 0;

        // Factor 1: No user/service identity
        if (string.IsNullOrEmpty(context.UserId) && string.IsNullOrEmpty(context.ServiceId))
            score += 50;

        // Factor 2: External network
        if (!context.IsInternalNetwork)
            score += 15;

        // Factor 3: No certificate
        if (string.IsNullOrEmpty(context.CertificateThumbprint))
            score += 10;

        // Factor 4: Unusual time (outside business hours)
        var hour = context.RequestTime.Hour;
        if (hour < 6 || hour > 22)
            score += 10;

        // Factor 5: High-risk actions
        if (context.Action is "Delete" or "Export" or "BulkUpdate")
            score += 15;

        // Factor 6: Sensitive resources
        if (context.ResourceType is "Formula" or "RawMaterial")
            score += 5;

        return Math.Min(score, 100);
    }
}
