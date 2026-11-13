// Author: Damianos Pythagoras Ougkrenidis
// Subscription & Billing API Controller

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Infrastructure.ApiEnvelope;
using INCIS_SaaP.Infrastructure.Tenancy;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// API Controller for Subscription and Billing management.
/// </summary>
[ApiController]
[Route("api/v1/subscription")]
[Authorize]
[EnableRateLimiting("Strict")]
public class ApiSubscriptionController : ControllerBase
{
    private readonly INCISDbContext _context;
    private readonly ILogger<ApiSubscriptionController> _logger;
    private readonly ITenantService _tenantService;

    public ApiSubscriptionController(INCISDbContext context, ILogger<ApiSubscriptionController> logger, ITenantService tenantService)
    {
        _context = context;
        _logger = logger;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Get available subscription plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.MonthlyPrice)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.MonthlyPrice,
                p.AnnualPrice,
                p.MaxUsers,
                p.MaxFormulas,
                p.MaxRawMaterials,
                p.MaxStorageMB,
                p.ApiRateLimit,
                p.HasWebhooks,
                p.HasProduction,
                p.HasReports,
                p.HasApiAccess
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(plans));
    }

    /// <summary>
    /// Get current tenant's subscription status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] int? tenantId = null)
    {
        // current tenant (Admin may pass tenantId explicitly)
        var targetTenantId = tenantId ?? _tenantService.GetCurrentTenantId();
        if (targetTenantId == null)
            return BadRequest(ApiResponse<object>.Fail("Tenant context is required. Provide tenantId (Admin)."));

        var tenant = await _context.Tenants
            .Include(t => t.SubscriptionPlan)
            .FirstOrDefaultAsync(t => t.Id == targetTenantId.Value);

        if (tenant == null)
            return NotFound(ApiResponse<object>.Fail("Tenant not found."));

        var userCount = await _context.Users.CountAsync(u => ((ApplicationUser)u).TenantId == tenant.Id);

        return Ok(ApiResponse<object>.Ok(new
        {
            tenant.Name,
            tenant.Slug,
            Plan = tenant.SubscriptionPlan?.Name ?? "No plan",
            tenant.SubscriptionStatus,
            tenant.SubscriptionExpiresAt,
            CurrentUsers = userCount,
            tenant.MaxUsers,
            UsedStorageMB = tenant.UsedStorageBytes / (1024 * 1024),
            tenant.MaxStorageMB,
            IsExpired = tenant.SubscriptionExpiresAt.HasValue && tenant.SubscriptionExpiresAt < DateTime.UtcNow
        }));
    }

    /// <summary>
    /// Get billing invoices for current tenant
    /// </summary>
    [HttpGet("invoices")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? tenantId = null)
    {
        var targetTenantId = tenantId ?? _tenantService.GetCurrentTenantId();
        if (targetTenantId == null)
            return BadRequest(ApiResponse<object>.Fail("Tenant context is required. Provide tenantId (Admin)."));

        var query = _context.BillingInvoices.Where(i => i.TenantId == targetTenantId.Value);
        var total = await query.CountAsync();

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.Amount,
                i.Tax,
                i.TotalAmount,
                i.Currency,
                i.Status,
                i.InvoiceDate,
                i.DueDate,
                i.PaidAt,
                i.Description,
                i.PaymentMethod
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Paginated(invoices, total, page, pageSize));
    }

    /// <summary>
    /// Subscribe/change plan (Admin only)
    /// </summary>
    [HttpPost("subscribe")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request, [FromQuery] int? tenantId = null)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(request.PlanId);
        if (plan == null || !plan.IsActive)
            return NotFound(ApiResponse<object>.Fail("Plan not found or inactive."));

        var targetTenantId = tenantId ?? _tenantService.GetCurrentTenantId();
        if (targetTenantId == null)
            return BadRequest(ApiResponse<object>.Fail("Tenant context is required. Provide tenantId (Admin)."));

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == targetTenantId.Value);
        if (tenant == null)
            return NotFound(ApiResponse<object>.Fail("Tenant not found."));

        tenant.SubscriptionPlanId = plan.Id;
        tenant.SubscriptionStatus = "Active";
        tenant.MaxUsers = plan.MaxUsers;
        tenant.MaxStorageMB = plan.MaxStorageMB;
        tenant.SubscriptionExpiresAt = request.IsAnnual
            ? DateTime.UtcNow.AddYears(1)
            : DateTime.UtcNow.AddMonths(1);
        tenant.UpdatedAt = DateTime.UtcNow;

        // Create invoice
        var amount = request.IsAnnual ? plan.AnnualPrice : plan.MonthlyPrice;
        var invoice = new BillingInvoice
        {
            TenantId = tenant.Id,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMM}-{await _context.BillingInvoices.CountAsync() + 1:D4}",
            Amount = amount,
            Tax = Math.Round(amount * 0.24m, 2), // 24% VAT Greece
            TotalAmount = Math.Round(amount * 1.24m, 2),
            Status = "Pending",
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = $"Subscription: {plan.Name} ({(request.IsAnnual ? "Annual" : "Monthly")})",
            PaymentMethod = request.PaymentMethod
        };

        _context.BillingInvoices.Add(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant {TenantSlug} subscribed to plan {Plan} ({Billing})",
            tenant.Slug, plan.Name, request.IsAnnual ? "Annual" : "Monthly");

        return Ok(ApiResponse<object>.Ok(new
        {
            Plan = plan.Name,
            tenant.SubscriptionStatus,
            tenant.SubscriptionExpiresAt,
            Invoice = invoice.InvoiceNumber,
            invoice.TotalAmount
        }, $"Subscribed to {plan.Name} plan."));
    }
}

public class SubscribeRequest
{
    public int PlanId { get; set; }
    public bool IsAnnual { get; set; }
    public string? PaymentMethod { get; set; } // Card, BankTransfer
}
