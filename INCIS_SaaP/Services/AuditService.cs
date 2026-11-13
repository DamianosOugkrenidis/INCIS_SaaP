// Author: Damianos Pythagoras Ougkrenidis
//
using System.Text.Json;
using INCIS_SaaP.Models;
using INCIS_SaaP.Data;

namespace INCIS_SaaP.Services;

/// <summary>
/// Service για καταγραφή αλλαγών (Audit Logging)
/// </summary>
public class AuditService
{
    private readonly INCISDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        INCISDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Καταγράφει μια αλλαγή
    /// </summary>
    public async Task LogAsync(
        string entityName,
        string entityId,
        AuditAction action,
        string? userId = null,
        string? userEmail = null,
        object? oldValues = null,
        object? newValues = null,
        string? description = null)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            var ipAddress = context?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = context?.Request.Headers.UserAgent.ToString() ?? "Unknown";

            var auditLog = new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = action.ToString(),
                UserId = userId,
                UserEmail = userEmail,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                Description = description,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Audit: {Action} on {EntityName}:{EntityId} by {UserEmail}",
                action, entityName, entityId, userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log");
        }
    }

    /// <summary>
    /// Καταγράφει δημιουργία entity
    /// </summary>
    public async Task LogCreateAsync<T>(
        string entityId,
        object entity,
        string? userId = null,
        string? userEmail = null)
    {
        await LogAsync(
            typeof(T).Name,
            entityId,
            AuditAction.Created,
            userId,
            userEmail,
            null,
            entity,
            $"{typeof(T).Name} created");
    }

    /// <summary>
    /// Καταγράφει ενημέρωση entity
    /// </summary>
    public async Task LogUpdateAsync<T>(
        string entityId,
        object oldValues,
        object newValues,
        string? userId = null,
        string? userEmail = null)
    {
        await LogAsync(
            typeof(T).Name,
            entityId,
            AuditAction.Updated,
            userId,
            userEmail,
            oldValues,
            newValues,
            $"{typeof(T).Name} updated");
    }

    /// <summary>
    /// Καταγράφει διαγραφή entity
    /// </summary>
    public async Task LogDeleteAsync<T>(
        string entityId,
        object entity,
        string? userId = null,
        string? userEmail = null)
    {
        await LogAsync(
            typeof(T).Name,
            entityId,
            AuditAction.Deleted,
            userId,
            userEmail,
            entity,
            null,
            $"{typeof(T).Name} deleted");
    }

    /// <summary>
    /// Καταγράφει προβολή entity
    /// </summary>
    public async Task LogViewAsync<T>(
        string entityId,
        string? userId = null,
        string? userEmail = null)
    {
        await LogAsync(
            typeof(T).Name,
            entityId,
            AuditAction.Viewed,
            userId,
            userEmail,
            description: $"{typeof(T).Name} viewed");
    }
}
