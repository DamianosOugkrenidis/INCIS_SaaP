// Author: Damianos Pythagoras Ougkrenidis
//
using Microsoft.AspNetCore.Identity;
using INCIS_SaaP.Models;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace INCIS_SaaP.Services;

/// <summary>
/// Service για Two-Factor Authentication (2FA)
/// </summary>
public class TwoFactorService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(
        UserManager<ApplicationUser> userManager,
        ILogger<TwoFactorService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Ενεργοποίηση 2FA για χρήστη
    /// </summary>
    public async Task<(bool Success, string? Error, string? QrCodeUrl, string? SecretKey)> EnableTwoFactorAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, "User not found", null, null);

            // Generate secret key
            var secretKey = GenerateSecretKey();
            
            // Store the secret key temporarily (not yet enabled)
            user.TwoFactorSecretKey = secretKey;
            var result = await _userManager.UpdateAsync(user);
            
            if (!result.Succeeded)
            {
                return (false, "Failed to update user", null, null);
            }

            // Generate QR code URL for authenticator apps
            var qrCodeUrl = GenerateQrCodeUrl(
                "INCIS SaaP",
                user.Email!,
                secretKey);

            // Generate QR code image
            var qrCodeImage = GenerateQrCodeImage(qrCodeUrl);

            _logger.LogInformation("2FA enabled for user {UserId}", userId);

            return (true, null, qrCodeImage, secretKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling 2FA");
            return (false, ex.Message, null, null);
        }
    }

    /// <summary>
    /// Επαλήθευση κωδικού 2FA
    /// </summary>
    public async Task<(bool Success, string? Error)> VerifyTwoFactorCodeAsync(string userId, string code)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.TwoFactorSecretKey))
                return (false, "2FA not enabled");

            // Verify the code using TOTP
            var isValid = VerifyTotpCode(user.TwoFactorSecretKey, code);
            
            if (isValid)
            {
                // Enable 2FA permanently
                user.TwoFactorEnabled = true;
                await _userManager.UpdateAsync(user);
                
                _logger.LogInformation("2FA verified and enabled for user {UserId}", userId);
                return (true, null);
            }

            return (false, "Invalid code");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying 2FA code");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Απενεργοποίηση 2FA
    /// </summary>
    public async Task<(bool Success, string? Error)> DisableTwoFactorAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, "User not found");

            user.TwoFactorEnabled = false;
            user.TwoFactorSecretKey = null;
            
            await _userManager.UpdateAsync(user);
            
            _logger.LogInformation("2FA disabled for user {UserId}", userId);
            
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling 2FA");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Δημιουργία κωδικού επαλήθευσης (για email 2FA)
    /// </summary>
    public string GenerateVerificationCode(int length = 6)
    {
        var random = new Random();
        var code = "";
        for (int i = 0; i < length; i++)
        {
            code += random.Next(0, 10).ToString();
        }
        return code;
    }

    /// <summary>
    /// Αποστολή κωδικού 2FA μέσω email
    /// </summary>
    public async Task<bool> SendTwoFactorCodeByEmailAsync(string email, string code)
    {
        // Implementation would use IEmailService
        _logger.LogInformation("Sending 2FA code to {Email}", email);
        await Task.CompletedTask;
        return true;
    }

    private string GenerateSecretKey()
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var secret = "";
        for (int i = 0; i < 32; i++)
        {
            secret += chars[random.Next(chars.Length)];
        }
        return secret;
    }

    private string GenerateQrCodeUrl(string issuer, string account, string secret)
    {
        return $"otpauth://totp/{issuer}:{account}?secret={secret}&issuer={issuer}";
    }

    private string GenerateQrCodeImage(string qrCodeUrl)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrCodeUrl, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);
        return Convert.ToBase64String(qrCodeBytes);
    }

    /// <summary>
    /// Επαλήθευση TOTP κωδικού κατά το login (καθαρός έλεγχος, χωρίς μεταβολή κατάστασης).
    /// </summary>
    public bool ValidateCode(string? secretKey, string code) =>
        !string.IsNullOrEmpty(secretKey) && VerifyTotpCode(secretKey!, code);

    private bool VerifyTotpCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code)) return false;
        try
        {
            // Πραγματικό RFC 6238 TOTP (Otp.NET) με Base32 secret και ανοχή ±1 βήμα (30s)
            var bytes = OtpNet.Base32Encoding.ToBytes(secret);
            var totp = new OtpNet.Totp(bytes);
            return totp.VerifyTotp(code.Trim(), out _, new OtpNet.VerificationWindow(previous: 1, future: 1));
        }
        catch
        {
            return false;
        }
    }
}
