// Author: Damianos Pythagoras Ougkrenidis
//
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// Provider-level (global Admin) κεντρική διαχείριση tenants/πελατών για εμπορικό SaaS:
/// δημιουργία πελάτη (+ TenantAdmin), ενεργοποίηση, αναστολή/απενεργοποίηση, όρια χρηστών.
/// Κάθε TenantAdmin διαχειρίζεται μόνο τον δικό του οργανισμό (βλ. UsersController).
/// </summary>
[Authorize(Roles = AppRoles.Admin)]
public class TenantsController : Controller
{
    private readonly INCISDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(INCISDbContext context, UserManager<ApplicationUser> userManager, ILogger<TenantsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public class TenantListRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public int MaxUsers { get; set; }
        public int UserCount { get; set; }
        public bool IsActive { get; set; }
        public string? PlanName { get; set; }
    }

    public async Task<IActionResult> Index()
    {
        var counts = await _context.Users
            .Where(u => u.TenantId != null)
            .GroupBy(u => u.TenantId!.Value)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count);

        var tenants = await _context.Tenants
            .Include(t => t.SubscriptionPlan)
            .OrderBy(t => t.Name)
            .ToListAsync();

        var rows = tenants.Select(t => new TenantListRow
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            Email = t.Email,
            Status = t.SubscriptionStatus,
            ExpiresAt = t.SubscriptionExpiresAt,
            MaxUsers = t.MaxUsers,
            UserCount = counts.TryGetValue(t.Id, out var c) ? c : 0,
            IsActive = t.IsActive,
            PlanName = t.SubscriptionPlan?.Name
        }).ToList();

        ViewData["Plans"] = await _context.SubscriptionPlans.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        ViewData["CreatedCredentials"] = TempData["CreatedCredentials"];
        ViewData["Error"] = TempData["Error"] ?? TempData["Error"];
        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string email, int maxUsers, int? planId, int trialDays, bool activate)
    {
        name = (name ?? string.Empty).Trim();
        email = (email ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Το όνομα πελάτη είναι υποχρεωτικό.";
            return RedirectToAction(nameof(Index));
        }
        if (await _context.Tenants.AnyAsync(t => t.Name == name))
        {
            TempData["Error"] = "Υπάρχει ήδη πελάτης με αυτό το όνομα.";
            return RedirectToAction(nameof(Index));
        }

        var slug = new string(name.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray()).Trim('-');
        if (string.IsNullOrWhiteSpace(slug)) slug = "tenant-" + Guid.NewGuid().ToString("N")[..6];

        SubscriptionPlan? plan = null;
        if (planId.HasValue)
        {
            plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == planId.Value && p.IsActive);
            if (plan == null)
            {
                TempData["Error"] = "Το επιλεγμένο πακέτο δεν είναι έγκυρο ή ενεργό.";
                return RedirectToAction(nameof(Index));
            }
        }

        var tenant = new Tenant
        {
            Name = name,
            Slug = slug,
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
            MaxUsers = plan != null ? plan.MaxUsers : Math.Clamp(maxUsers <= 0 ? 5 : maxUsers, 1, 1000),
            MaxStorageMB = plan != null ? plan.MaxStorageMB : 1024,
            SubscriptionPlanId = planId,
            SubscriptionStatus = activate ? "Active" : "Trial",
            SubscriptionExpiresAt = activate ? null : DateTime.UtcNow.AddDays(trialDays <= 0 ? 30 : trialDays),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(email))
        {
            var temp = GeneratePassword();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = name + " Admin",
                TenantId = tenant.Id,
                IsActive = true,
                IsApproved = true,
                IsSelfRegistered = false,
                EmailConfirmed = true,
                MustChangePassword = true,
                CreatedAt = DateTime.UtcNow
            };
            var res = await _userManager.CreateAsync(user, temp);
            if (res.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, AppRoles.TenantAdmin);
                TempData["CreatedCredentials"] = $"{email}  /  {temp}";
                _logger.LogInformation("Tenant {Name} (id={Id}) created; admin {Email} provisioned.", name, tenant.Id, email);
            }
            else
            {
                var errs = string.Join("; ", res.Errors.Select(e => e.Description));
                _logger.LogWarning("Tenant {Name} created but admin user failed: {Errors}", name, errs);
                TempData["Error"] = "Ο πελάτης δημιουργήθηκε αλλά ο TenantAdmin απέτυχε: " + errs;
            }
        }

        TempData["Success"] = $"Ο πελάτης «{name}» δημιουργήθηκε.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, string status)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();

        var normalized = (status ?? string.Empty).Trim();
        var allowed = new[] { "Trial", "Active", "Suspended", "Cancelled" };
        if (!allowed.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Μη έγκυρη κατάσταση συνδρομής.";
            return RedirectToAction(nameof(Index));
        }

        tenant.SubscriptionStatus = allowed.First(s => string.Equals(s, normalized, StringComparison.OrdinalIgnoreCase));
        tenant.IsActive = tenant.SubscriptionStatus is "Trial" or "Active";
        tenant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant {Id} status set to {Status} (IsActive={Active})", id, tenant.SubscriptionStatus, tenant.IsActive);
        TempData["Success"] = $"Ο πελάτης «{tenant.Name}» ορίστηκε σε {tenant.SubscriptionStatus}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLimits(int id, int maxUsers)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();

        tenant.MaxUsers = Math.Clamp(maxUsers, 1, 1000);
        tenant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Το όριο χρηστών του «{tenant.Name}» ορίστηκε σε {tenant.MaxUsers}.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Δημιουργία πακέτου συνδρομής (Provider Admin).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePlan(string name, string? description, int maxUsers, int maxFormulas,
        int maxRawMaterials, int maxStorageMB, decimal monthlyPrice, decimal annualPrice,
        bool hasWebhooks, bool hasProduction, bool hasReports, bool hasApiAccess)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Το όνομα πακέτου είναι υποχρεωτικό.";
            return RedirectToAction(nameof(Index));
        }

        var trimmed = name.Trim();
        if (await _context.SubscriptionPlans.AnyAsync(p => p.Name == trimmed))
        {
            TempData["Error"] = $"Υπάρχει ήδη πακέτο με όνομα «{trimmed}».";
            return RedirectToAction(nameof(Index));
        }

        _context.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Name = trimmed,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            MaxUsers = Math.Clamp(maxUsers <= 0 ? 5 : maxUsers, 1, 1000),
            MaxFormulas = Math.Max(0, maxFormulas),
            MaxRawMaterials = Math.Max(0, maxRawMaterials),
            MaxStorageMB = Math.Max(0, maxStorageMB),
            MonthlyPrice = Math.Max(0m, monthlyPrice),
            AnnualPrice = Math.Max(0m, annualPrice),
            HasWebhooks = hasWebhooks,
            HasProduction = hasProduction,
            HasReports = hasReports,
            HasApiAccess = hasApiAccess,
            IsActive = true
        });

        await _context.SaveChangesAsync();
        _logger.LogInformation("Created subscription plan {Name}", trimmed);
        TempData["Success"] = $"Το πακέτο «{trimmed}» δημιουργήθηκε.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Ενεργοποίηση/απενεργοποίηση πακέτου.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePlan(int id)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(id);
        if (plan == null) return NotFound();

        plan.IsActive = !plan.IsActive;
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Το πακέτο «{plan.Name}» {(plan.IsActive ? "ενεργοποιήθηκε" : "απενεργοποιήθηκε")}.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Ανανέωση συνδρομής κατά N μήνες (extend expiry, activate).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Renew(int id, int months = 1)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();

        months = Math.Clamp(months, 1, 36);
        var baseDate = tenant.SubscriptionExpiresAt.HasValue && tenant.SubscriptionExpiresAt.Value > DateTime.UtcNow
            ? tenant.SubscriptionExpiresAt.Value
            : DateTime.UtcNow;

        tenant.SubscriptionExpiresAt = baseDate.AddMonths(months);
        tenant.SubscriptionStatus = "Active";
        tenant.IsActive = true;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Renewed tenant {TenantId} by {Months} months", tenant.Id, months);
        TempData["Success"] = $"Η συνδρομή του «{tenant.Name}» ανανεώθηκε κατά {months} μήνα/ες.";
        return RedirectToAction(nameof(Index));
    }

    private static string GeneratePassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%*?";
        var all = upper + lower + digits + special;

        var chars = new List<char>
        {
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            special[RandomNumberGenerator.GetInt32(special.Length)]
        };
        while (chars.Count < 16)
            chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

        // shuffle (Fisher-Yates)
        for (int i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars.ToArray());
    }
}
