// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using System.ComponentModel.DataAnnotations;

namespace INCIS_SaaP.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly INCISDbContext _context;
    private readonly ILogger<AccountController> _logger;
    private readonly TwoFactorService _twoFactorService;
    private readonly EmailService _emailService;
    private readonly IConfiguration _configuration;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        INCISDbContext context,
        ILogger<AccountController> logger,
        TwoFactorService twoFactorService,
        EmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _logger = logger;
        _twoFactorService = twoFactorService;
        _emailService = emailService;
        _configuration = configuration;
    }

    private bool SelfRegistrationEnabled => _configuration.GetValue("Security:AllowSelfRegistration", true);

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // If already signed-in, send to Home (so '/' opens dashboard for logged-in users)
        if (User?.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View();
            }

            // Set MustChangePassword to false after successful password change
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User changed their password successfully.");
            return RedirectToAction(nameof(Index), "Home");
        }

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(model.Email);
            }
            
            if (user != null)
            {
                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Ο λογαριασμός σας είναι απενεργοποιημένος.");
                    return View(model);
                }

                if (!user.IsApproved)
                {
                    ModelState.AddModelError(string.Empty, "Ο λογαριασμός σας εκκρεμεί έγκριση από διαχειριστή.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName!,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    // Επιβολή συνδρομής/trial ανά tenant (εξαιρούνται οι Admin)
                    if (user.TenantId != null && !await _userManager.IsInRoleAsync(user, AppRoles.Admin))
                    {
                        var tenant = await _context.Tenants.FindAsync(user.TenantId.Value);
                        if (tenant != null)
                        {
                            var inactive = !tenant.IsActive
                                || tenant.SubscriptionStatus == "Suspended"
                                || tenant.SubscriptionStatus == "Cancelled";
                            var expired = tenant.SubscriptionStatus != "Active"
                                && tenant.SubscriptionExpiresAt.HasValue
                                && tenant.SubscriptionExpiresAt.Value < DateTime.UtcNow;

                            if (inactive || expired)
                            {
                                await _signInManager.SignOutAsync();
                                ModelState.AddModelError(string.Empty,
                                    "Η συνδρομή του οργανισμού σας έχει λήξει ή είναι ανενεργή. Επικοινωνήστε με τον διαχειριστή.");
                                return View(model);
                            }
                        }
                    }

                    // Two-Factor Authentication: αν είναι ενεργό, απαίτησε κωδικό πριν την πλήρη σύνδεση
                    if (user.TwoFactorEnabled && !string.IsNullOrEmpty(user.TwoFactorSecretKey))
                    {
                        await _signInManager.SignOutAsync();
                        TempData["2fa_userId"] = user.Id;
                        TempData["2fa_remember"] = model.RememberMe;
                        TempData["2fa_returnUrl"] = returnUrl;
                        return RedirectToAction(nameof(LoginTwoFactor));
                    }

                    // Ενημέρωση στοιχείων τελευταίας σύνδεσης
                    user.LastLoginAt = DateTime.Now;
                    user.LastLoginIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _userManager.UpdateAsync(user);

                    // If MustChangePassword is set, allow the demo admin to proceed without redirecting
                    // and clear the flag. For any other user keep existing behavior.
                    if (user.MustChangePassword)
                    {
                        if (string.Equals(user.Email, "admin.demo@incis.local", StringComparison.OrdinalIgnoreCase))
                        {
                            user.MustChangePassword = false;
                            await _userManager.UpdateAsync(user);
                        }
                        else
                        {
                            return RedirectToAction(nameof(ChangePassword));
                        }
                    }

                    return LocalRedirect(returnUrl ?? Url.Action("Index", "Home") ?? "/");
                }
                
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    ModelState.AddModelError(string.Empty, "Ο λογαριασμός είναι κλειδωμένος. Δοκιμάστε αργότερα.");
                    return View(model);
                }
            }
            
            ModelState.AddModelError(string.Empty, "Ελέγξτε τα στοιχεία σας και δοκιμάστε ξανά.");
        }

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult LoginTwoFactor()
    {
        if (TempData.Peek("2fa_userId") is not string)
            return RedirectToAction(nameof(Login));

        TempData.Keep("2fa_userId");
        TempData.Keep("2fa_remember");
        TempData.Keep("2fa_returnUrl");
        return View(new TwoFactorLoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginTwoFactor(TwoFactorLoginViewModel model)
    {
        var userId = TempData.Peek("2fa_userId") as string;
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
        {
            TempData.Keep();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return RedirectToAction(nameof(Login));

        if (!_twoFactorService.ValidateCode(user.TwoFactorSecretKey, model.Code))
        {
            ModelState.AddModelError(string.Empty, "Μη έγκυρος κωδικός επαλήθευσης. Δοκιμάστε ξανά.");
            TempData.Keep();
            return View(model);
        }

        var remember = TempData["2fa_remember"] as bool? ?? false;
        var returnUrl = TempData["2fa_returnUrl"] as string;

        await _signInManager.SignInAsync(user, isPersistent: remember);
        user.LastLoginAt = DateTime.Now;
        user.LastLoginIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User completed 2FA login.");
        return LocalRedirect(returnUrl ?? Url.Action("Index", "Home") ?? "/");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        if (!SelfRegistrationEnabled)
        {
            TempData["Info"] = "Η δημόσια εγγραφή είναι απενεργοποιημένη. Επικοινωνήστε με τον διαχειριστή.";
            return RedirectToAction(nameof(Login));
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        if (!SelfRegistrationEnabled)
        {
            TempData["Info"] = "Η δημόσια εγγραφή είναι απενεργοποιημένη. Επικοινωνήστε με τον διαχειριστή.";
            return RedirectToAction(nameof(Login));
        }
        ViewData["ReturnUrl"] = returnUrl;
        
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsSelfRegistered = true,
                IsApproved = false, // Requires admin approval for self-registered users
                Department = model.Department,
                JobTitle = model.JobTitle,
                // Κρυπτογράφηση ευαίσθητων δεδομένων
                EncryptedPhone = !string.IsNullOrEmpty(model.Phone) 
                    ? EncryptionService.Encrypt(model.Phone) : null,
                EncryptedAFM = !string.IsNullOrEmpty(model.AFM) 
                    ? EncryptionService.Encrypt(model.AFM) : null
            };

            // Assign to existing tenant if organization name matches, or create new
            var isTenantOwner = false;
            if (!string.IsNullOrEmpty(model.OrganizationName))
            {
                var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Name == model.OrganizationName && t.IsActive);
                if (tenant != null)
                {
                    // Επιβολή ορίου χρηστών συνδρομής
                    var userCount = await _context.Users.CountAsync(u => u.TenantId == tenant.Id);
                    if (userCount >= tenant.MaxUsers)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Ο οργανισμός '{tenant.Name}' έχει φτάσει το όριο χρηστών ({tenant.MaxUsers}) της συνδρομής του. Επικοινωνήστε με τον διαχειριστή του οργανισμού.");
                        return View(model);
                    }
                    user.TenantId = tenant.Id;
                }
                else
                {
                    // Create new tenant with trial subscription — ο δημιουργός γίνεται TenantAdmin
                    var newTenant = new Tenant
                    {
                        Name = model.OrganizationName,
                        Slug = model.OrganizationName.ToLower().Replace(" ", "-").Replace(".", ""),
                        Email = model.Email,
                        SubscriptionStatus = "Trial",
                        SubscriptionExpiresAt = DateTime.UtcNow.AddDays(30),
                        MaxUsers = 5
                    };
                    _context.Tenants.Add(newTenant);
                    await _context.SaveChangesAsync();
                    user.TenantId = newTenant.Id;
                    isTenantOwner = true;
                    user.IsApproved = true; // ο ιδιοκτήτης της εταιρείας εγκρίνεται αυτόματα
                }
            }

            var result = await _userManager.CreateAsync(user, model.Password);
            
            if (result.Succeeded)
            {
                if (isTenantOwner)
                {
                    // Ιδιοκτήτης εταιρείας: TenantAdmin, μπορεί να συνδεθεί άμεσα και να διαχειρίζεται τους χρήστες του
                    await _userManager.AddToRoleAsync(user, AppRoles.TenantAdmin);
                    _logger.LogInformation("Tenant owner {Email} registered and approved (TenantAdmin)", model.Email);
                    TempData["Success"] = "Ο οργανισμός σας δημιουργήθηκε! Μπορείτε να συνδεθείτε και να προσκαλέσετε/εγκρίνετε χρήστες.";
                    return RedirectToAction("Login");
                }

                // Μέλος υπάρχουσας εταιρείας: User, εκκρεμεί έγκριση από τον TenantAdmin
                await _userManager.AddToRoleAsync(user, AppRoles.User);
                _logger.LogInformation("Self-registration: User {Email} registered (pending approval)", model.Email);
                TempData["Info"] = "Η εγγραφή σας ολοκληρώθηκε. Θα λάβετε ειδοποίηση όταν εγκριθεί ο λογαριασμός σας από τον διαχειριστή της εταιρείας σας.";
                return RedirectToAction("Login");
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // ─────────────────────────────────────────────────────────────
    // Ξεχάσατε / Επαναφορά κωδικού (μέσω email)
    // ─────────────────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        // Δεν αποκαλύπτουμε αν υπάρχει ο χρήστης (αποφυγή enumeration)
        if (user != null && user.IsActive)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
            var resetUrl = Url.Action(nameof(ResetPassword), "Account",
                new { email = user.Email, token = encoded }, Request.Scheme);

            try
            {
                await _emailService.SendEmailAsync(user.Email!,
                    "Επαναφορά κωδικού - INCIS SaaP",
                    $"<p>Γεια σας {user.FullName},</p><p>Για επαναφορά του κωδικού σας πατήστε <a href=\"{resetUrl}\">εδώ</a>. Ο σύνδεσμος λήγει σύντομα. Αν δεν το ζητήσατε, αγνοήστε το.</p>");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Password reset email could not be sent to {Email}", user.Email);
            }
        }

        TempData["Info"] = "Αν υπάρχει λογαριασμός με αυτό το email, θα λάβετε οδηγίες επαναφοράς.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            return RedirectToAction(nameof(Login));
        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            TempData["Success"] = "Ο κωδικός σας ενημερώθηκε. Μπορείτε να συνδεθείτε.";
            return RedirectToAction(nameof(Login));
        }

        string token;
        try { token = System.Text.Encoding.UTF8.GetString(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(model.Token)); }
        catch { ModelState.AddModelError(string.Empty, "Μη έγκυρο token επαναφοράς."); return View(model); }

        var result = await _userManager.ResetPasswordAsync(user, token, model.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset for {Email}", user.Email);
            TempData["Success"] = "Ο κωδικός σας ενημερώθηκε. Μπορείτε να συνδεθείτε.";
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }
}

// View Models
public class LoginViewModel
{
    [Required(ErrorMessage = "Το email είναι υποχρεωτικό")]
    [EmailAddress(ErrorMessage = "Μη έγκυρη διεύθυνση email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ο κωδικός είναι υποχρεωτικός")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Να με θυμάσαι")]
    public bool RememberMe { get; set; }
}

public class TwoFactorLoginViewModel
{
    [Required(ErrorMessage = "Ο κωδικός επαλήθευσης είναι υποχρεωτικός")]
    [StringLength(8, MinimumLength = 6, ErrorMessage = "Ο κωδικός είναι 6 ψηφία")]
    [Display(Name = "Κωδικός Επαλήθευσης")]
    public string Code { get; set; } = string.Empty;
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Το email είναι υποχρεωτικό")]
    [EmailAddress(ErrorMessage = "Μη έγκυρη διεύθυνση email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ο κωδικός είναι υποχρεωτικός")]
    [StringLength(100, MinimumLength = 12, ErrorMessage = "Ο κωδικός πρέπει να είναι τουλάχιστον {2} χαρακτήρες.")]
    [DataType(DataType.Password)]
    [Display(Name = "Νέος Κωδικός")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Επιβεβαίωση Κωδικού")]
    [Compare("Password", ErrorMessage = "Οι κωδικοί δεν ταιριάζουν.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Το όνομα είναι υποχρεωτικό")]
    [Display(Name = "Πλήρες Όνομα")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Το email είναι υποχρεωτικό")]
    [EmailAddress(ErrorMessage = "Μη έγκυρη διεύθυνση email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Μη έγκυρος αριθμός τηλεφώνου")]
    [Display(Name = "Τηλέφωνο")]
    public string? Phone { get; set; }

    [Display(Name = "ΑΦΜ")]
    public string? AFM { get; set; }

    [Required(ErrorMessage = "Ο οργανισμός/εταιρεία είναι υποχρεωτικός")]
    [Display(Name = "Οργανισμός / Εταιρεία")]
    public string? OrganizationName { get; set; }

    [Display(Name = "Τμήμα")]
    public string? Department { get; set; }

    [Display(Name = "Θέση Εργασίας")]
    public string? JobTitle { get; set; }

    [Required(ErrorMessage = "Ο κωδικός είναι υποχρεωτικός")]
    [StringLength(100, ErrorMessage = "Ο κωδικός πρέπει να είναι τουλάχιστον {2} χαρακτήρες.", MinimumLength = 12)]
    [DataType(DataType.Password)]
    [Display(Name = "Κωδικός")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Επιβεβαίωση κωδικού")]
    [Compare("Password", ErrorMessage = "Οι κωδικοί δεν ταιριάζουν.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Τρέχων κωδικός")]
    public string OldPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "Ο {0} πρέπει να είναι τουλάχιστον {2} και μέγιστο {1} χαρακτήρες.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Νέος κωδικός")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Επιβεβαίωση νέου κωδικού")]
    [Compare("NewPassword", ErrorMessage = "Ο νέος κωδικός και η επιβεβαίωση δεν ταιριάζουν.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
