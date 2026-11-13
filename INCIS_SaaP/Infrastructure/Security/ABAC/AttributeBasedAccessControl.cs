// Author: Damianos Pythagoras Ougkrenidis
// Attribute-Based Access Control (ABAC) - Fine-grained authorization

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace INCIS_SaaP.Infrastructure.Security.ABAC;

/// <summary>
/// ABAC Policy Definition
/// Specifies rules based on subject, resource, action, and environment attributes
/// </summary>
public class AbacPolicy
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Resource type this policy applies to (e.g., "Formula", "RawMaterial", "Client")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ResourceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Action this policy controls (e.g., "Read", "Write", "Delete", "Export", "Validate")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON condition rules for evaluation
    /// </summary>
    public string ConditionRulesJson { get; set; } = "{}";
    
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// Effect: Permit or Deny
    /// </summary>
    [MaxLength(10)]
    public string Effect { get; set; } = "Permit";
}

/// <summary>
/// User security attribute (attached to user profiles)
/// </summary>
public class UserSecurityAttribute
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string AttributeName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string AttributeValue { get; set; } = string.Empty;
    
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidUntil { get; set; }
}

/// <summary>
/// Access decision request
/// </summary>
public class AccessDecisionRequest
{
    public ClaimsPrincipal Subject { get; set; } = null!;
    public string ResourceType { get; set; } = string.Empty;
    public int? ResourceId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Environment { get; set; } = new();
    public Dictionary<string, object> ResourceAttributes { get; set; } = new();
}

/// <summary>
/// Access decision result
/// </summary>
public class AccessDecision
{
    public bool IsPermitted { get; init; }
    public string? Reason { get; init; }
    public string? PolicyName { get; init; }
    public List<string> ApplicablePolicies { get; init; } = new();
    
    public static AccessDecision Permit(string? policyName = null) => new() { IsPermitted = true, PolicyName = policyName };
    public static AccessDecision Deny(string reason, string? policyName = null) => new() { IsPermitted = false, Reason = reason, PolicyName = policyName };
}

/// <summary>
/// ABAC Engine - evaluates access decisions based on policies
/// </summary>
public interface IAbacEngine
{
    Task<AccessDecision> EvaluateAsync(AccessDecisionRequest request, CancellationToken cancellationToken = default);
    Task<bool> CanAccessFieldAsync(ClaimsPrincipal user, string resourceType, string fieldName, CancellationToken cancellationToken = default);
}

/// <summary>
/// ABAC Engine implementation
/// </summary>
public class AbacEngine : IAbacEngine
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AbacEngine> _logger;

    public AbacEngine(IServiceProvider serviceProvider, ILogger<AbacEngine> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<AccessDecision> EvaluateAsync(AccessDecisionRequest request, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        // Get applicable policies
        var policies = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<AbacPolicy>()
                    .Where(p => p.IsActive && p.ResourceType == request.ResourceType && p.Action == request.Action)
                    .OrderByDescending(p => p.Priority),
                cancellationToken);

        if (!policies.Any())
        {
            // Default: permit if no policies defined (fallback to role-based)
            return AccessDecision.Permit("No ABAC policies defined - using default");
        }

        // Get user attributes
        var userId = request.Subject.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var userAttributes = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<UserSecurityAttribute>()
                    .Where(a => a.UserId == userId && (a.ValidUntil == null || a.ValidUntil > DateTime.UtcNow)),
                cancellationToken);

        var userAttrDict = userAttributes.ToDictionary(a => a.AttributeName, a => a.AttributeValue);

        // Evaluate policies in priority order
        foreach (var policy in policies)
        {
            var result = EvaluatePolicy(policy, request, userAttrDict);
            if (result != null)
            {
                _logger.LogDebug("ABAC decision for {User} on {Resource}/{Action}: {Decision} (policy: {Policy})",
                    userId, request.ResourceType, request.Action, result.IsPermitted ? "PERMIT" : "DENY", policy.Name);
                return result;
            }
        }

        return AccessDecision.Deny("No matching policy found");
    }

    public async Task<bool> CanAccessFieldAsync(ClaimsPrincipal user, string resourceType, string fieldName, CancellationToken cancellationToken = default)
    {
        var request = new AccessDecisionRequest
        {
            Subject = user,
            ResourceType = resourceType,
            Action = $"ReadField:{fieldName}",
            ResourceAttributes = { ["fieldName"] = fieldName }
        };

        var decision = await EvaluateAsync(request, cancellationToken);
        return decision.IsPermitted;
    }

    private AccessDecision? EvaluatePolicy(AbacPolicy policy, AccessDecisionRequest request, Dictionary<string, string> userAttributes)
    {
        try
        {
            var conditions = System.Text.Json.JsonSerializer.Deserialize<PolicyConditions>(policy.ConditionRulesJson);
            if (conditions == null) return null;

            // Check department condition
            if (conditions.RequiredDepartment != null)
            {
                if (!userAttributes.TryGetValue("department", out var dept) || dept != conditions.RequiredDepartment)
                    return null; // Policy doesn't apply
            }

            // Check clearance level
            if (conditions.MinClearanceLevel > 0)
            {
                if (!userAttributes.TryGetValue("clearanceLevel", out var levelStr) || 
                    !int.TryParse(levelStr, out var level) || level < conditions.MinClearanceLevel)
                {
                    if (policy.Effect == "Deny")
                        return AccessDecision.Deny($"Insufficient clearance level (required: {conditions.MinClearanceLevel})", policy.Name);
                    return null;
                }
            }

            // Check role condition
            if (conditions.RequiredRoles?.Any() == true)
            {
                var hasRole = conditions.RequiredRoles.Any(r => request.Subject.IsInRole(r));
                if (!hasRole) return null;
            }

            // Check tenant condition
            if (conditions.RequiredTenant != null)
            {
                var userTenant = userAttributes.GetValueOrDefault("tenant") ?? 
                    request.Subject.FindFirst("TenantId")?.Value;
                if (userTenant != conditions.RequiredTenant) return null;
            }

            // All conditions met - apply effect
            return policy.Effect == "Permit" 
                ? AccessDecision.Permit(policy.Name) 
                : AccessDecision.Deny(policy.Description ?? "Access denied by policy", policy.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating ABAC policy: {PolicyName}", policy.Name);
            return null;
        }
    }
}

/// <summary>
/// Policy conditions (serialized as JSON in AbacPolicy.ConditionRulesJson)
/// </summary>
public class PolicyConditions
{
    public string? RequiredDepartment { get; set; }
    public int MinClearanceLevel { get; set; }
    public List<string>? RequiredRoles { get; set; }
    public string? RequiredTenant { get; set; }
    public List<string>? AllowedFields { get; set; }
    public List<string>? DeniedFields { get; set; }
    public bool? RequireBusinessHours { get; set; }
    public string? RequiredIpRange { get; set; }
}

/// <summary>
/// Field-level permission attribute for model properties
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class FieldPermissionAttribute : Attribute
{
    public string[] AllowedRoles { get; }
    public int MinClearanceLevel { get; }

    public FieldPermissionAttribute(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
        MinClearanceLevel = 0;
    }

    public FieldPermissionAttribute(int minClearanceLevel, params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
        MinClearanceLevel = minClearanceLevel;
    }
}

/// <summary>
/// Break-glass access record (emergency access with full audit)
/// </summary>
public class BreakGlassAccessRecord
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ResourceType { get; set; } = string.Empty;
    
    public int? ResourceId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;
    
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    
    [MaxLength(100)]
    public string? ReviewedBy { get; set; }
    
    [MaxLength(500)]
    public string? ReviewNote { get; set; }
    
    public bool IsApproved { get; set; }
}
