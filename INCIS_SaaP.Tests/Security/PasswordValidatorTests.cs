using FluentAssertions;
using INCIS_SaaP.Models;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace INCIS_SaaP.Tests.Security;

/// <summary>
/// Tests for the CustomPasswordValidator (weak/sequential/repeated password rules).
/// </summary>
public class PasswordValidatorTests
{
    private readonly CustomPasswordValidator<ApplicationUser> _validator = new();

    private async Task<IdentityResult> Validate(string? password)
        => await _validator.ValidateAsync(null!, null!, password);

    [Fact]
    public async Task Rejects_NullPassword()
    {
        var result = await Validate(null);
        result.Succeeded.Should().BeFalse();
    }

    [Theory]
    [InlineData("Password123!")]
    [InlineData("Welcome1!")]
    [InlineData("Qwerty123!")]
    public async Task Rejects_CommonPasswords(string password)
    {
        var result = await Validate(password);
        result.Succeeded.Should().BeFalse();
    }

    [Theory]
    [InlineData("Ab123xyz")]   // contains "123"
    [InlineData("Testabc99")]  // contains "abc"
    [InlineData("Zqwe887x")]   // contains "qwe"
    public async Task Rejects_SequentialCharacters(string password)
    {
        var result = await Validate(password);
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Rejects_RepeatedCharacters()
    {
        var result = await Validate("Xaaaa9$k");
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Accepts_StrongPassword()
    {
        var result = await Validate("Xk9$mQ2&vLp!");
        result.Succeeded.Should().BeTrue();
    }
}
