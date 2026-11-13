using FluentAssertions;
using INCIS_SaaP.Services;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// Tests for the static EncryptionService (AES-GCM, PBKDF2, masking, HMAC).
/// </summary>
public class EncryptionServiceTests
{
    [Fact]
    public void Encrypt_Then_Decrypt_ReturnsOriginal()
    {
        const string plain = "Sensitive-Data-123!";

        var cipher = EncryptionService.Encrypt(plain);
        cipher.Should().NotBeNullOrEmpty();
        cipher.Should().NotBe(plain);

        var roundTrip = EncryptionService.Decrypt(cipher);
        roundTrip.Should().Be(plain);
    }

    [Fact]
    public void Encrypt_EmptyInput_ReturnsEmpty()
    {
        EncryptionService.Encrypt("").Should().BeEmpty();
        EncryptionService.Decrypt("").Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_InvalidInput_ReturnsEmpty()
    {
        EncryptionService.Decrypt("not-valid-base64!!!").Should().BeEmpty();
    }

    [Theory]
    [InlineData("6971234567", "****4567")]
    [InlineData("123", "****")]
    [InlineData("", "****")]
    public void MaskPhone_MasksAllButLastFour(string input, string expected)
    {
        EncryptionService.MaskPhone(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("123456789", "***789")]
    [InlineData("12", "***")]
    public void MaskAFM_MasksAllButLastThree(string input, string expected)
    {
        EncryptionService.MaskAFM(input).Should().Be(expected);
    }

    [Fact]
    public void HashPassword_And_VerifyPassword_Roundtrip()
    {
        const string password = "Str0ng!Passw0rd#";

        var hash = EncryptionService.HashPassword(password);
        hash.Should().NotBeNullOrEmpty();

        EncryptionService.VerifyPassword(password, hash).Should().BeTrue();
        EncryptionService.VerifyPassword("wrong-password", hash).Should().BeFalse();
    }

    [Fact]
    public void HashPassword_ProducesDifferentHashes_ForSamePassword()
    {
        // Random salt => different hashes each time
        var h1 = EncryptionService.HashPassword("same");
        var h2 = EncryptionService.HashPassword("same");
        h1.Should().NotBe(h2);
    }

    [Fact]
    public void VerifyPassword_WithEmptyInputs_ReturnsFalse()
    {
        EncryptionService.VerifyPassword("", "hash").Should().BeFalse();
        EncryptionService.VerifyPassword("pwd", "").Should().BeFalse();
    }

    [Fact]
    public void GenerateSecureToken_ReturnsUniqueNonEmptyTokens()
    {
        var t1 = EncryptionService.GenerateSecureToken();
        var t2 = EncryptionService.GenerateSecureToken();

        t1.Should().NotBeNullOrEmpty();
        t1.Should().NotBe(t2);
    }

    [Fact]
    public void ComputeHmacSha256_IsDeterministic_ForSameSecret()
    {
        var a = EncryptionService.ComputeHmacSha256("message", "secret");
        var b = EncryptionService.ComputeHmacSha256("message", "secret");
        var c = EncryptionService.ComputeHmacSha256("message", "other-secret");

        a.Should().Be(b);
        a.Should().NotBe(c);
    }
}
