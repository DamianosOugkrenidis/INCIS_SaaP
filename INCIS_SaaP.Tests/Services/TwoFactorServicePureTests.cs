using FluentAssertions;
using INCIS_SaaP.Models;
using INCIS_SaaP.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace INCIS_SaaP.Tests.Services;

/// <summary>
/// Tests for the pure (non-DB) methods of TwoFactorService: code generation and TOTP validation.
/// </summary>
public class TwoFactorServicePureTests
{
    private static TwoFactorService CreateService()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        return new TwoFactorService(userManager.Object, NullLogger<TwoFactorService>.Instance);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(4)]
    [InlineData(8)]
    public void GenerateVerificationCode_ReturnsDigitsOfRequestedLength(int length)
    {
        var service = CreateService();

        var code = service.GenerateVerificationCode(length);

        code.Should().HaveLength(length);
        code.Should().MatchRegex("^[0-9]+$");
    }

    [Fact]
    public void ValidateCode_ReturnsTrue_ForCurrentTotp()
    {
        var service = CreateService();
        const string secret = "JBSWY3DPEHPK3PXP"; // valid Base32

        var totp = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(secret));
        var currentCode = totp.ComputeTotp();

        service.ValidateCode(secret, currentCode).Should().BeTrue();
    }

    [Fact]
    public void ValidateCode_ReturnsFalse_ForNullOrEmptySecret()
    {
        var service = CreateService();

        service.ValidateCode(null, "123456").Should().BeFalse();
        service.ValidateCode("", "123456").Should().BeFalse();
    }

    [Fact]
    public void ValidateCode_ReturnsFalse_ForEmptyCode()
    {
        var service = CreateService();
        service.ValidateCode("JBSWY3DPEHPK3PXP", "").Should().BeFalse();
    }

    [Fact]
    public async Task SendTwoFactorCodeByEmailAsync_ReturnsTrue()
    {
        var service = CreateService();
        (await service.SendTwoFactorCodeByEmailAsync("a@b.com", "123456")).Should().BeTrue();
    }
}
