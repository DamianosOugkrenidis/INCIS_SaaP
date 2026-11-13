// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// Διαχείριση χρηστών (μόνο Admin) — έγκριση self-registered χρηστών,
/// ενεργοποίηση/απενεργοποίηση. Απαραίτητο για multi-tenant SaaS onboarding.
/// </summary>
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.TenantAdmin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INCISDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        INCISDbContext context,
        EmailService emailService,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    private bool IsGlobalAdmin => User.IsInRole(AppRoles.Admin);

    private async Task<int?> GetCurrentTenantIdAsync()
    {
        var u = await _userManager.GetUserAsync(User);
        return u?.TenantId;
    }

    public async Task<IActionResult> Index(string? filter, string? searchString)
    {
        var query = _userManager.Users.AsQueryable();

        // TenantAdmin βλέπει ΜΟΝΟ τους χρήστες της εταιρείας του· ο global Admin τους πάντες
        int? scopeTenantId = null;
        if (!IsGlobalAdmin)
        {
            scopeTenantId = await GetCurrentTenantIdAsync();
            query = query.Where(u => u.TenantId == scopeTenantId);
        }

        if (filter == "pending")
            query = query.Where(u => !u.IsApproved);
        else if (filter == "inactive")
            query = query.Where(u => !u.IsActive);

        if (!string.IsNullOrWhiteSpace(searchString))
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(searchString))
                || (u.FullName != null && u.FullName.Contains(searchString)));

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

        // Tenant ονόματα
        var tenantIds = users.Where(u => u.TenantId != null).Select(u => u.TenantId!.Value).Distinct().ToList();
        var tenants = await _context.Tenants.Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name);

        ViewData["Tenants"] = tenants;
        ViewData["CurrentFilter"] = filter;
        ViewData["SearchString"] = searchString;
        ViewData["IsGlobalAdmin"] = IsGlobalAdmin;
        ViewData["PendingCount"] = IsGlobalAdmin
            ? await _userManager.Users.CountAsync(u => !u.IsApproved)
            : await _userManager.Users.CountAsync(u => !u.IsApproved && u.TenantId == scopeTenantId);

        // Legacy (κοινόχρηστα) δεδομένα — ΜΟΝΟ ο global Admin
        if (IsGlobalAdmin)
        {
            ViewData["LegacyFormulas"] = await _context.Formulas.CountAsync(f => f.TenantId == null);
            ViewData["LegacyRawMaterials"] = await _context.RawMaterials.CountAsync(r => r.TenantId == null);
            ViewData["AllTenants"] = await _context.Tenants.Where(t => t.IsActive)
                .OrderBy(t => t.Name).Select(t => new { t.Id, t.Name }).ToListAsync();
        }

        return View(users);
    }

    /// <summary>
    /// Αναθέτει τα κοινόχρηστα/legacy δεδομένα (TenantId == null) σε συγκεκριμένο tenant.
    /// Χρήσιμο για καθαρή έναρξη SaaS με υπάρχοντα δεδομένα. (μόνο global Admin)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> AssignLegacyData(int tenantId)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) { TempData["Error"] = "Μη έγκυρος οργανισμός."; return RedirectToAction(nameof(Index)); }

        int n = 0;
        n += await _context.Formulas.Where(f => f.TenantId == null).ExecuteUpdateAsync(s => s.SetProperty(f => f.TenantId, tenantId));
        n += await _context.Clients.Where(c => c.TenantId == null).ExecuteUpdateAsync(s => s.SetProperty(c => c.TenantId, tenantId));
        n += await _context.RawMaterials.Where(r => r.TenantId == null).ExecuteUpdateAsync(s => s.SetProperty(r => r.TenantId, tenantId));
        n += await _context.Suppliers.Where(su => su.TenantId == null).ExecuteUpdateAsync(s => s.SetProperty(su => su.TenantId, tenantId));

        _logger.LogInformation("Admin assigned {Count} legacy records to tenant {TenantId}", n, tenantId);
        TempData["Success"] = $"Ανατέθηκαν {n} εγγραφές στον οργανισμό '{tenant.Name}'.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string id)
    {
        _logger.LogInformation("Approve invoked: targetId={Id} actor={Actor}", id, _userManager.GetUserId(User));
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) { _logger.LogWarning("Approve: user {Id} not found", id); return NotFound(); }

        // TenantAdmin μπορεί να εγκρίνει ΜΟΝΟ χρήστες της δικής του εταιρείας
        if (!IsGlobalAdmin && user.TenantId != await GetCurrentTenantIdAsync())
        {
            _logger.LogWarning("Approve denied: actor={Actor} actorTenant={ActorTenant} target={Target} targetTenant={TargetTenant}",
                _userManager.GetUserId(User), await GetCurrentTenantIdAsync(), user.Id, user.TenantId);
            return Forbid();
        }

        user.IsApproved = true;
        user.IsActive = true;
        await _userManager.UpdateAsync(user);
        _logger.LogInformation("Admin approved user {Email}", user.Email);

        // Ειδοποίηση χρήστη (best-effort — δεν αποτυγχάνει αν λείπει SMTP config)
        if (!string.IsNullOrEmpty(user.Email))
        {
            try
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Ο λογαριασμός σας εγκρίθηκε - INCIS SaaP",
                    $"<p>Γεια σας {user.FullName},</p><p>Ο λογαριασμός σας στο INCIS SaaP εγκρίθηκε. Μπορείτε πλέον να συνδεθείτε.</p>");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Approval email could not be sent to {Email}", user.Email);
            }
        }

        TempData["Success"] = $"Ο χρήστης {user.Email} εγκρίθηκε.";
        return RedirectToAction(nameof(Index), new { filter = "pending" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) { _logger.LogWarning("ToggleActive: user {Id} not found", id); return NotFound(); }

        // TenantAdmin μόνο για χρήστες της εταιρείας του
        if (!IsGlobalAdmin && user.TenantId != await GetCurrentTenantIdAsync())
        {
            _logger.LogWarning("ToggleActive denied: actor={Actor} actorTenant={ActorTenant} target={Target} targetTenant={TargetTenant}",
                _userManager.GetUserId(User), await GetCurrentTenantIdAsync(), user.Id, user.TenantId);
            return Forbid();
        }

        // Αποτροπή αυτο-απενεργοποίησης
        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "Δεν μπορείτε να απενεργοποιήσετε τον δικό σας λογαριασμό.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
        TempData["Success"] = user.IsActive ? $"Ο χρήστης {user.Email} ενεργοποιήθηκε." : $"Ο χρήστης {user.Email} απενεργοποιήθηκε.";
        return RedirectToAction(nameof(Index), new { filter = ViewData["CurrentFilter"] });
    }
}
