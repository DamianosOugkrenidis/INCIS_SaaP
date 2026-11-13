// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using System.ComponentModel.DataAnnotations;

namespace INCIS_SaaP.Controllers;

/// <summary>
/// Προσωπικό panel/προφίλ χρήστη — εμφανίζει τα στοιχεία και τα δεδομένα
/// του συνδεδεμένου χρήστη (per-user). Τα στατιστικά φιλτράρονται αυτόματα
/// μέσω των global query filters.
/// </summary>
[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly INCISDbContext _context;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        INCISDbContext context,
        ILogger<ProfileController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var model = await BuildViewModel(user);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!ModelState.IsValid)
        {
            var vm = await BuildViewModel(user);
            vm.FullName = model.FullName;
            vm.Department = model.Department;
            vm.JobTitle = model.JobTitle;
            vm.Phone = model.Phone;
            return View(vm);
        }

        user.FullName = model.FullName;
        user.Department = model.Department;
        user.JobTitle = model.JobTitle;
        user.EncryptedPhone = string.IsNullOrWhiteSpace(model.Phone) ? null : EncryptionService.Encrypt(model.Phone);
        await _userManager.UpdateAsync(user);

        TempData["Success"] = "Το προφίλ σας ενημερώθηκε.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// GDPR — Δικαίωμα φορητότητας: εξαγωγή των προσωπικών &amp; επιχειρησιακών δεδομένων του χρήστη σε JSON.
    /// </summary>
    public async Task<IActionResult> ExportMyData()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var roles = await _userManager.GetRolesAsync(user);
        string? phone = null;
        if (!string.IsNullOrEmpty(user.EncryptedPhone))
        {
            try { phone = EncryptionService.Decrypt(user.EncryptedPhone); } catch { phone = null; }
        }

        // Τα queries είναι tenant-scoped (global filters) — εξάγονται δεδομένα προσβάσιμα στον χρήστη
        var export = new
        {
            ExportedAt = DateTime.UtcNow,
            Account = new
            {
                user.Id,
                user.Email,
                user.FullName,
                user.Department,
                user.JobTitle,
                Phone = phone,
                Roles = roles,
                user.CreatedAt,
                user.LastLoginAt,
                user.TenantId
            },
            Formulas = await _context.Formulas
                .Select(f => new { f.FormulaCode, f.ProductName, f.ProductType, f.CreationDate }).ToListAsync(),
            Clients = await _context.Clients
                .Select(c => new { c.Name, c.Email, c.Phone }).ToListAsync()
        };

        var json = System.Text.Json.JsonSerializer.Serialize(export,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", $"my-data-{DateTime.Now:yyyyMMdd}.json");
    }

    /// <summary>
    /// GDPR — Δικαίωμα διαγραφής: διαγράφει τον λογαριασμό του χρήστη.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMyAccount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Αποτροπή ορφανού tenant: αν είναι ο μόνος TenantAdmin με άλλα μέλη, μπλοκάρισμα
        if (user.TenantId != null && await _userManager.IsInRoleAsync(user, AppRoles.TenantAdmin))
        {
            var otherMembers = await _context.Users.CountAsync(u => u.TenantId == user.TenantId && u.Id != user.Id);
            if (otherMembers > 0)
            {
                TempData["Error"] = "Είστε ο διαχειριστής της εταιρείας με ενεργά μέλη. Ορίστε άλλον διαχειριστή ή επικοινωνήστε με την υποστήριξη πριν τη διαγραφή.";
                return RedirectToAction(nameof(Index));
            }
        }

        var email = user.Email;
        await _signInManager.SignOutAsync();
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = "Η διαγραφή απέτυχε. Επικοινωνήστε με την υποστήριξη.";
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("GDPR: user {Email} deleted their account.", email);
        return RedirectToAction("Login", "Account");
    }

    private async Task<ProfileViewModel> BuildViewModel(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        string? phone = null;
        if (!string.IsNullOrEmpty(user.EncryptedPhone))
        {
            try { phone = EncryptionService.Decrypt(user.EncryptedPhone); } catch { phone = null; }
        }

        return new ProfileViewModel
        {
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Department = user.Department,
            JobTitle = user.JobTitle,
            Phone = phone,
            Roles = string.Join(", ", roles),
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsApproved = user.IsApproved,
            // Στατιστικά (φιλτράρονται per-user από τα global query filters)
            FormulasCount = await _context.Formulas.CountAsync(),
            ClientsCount = await _context.Clients.CountAsync()
        };
    }
}

public class ProfileViewModel
{
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Πλήρες Όνομα")]
    [MaxLength(200)]
    public string? FullName { get; set; }

    [Display(Name = "Τμήμα")]
    [MaxLength(200)]
    public string? Department { get; set; }

    [Display(Name = "Θέση Εργασίας")]
    [MaxLength(200)]
    public string? JobTitle { get; set; }

    [Display(Name = "Τηλέφωνο")]
    [Phone(ErrorMessage = "Μη έγκυρος αριθμός τηλεφώνου")]
    public string? Phone { get; set; }

    public string Roles { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsApproved { get; set; }
    public int FormulasCount { get; set; }
    public int ClientsCount { get; set; }
}
