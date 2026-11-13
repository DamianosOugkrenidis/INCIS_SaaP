// Author: Damianos Pythagoras Ougkrenidis
// Webhooks API Controller

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
/// API Controller for Webhook subscription management
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
[Authorize(Roles = "Admin,SuperUser,TenantAdmin")]
[EnableRateLimiting("Strict")]
public class ApiWebhooksController : ControllerBase
{
    private readonly INCISDbContext _context;
    private readonly ILogger<ApiWebhooksController> _logger;
    private readonly ITenantService _tenantService;

    public ApiWebhooksController(INCISDbContext context, ILogger<ApiWebhooksController> logger, ITenantService tenantService)
    {
        _context = context;
        _logger = logger;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Get all webhook subscriptions for current tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var webhooks = await _context.WebhookSubscriptions
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new
            {
                w.Id,
                w.Url,
                w.Events,
                w.IsActive,
                w.FailureCount,
                w.LastSuccessAt,
                w.LastFailureAt,
                w.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(webhooks));
    }

    /// <summary>
    /// Create a new webhook subscription
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWebhookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(ApiResponse<object>.Fail("URL is required."));

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
            return BadRequest(ApiResponse<object>.Fail("Invalid URL. Must be a valid HTTP/HTTPS URL."));

        if (string.IsNullOrWhiteSpace(request.Events))
            return BadRequest(ApiResponse<object>.Fail("At least one event is required."));

        // Generate a secret for HMAC signing
        var secretBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(secretBytes);
        }
        var secret = Convert.ToBase64String(secretBytes);

        var tenantId = _tenantService.GetCurrentTenantId();
        if (tenantId == null)
            return BadRequest(ApiResponse<object>.Fail("Tenant context is required to create webhooks."));

        // Feature gating: το πακέτο πρέπει να περιλαμβάνει webhooks
        var tenant = _tenantService.GetCurrentTenant();
        if (tenant?.SubscriptionPlan != null && !tenant.SubscriptionPlan.HasWebhooks)
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<object>.Fail("Το πακέτο συνδρομής σας δεν περιλαμβάνει webhooks."));

        var webhook = new WebhookSubscription
        {
            TenantId = tenantId,
            Url = request.Url,
            Secret = secret,
            Events = request.Events,
            IsActive = true
        };

        _context.WebhookSubscriptions.Add(webhook);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Webhook subscription created: {Url} for events [{Events}]", request.Url, request.Events);

        return CreatedAtAction(nameof(GetAll), null,
            ApiResponse<object>.Ok(new
            {
                webhook.Id,
                webhook.Url,
                webhook.Secret, // Only returned once at creation!
                webhook.Events,
                webhook.IsActive,
                webhook.CreatedAt
            }, "Webhook created. Save the secret - it won't be shown again."));
    }

    /// <summary>
    /// Update webhook subscription
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWebhookRequest request)
    {
        var webhook = await _context.WebhookSubscriptions.FirstOrDefaultAsync(w => w.Id == id);
        if (webhook == null)
            return NotFound(ApiResponse<object>.Fail("Webhook not found."));

        if (!string.IsNullOrWhiteSpace(request.Url))
            webhook.Url = request.Url;
        if (!string.IsNullOrWhiteSpace(request.Events))
            webhook.Events = request.Events;
        if (request.IsActive.HasValue)
            webhook.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { webhook.Id, webhook.Url, webhook.Events, webhook.IsActive }, "Webhook updated."));
    }

    /// <summary>
    /// Delete webhook subscription
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var webhook = await _context.WebhookSubscriptions.FirstOrDefaultAsync(w => w.Id == id);
        if (webhook == null)
            return NotFound(ApiResponse<object>.Fail("Webhook not found."));

        _context.WebhookSubscriptions.Remove(webhook);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Webhook deleted: {Id} ({Url})", id, webhook.Url);
        return Ok(ApiResponse<object>.Ok(new { deleted = true }, "Webhook deleted."));
    }

    /// <summary>
    /// Get delivery history for a webhook
    /// </summary>
    [HttpGet("{id}/deliveries")]
    public async Task<IActionResult> GetDeliveries(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var deliveries = await _context.WebhookDeliveries
            .Where(d => d.WebhookSubscriptionId == id)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                d.Id,
                d.EventType,
                d.Status,
                d.ResponseStatusCode,
                d.Attempts,
                d.DurationMs,
                d.CreatedAt,
                d.DeliveredAt
            })
            .ToListAsync();

        var total = await _context.WebhookDeliveries.CountAsync(d => d.WebhookSubscriptionId == id);

        return Ok(ApiResponse<object>.Paginated(deliveries, total, page, pageSize));
    }

    /// <summary>
    /// Test a webhook (sends a test payload)
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<IActionResult> Test(int id)
    {
        var webhook = await _context.WebhookSubscriptions.FirstOrDefaultAsync(w => w.Id == id);
        if (webhook == null)
            return NotFound(ApiResponse<object>.Fail("Webhook not found."));

        // Create test delivery record
        var delivery = new WebhookDelivery
        {
            WebhookSubscriptionId = id,
            EventType = "test.ping",
            Payload = System.Text.Json.JsonSerializer.Serialize(new { @event = "test.ping", timestamp = DateTime.UtcNow, data = new { message = "This is a test webhook delivery." } }),
            Status = "Pending"
        };

        _context.WebhookDeliveries.Add(delivery);
        await _context.SaveChangesAsync();

        // Attempt delivery
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await httpClient.PostAsync(webhook.Url,
                new StringContent(delivery.Payload, System.Text.Encoding.UTF8, "application/json"));

            delivery.ResponseStatusCode = (int)response.StatusCode;
            delivery.Status = response.IsSuccessStatusCode ? "Success" : "Failed";
            delivery.DeliveredAt = DateTime.UtcNow;
            delivery.Attempts = 1;
            delivery.DurationMs = (int)(DateTime.UtcNow - delivery.CreatedAt).TotalMilliseconds;
        }
        catch (Exception ex)
        {
            delivery.Status = "Failed";
            delivery.ResponseBody = ex.Message;
            delivery.Attempts = 1;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { delivery.Id, delivery.Status, delivery.ResponseStatusCode }, "Test webhook sent."));
    }
}

/// <summary>
/// Available webhook event types
/// </summary>
public static class WebhookEvents
{
    public const string FormulaCreated = "formula.created";
    public const string FormulaValidated = "formula.validated";
    public const string FormulaProduced = "formula.produced";
    public const string RawMaterialCreated = "rawmaterial.created";
    public const string RawMaterialExpired = "rawmaterial.expired";
    public const string ClientCreated = "client.created";
    public const string BatchCompleted = "batch.completed";
    public const string SampleExpired = "sample.expired";
}

public class CreateWebhookRequest
{
    public string Url { get; set; } = string.Empty;
    public string Events { get; set; } = string.Empty; // Comma-separated: "formula.created,batch.completed"
}

public class UpdateWebhookRequest
{
    public string? Url { get; set; }
    public string? Events { get; set; }
    public bool? IsActive { get; set; }
}
