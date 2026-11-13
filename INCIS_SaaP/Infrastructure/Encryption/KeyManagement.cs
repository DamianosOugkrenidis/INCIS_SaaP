// Author: Damianos Pythagoras Ougkrenidis
// Advanced Encryption - Key Rotation, Envelope Encryption, Tokenization

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace INCIS_SaaP.Infrastructure.Encryption;

/// <summary>
/// Encryption key metadata stored in the database
/// </summary>
public class EncryptionKeyMetadata
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string KeyId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Algorithm { get; set; } = "AES-256-GCM";
    
    /// <summary>
    /// The DEK (Data Encryption Key) encrypted by the KEK (Key Encryption Key)
    /// This is the envelope encryption pattern
    /// </summary>
    [Required]
    public string EncryptedKeyMaterial { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RotatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Purpose: "FormulasEncryption", "TokenizationKey", "AuditLogKey"
    /// </summary>
    [MaxLength(100)]
    public string Purpose { get; set; } = string.Empty;
    
    public int Version { get; set; } = 1;
}

/// <summary>
/// Tokenized value storage (for sensitive fields like AFM, phone)
/// </summary>
public class TokenVaultEntry
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(64)]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    public string EncryptedValue { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FieldType { get; set; } = string.Empty; // "AFM", "Phone", "Email"
    
    [MaxLength(100)]
    public string? EntityType { get; set; }
    
    public int? EntityId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(64)]
    public string KeyId { get; set; } = string.Empty;
}

/// <summary>
/// Key Management Service - handles key rotation and envelope encryption
/// </summary>
public interface IKeyManagementService
{
    /// <summary>
    /// Generates a new DEK (Data Encryption Key) wrapped with the KEK
    /// </summary>
    Task<string> GenerateDataKeyAsync(string purpose, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current active key for a purpose
    /// </summary>
    Task<byte[]> GetActiveKeyAsync(string purpose, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rotates the key for a specific purpose (creates new key, marks old as inactive)
    /// </summary>
    Task RotateKeyAsync(string purpose, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Decrypts data that was encrypted with a specific key version
    /// </summary>
    Task<byte[]> GetKeyByIdAsync(string keyId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Tokenization service - replaces sensitive values with tokens
/// </summary>
public interface ITokenizationService
{
    /// <summary>
    /// Tokenizes a value (stores encrypted original, returns token)
    /// </summary>
    Task<string> TokenizeAsync(string value, string fieldType, string? entityType = null, 
        int? entityId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Detokenizes (retrieves original value from token)
    /// Requires appropriate permissions
    /// </summary>
    Task<string> DetokenizeAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a value is a token
    /// </summary>
    bool IsToken(string value);
    
    /// <summary>
    /// Gets masked version without detokenizing (e.g., "****1234")
    /// </summary>
    Task<string> GetMaskedValueAsync(string token, CancellationToken cancellationToken = default);
}

/// <summary>
/// Key management implementation using envelope encryption
/// KEK (Key Encryption Key) is from DPAPI/config, DEKs (Data Encryption Keys) are in DB
/// </summary>
public class KeyManagementService : IKeyManagementService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KeyManagementService> _logger;
    private readonly IConfiguration _configuration;
    private byte[]? _kek; // Key Encryption Key (from secure source)

    public KeyManagementService(IServiceProvider serviceProvider, ILogger<KeyManagementService> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    private byte[] GetKEK()
    {
        if (_kek != null) return _kek;

        // KEK from configuration (in production, this would come from HSM/Azure Key Vault)
        var kekBase64 = _configuration["Encryption:KEK"];
        if (!string.IsNullOrEmpty(kekBase64))
        {
            _kek = Convert.FromBase64String(kekBase64);
        }
        else
        {
            // Generate and store KEK using DPAPI (Windows) or file-based key
            _kek = GenerateAndStoreKEK();
        }
        return _kek;
    }

    public async Task<string> GenerateDataKeyAsync(string purpose, CancellationToken cancellationToken = default)
    {
        var dek = new byte[32]; // 256-bit key
        RandomNumberGenerator.Fill(dek);

        // Encrypt DEK with KEK (envelope encryption)
        var encryptedDek = EncryptWithKEK(dek);
        var keyId = $"key-{purpose}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..50];

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var keyMetadata = new EncryptionKeyMetadata
        {
            KeyId = keyId,
            Algorithm = "AES-256-GCM",
            EncryptedKeyMaterial = Convert.ToBase64String(encryptedDek),
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddDays(90), // 90-day rotation period
            IsActive = true
        };

        dbContext.Set<EncryptionKeyMetadata>().Add(keyMetadata);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated new data key: {KeyId} for purpose: {Purpose}", keyId, purpose);
        return keyId;
    }

    public async Task<byte[]> GetActiveKeyAsync(string purpose, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var keyMetadata = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                dbContext.Set<EncryptionKeyMetadata>()
                    .Where(k => k.Purpose == purpose && k.IsActive)
                    .OrderByDescending(k => k.Version),
                cancellationToken);

        if (keyMetadata == null)
        {
            // Auto-generate first key
            await GenerateDataKeyAsync(purpose, cancellationToken);
            keyMetadata = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstOrDefaultAsync(
                    dbContext.Set<EncryptionKeyMetadata>()
                        .Where(k => k.Purpose == purpose && k.IsActive),
                    cancellationToken);
        }

        if (keyMetadata == null)
            throw new InvalidOperationException($"No encryption key found for purpose: {purpose}");

        // Decrypt DEK with KEK
        var encryptedDek = Convert.FromBase64String(keyMetadata.EncryptedKeyMaterial);
        return DecryptWithKEK(encryptedDek);
    }

    public async Task RotateKeyAsync(string purpose, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        // Deactivate current key
        var currentKeys = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                dbContext.Set<EncryptionKeyMetadata>()
                    .Where(k => k.Purpose == purpose && k.IsActive),
                cancellationToken);

        foreach (var key in currentKeys)
        {
            key.IsActive = false;
            key.RotatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Generate new key
        await GenerateDataKeyAsync(purpose, cancellationToken);
        
        _logger.LogInformation("Rotated encryption key for purpose: {Purpose}", purpose);
    }

    public async Task<byte[]> GetKeyByIdAsync(string keyId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var keyMetadata = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                dbContext.Set<EncryptionKeyMetadata>().Where(k => k.KeyId == keyId),
                cancellationToken);

        if (keyMetadata == null)
            throw new InvalidOperationException($"Key not found: {keyId}");

        var encryptedDek = Convert.FromBase64String(keyMetadata.EncryptedKeyMaterial);
        return DecryptWithKEK(encryptedDek);
    }

    private byte[] EncryptWithKEK(byte[] plainData)
    {
        var kek = GetKEK();
        var iv = new byte[12];
        RandomNumberGenerator.Fill(iv);

        using var aes = new AesGcm(kek, 16);
        var cipherText = new byte[plainData.Length];
        var tag = new byte[16];
        aes.Encrypt(iv, plainData, cipherText, tag);

        // IV + Tag + CipherText
        var result = new byte[iv.Length + tag.Length + cipherText.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(tag, 0, result, iv.Length, tag.Length);
        Buffer.BlockCopy(cipherText, 0, result, iv.Length + tag.Length, cipherText.Length);
        return result;
    }

    private byte[] DecryptWithKEK(byte[] encryptedData)
    {
        var kek = GetKEK();
        var iv = new byte[12];
        var tag = new byte[16];
        var cipherText = new byte[encryptedData.Length - 28];

        Buffer.BlockCopy(encryptedData, 0, iv, 0, 12);
        Buffer.BlockCopy(encryptedData, 12, tag, 0, 16);
        Buffer.BlockCopy(encryptedData, 28, cipherText, 0, cipherText.Length);

        using var aes = new AesGcm(kek, 16);
        var plainText = new byte[cipherText.Length];
        aes.Decrypt(iv, cipherText, tag, plainText);
        return plainText;
    }

    private byte[] GenerateAndStoreKEK()
    {
#pragma warning disable CA1416 // DPAPI (ProtectedData) is Windows-only by design; app targets Windows
        var keyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "INCIS_SaaP", ".kek");

        if (File.Exists(keyPath))
        {
            var protectedKey = File.ReadAllBytes(keyPath);
            return ProtectedData.Unprotect(protectedKey, null, DataProtectionScope.CurrentUser);
        }

        var key = new byte[32];
        RandomNumberGenerator.Fill(key);

        var dir = Path.GetDirectoryName(keyPath)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var encrypted = ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(keyPath, encrypted);
#pragma warning restore CA1416

        return key;
    }
}

/// <summary>
/// Tokenization service implementation
/// </summary>
public class TokenizationService : ITokenizationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IKeyManagementService _keyManagement;
    private readonly ILogger<TokenizationService> _logger;
    private const string TokenPrefix = "TKN_";

    public TokenizationService(IServiceProvider serviceProvider, IKeyManagementService keyManagement, ILogger<TokenizationService> logger)
    {
        _serviceProvider = serviceProvider;
        _keyManagement = keyManagement;
        _logger = logger;
    }

    public async Task<string> TokenizeAsync(string value, string fieldType, string? entityType = null, 
        int? entityId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        // Generate unique token
        var tokenBytes = new byte[24];
        RandomNumberGenerator.Fill(tokenBytes);
        var token = TokenPrefix + Convert.ToBase64String(tokenBytes).Replace("/", "_").Replace("+", "-")[..32];

        // Encrypt the actual value
        var key = await _keyManagement.GetActiveKeyAsync("Tokenization", cancellationToken);
        var encryptedValue = EncryptValue(value, key);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var entry = new TokenVaultEntry
        {
            Token = token,
            EncryptedValue = encryptedValue,
            FieldType = fieldType,
            EntityType = entityType,
            EntityId = entityId,
            KeyId = "active" // Would use actual key ID in production
        };

        dbContext.Set<TokenVaultEntry>().Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return token;
    }

    public async Task<string> DetokenizeAsync(string token, CancellationToken cancellationToken = default)
    {
        if (!IsToken(token)) return token;

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var entry = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                dbContext.Set<TokenVaultEntry>().Where(t => t.Token == token),
                cancellationToken);

        if (entry == null)
            throw new InvalidOperationException($"Token not found: {token}");

        var key = await _keyManagement.GetActiveKeyAsync("Tokenization", cancellationToken);
        return DecryptValue(entry.EncryptedValue, key);
    }

    public bool IsToken(string value)
    {
        return !string.IsNullOrEmpty(value) && value.StartsWith(TokenPrefix);
    }

    public async Task<string> GetMaskedValueAsync(string token, CancellationToken cancellationToken = default)
    {
        if (!IsToken(token)) return "****";

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Data.INCISDbContext>();

        var entry = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(
                dbContext.Set<TokenVaultEntry>().Where(t => t.Token == token),
                cancellationToken);

        if (entry == null) return "****";

        var key = await _keyManagement.GetActiveKeyAsync("Tokenization", cancellationToken);
        var value = DecryptValue(entry.EncryptedValue, key);

        return entry.FieldType switch
        {
            "AFM" => value.Length >= 3 ? "******" + value[^3..] : "***",
            "Phone" => value.Length >= 4 ? "******" + value[^4..] : "****",
            "Email" => MaskEmail(value),
            _ => "****" + (value.Length > 4 ? value[^4..] : "")
        };
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return "****@****.***";
        var name = parts[0].Length > 2 ? parts[0][..2] + "***" : "***";
        return $"{name}@{parts[1]}";
    }

    private static string EncryptValue(string value, byte[] key)
    {
        var iv = new byte[12];
        RandomNumberGenerator.Fill(iv);
        using var aes = new AesGcm(key, 16);
        var plainBytes = Encoding.UTF8.GetBytes(value);
        var cipherText = new byte[plainBytes.Length];
        var tag = new byte[16];
        aes.Encrypt(iv, plainBytes, cipherText, tag);

        var result = new byte[iv.Length + tag.Length + cipherText.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(tag, 0, result, iv.Length, tag.Length);
        Buffer.BlockCopy(cipherText, 0, result, iv.Length + tag.Length, cipherText.Length);
        return Convert.ToBase64String(result);
    }

    private static string DecryptValue(string encryptedBase64, byte[] key)
    {
        var combined = Convert.FromBase64String(encryptedBase64);
        var iv = new byte[12];
        var tag = new byte[16];
        var cipherText = new byte[combined.Length - 28];

        Buffer.BlockCopy(combined, 0, iv, 0, 12);
        Buffer.BlockCopy(combined, 12, tag, 0, 16);
        Buffer.BlockCopy(combined, 28, cipherText, 0, cipherText.Length);

        using var aes = new AesGcm(key, 16);
        var plainBytes = new byte[cipherText.Length];
        aes.Decrypt(iv, cipherText, tag, plainBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
