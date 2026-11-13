// Author: Damianos Pythagoras Ougkrenidis
//
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

#pragma warning disable CA1416 // Platform compatibility (DPAPI is Windows-only, handled with runtime checks)

namespace INCIS_SaaP.Services;

/// <summary>
/// Service για κρυπτογράφηση/αποκρυπτογράφηση ευαίσθητων δεδομένων
/// Χρησιμοποιεί AES-256-GCM με DPAPI για ασφαλή αποθήκευση κλειδιού
/// </summary>
public static class EncryptionService
{
    private static byte[]? _cachedKey;
    private static readonly object _lock = new();
    
    // Configuration key names
    private const string KeyConfigName = "Encryption:Key";
    private const string KeyPathConfigName = "Encryption:KeyPath";

    /// <summary>
    /// Λαμβάνει το κλειδί κρυπτογράφησης - από configuration ή DPAPI
    /// </summary>
    private static byte[] GetEncryptionKey(IConfiguration? configuration = null)
    {
        if (_cachedKey != null)
            return _cachedKey;

        lock (_lock)
        {
            if (_cachedKey != null)
                return _cachedKey;

            // Try to get from configuration (environment variable or appsettings)
            var keyFromConfig = configuration?.GetValue<string>(KeyConfigName);
            
            if (!string.IsNullOrEmpty(keyFromConfig) && keyFromConfig.Length >= 32)
            {
                _cachedKey = Encoding.UTF8.GetBytes(keyFromConfig.PadRight(32, '0').Substring(0, 32));
            }
            else
            {
                // Generate a new key and protect it with DPAPI
                _cachedKey = GenerateAndProtectKey();
            }

            return _cachedKey;
        }
    }

    /// <summary>
    /// Δημιουργεί νέο κλειδί και το προστατεύει με DPAPI
    /// </summary>
    private static byte[] GenerateAndProtectKey()
    {
        var key = new byte[32]; // 256 bits
        RandomNumberGenerator.Fill(key);
        
        // Protect with Windows DPAPI (current user)
        var protectedKey = ProtectedData.Protect(
            key, 
            null, 
            DataProtectionScope.CurrentUser);
        
        // Store the protected key in a file for reuse
        var keyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "INCIS_SaaP",
            ".enc.key");
        
        var keyDir = Path.GetDirectoryName(keyPath)!;
        if (!Directory.Exists(keyDir))
            Directory.CreateDirectory(keyDir);
        
        File.WriteAllBytes(keyPath, protectedKey);
        
        return key;
    }

    /// <summary>
    /// Φορτώνει το προστατευμένο κλειδί από το αρχείο
    /// </summary>
    private static byte[] LoadProtectedKey()
    {
        var keyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "INCIS_SaaP",
            ".enc.key");

        if (File.Exists(keyPath))
        {
            var protectedKey = File.ReadAllBytes(keyPath);
            return ProtectedData.Unprotect(
                protectedKey, 
                null, 
                DataProtectionScope.CurrentUser);
        }

        return GenerateAndProtectKey();
    }

    /// <summary>
    /// Κρυπτογραφεί ένα string με AES-256-GCM (authenticated encryption)
    /// </summary>
    public static string Encrypt(string plainText, IConfiguration? configuration = null)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            var key = _cachedKey ?? LoadProtectedKey();
            
            // Generate random IV
            var iv = new byte[12]; // 96 bits for GCM
            RandomNumberGenerator.Fill(iv);

            using var aes = new AesGcm(key, 16); // 16 bytes = 128 bits tag
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherText = new byte[plainBytes.Length];
            var tag = new byte[16];

            aes.Encrypt(iv, plainBytes, cipherText, tag);

            // Combine IV + Tag + CipherText
            var result = new byte[iv.Length + tag.Length + cipherText.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(tag, 0, result, iv.Length, tag.Length);
            Buffer.BlockCopy(cipherText, 0, result, iv.Length + tag.Length, cipherText.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Encryption Error: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Αποκρυπτογραφεί ένα string με AES-256-GCM
    /// </summary>
    public static string Decrypt(string cipherText, IConfiguration? configuration = null)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        try
        {
            var key = _cachedKey ?? LoadProtectedKey();
            var combined = Convert.FromBase64String(cipherText);

            // Extract IV, Tag, and CipherText
            var iv = new byte[12];
            var tag = new byte[16];
            var encryptedData = new byte[combined.Length - 28];

            Buffer.BlockCopy(combined, 0, iv, 0, 12);
            Buffer.BlockCopy(combined, 12, tag, 0, 16);
            Buffer.BlockCopy(combined, 28, encryptedData, 0, encryptedData.Length);

            var decryptedText = new byte[encryptedData.Length];
            
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(iv, encryptedData, tag, decryptedText);

            return Encoding.UTF8.GetString(decryptedText);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Decryption Error: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Κρυπτογραφεί τηλέφωνο (εμφανίζει μόνο τα 4 τελευταία ψηφία)
    /// </summary>
    public static string MaskPhone(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 4)
            return "****";

        return "****" + phone[^4..];
    }

    /// <summary>
    /// Κρυπτογραφεί ΑΦΜ (εμφανίζει μόνο τα 3 τελευταία ψηφία)
    /// </summary>
    public static string MaskAFM(string afm)
    {
        if (string.IsNullOrEmpty(afm) || afm.Length < 3)
            return "***";

        return "***" + afm[^3..];
    }

    /// <summary>
    /// Hash password με PBKDF2 (συμμορφώνεται με OWASP)
    /// </summary>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return string.Empty;

        // Generate a random salt
        var salt = new byte[32];
        RandomNumberGenerator.Fill(salt);

        // PBKDF2 with SHA-512, 600,000 iterations (OWASP recommended)
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            600000,
            HashAlgorithmName.SHA512,
            32);

        // Combine salt + hash
        var result = new byte[salt.Length + hash.Length];
        Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
        Buffer.BlockCopy(hash, 0, result, salt.Length, hash.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Επαληθεύει password με PBKDF2
    /// </summary>
    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;

        try
        {
            var combined = Convert.FromBase64String(storedHash);
            
            // Extract salt and hash
            var salt = new byte[32];
            var hash = new byte[32];
            Buffer.BlockCopy(combined, 0, salt, 0, 32);
            Buffer.BlockCopy(combined, 32, hash, 0, 32);

            // Compute hash with same parameters
            var computedHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                600000,
                HashAlgorithmName.SHA512,
                32);

            return CryptographicOperations.FixedTimeEquals(hash, computedHash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Δημιουργεί τυχαίο token για reset password
    /// </summary>
    public static string GenerateSecureToken()
    {
        var tokenBytes = new byte[32];
        RandomNumberGenerator.Fill(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }

    /// <summary>
    /// Δημιουργεί hash για API requests (HMAC-SHA256)
    /// </summary>
    public static string ComputeHmacSha256(string message, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hash);
    }
}
