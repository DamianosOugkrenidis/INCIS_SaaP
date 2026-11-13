using FluentAssertions;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// Tests for the UserManager-backed flows of TwoFactorService (enable/verify/disable).
/// </summary>
public class TwoFactorServiceTests
{
    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task EnableTwoFactorAsync_UnknownUser_ReturnsError()
    {
        var um = MockUserManager();
        um.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);
        var service = new TwoFactorService(um.Object, NullLogger<TwoFactorService>.Instance);

        var (success, error, qr, secret) = await service.EnableTwoFactorAsync("missing");

        success.Should().BeFalse();
        error.Should().Be("User not found");
        qr.Should().BeNull();
        secret.Should().BeNull();
    }

    [Fact]
    public async Task EnableTwoFactorAsync_ValidUser_ReturnsQrAndSecret()
    {
        var user = new ApplicationUser { Id = "u1", Email = "user@incis.local" };
        var um = MockUserManager();
        um.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        um.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        var service = new TwoFactorService(um.Object, NullLogger<TwoFactorService>.Instance);

        var (success, error, qr, secret) = await service.EnableTwoFactorAsync("u1");

        success.Should().BeTrue();
        error.Should().BeNull();
        qr.Should().NotBeNullOrEmpty();      // base64 PNG QR image
        secret.Should().NotBeNullOrEmpty();
        user.TwoFactorSecretKey.Should().Be(secret);
    }

    [Fact]
    public async Task EnableTwoFactorAsync_UpdateFails_ReturnsError()
    {
        var um = MockUserManager();
        um.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(new ApplicationUser { Id = "u1", Email = "e@e.com" });
        um.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "fail" }));
        var service = new TwoFactorService(um.Object, NullLogger<TwoFactorService>.Instance);

        var (success, error, _, _) = await service.EnableTwoFactorAsync("u1");

        success.Should().BeFalse();
        error.Should().Be("Failed to update user");
    }

    [Fact]
    public async Task VerifyTwoFactorCodeAsync_No2faEnabled_ReturnsError()
    {
        var um = MockUserManager();
        um.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(new ApplicationUser { Id = "u1", TwoFactorSecretKey = null });
        var service = new TwoFactorService(um.Object, NullLogger<TwoFactorService>.Instance);

        var (success, error) = await service.VerifyTwoFactorCodeAsync("u1", "123456");

        success.Should().BeFalse();
        error.Should().Be("2FA not enabled");
    }

    [Fact]
    public async Task VerifyTwoFactorCodeAsync_ValidCode_EnablesAndSucceeds()
    {
        const string secret = "JBSWY3DPEHPK3PXP";
        var user = new ApplicationUser { Id = "u1", TwoFactorSecretKey = secret };
        var um = MockUserManager();
        um.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        um.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        var service = new TwoFactorService(um.Object, NullLogger<TwoFactorService>.Instance);

        var totp = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(secret));
        var (success, error) = await service.VerifyTwoFactorCodeAsync("u1", totp.ComputeTotp());

        success.Should().BeTrue();
        error.Should().BeNull();
        user.TwoFactorEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyTwoFactorCodeAsync_InvalidCode_ReturnsError()
    {
        var user = new ApplicationUser { Id = "u1", TwoFactorSecretKey = "JBSWY3DPEHPK3PXP" };
        var um = MockUserManager();
        um.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        var service = new TwoFactorService(um.Object, NullLogger<TwoFactorService>.Instance);

        var (success, error) = await service.VerifyTwoFactorCodeAsync("u1", "000000");

        success.Should().BeFalse();
        error.Should().Be("Invalid code");
    }

    [Fact]
    public async Task DisableTwoFactorAsync_ValidUser_Succeeds()
    {
        var user = new ApplicationUser { Id = "u1", TwoFactorEnabled = true, TwoFactorSecretKey = "abc" };
        var um = MockUserManager();
        um.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        um.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        var service = new TwoFactorService(um.Object, NullLogger<TwoFactorService>.Instance);

        var (success, error) = await service.DisableTwoFactorAsync("u1");

        success.Should().BeTrue();
        error.Should().BeNull();
        user.TwoFactorEnabled.Should().BeFalse();
        user.TwoFactorSecretKey.Should().BeNull();
    }

    [Fact]
    public async Task DisableTwoFactorAsync_UnknownUser_ReturnsError()
    {
        var um = MockUserManager();
        um.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);
        var service = new TwoFactorService(um.Object, NullLogger<TwoFactorService>.Instance);

        var (success, error) = await service.DisableTwoFactorAsync("missing");

        success.Should().BeFalse();
        error.Should().Be("User not found");
    }
}
