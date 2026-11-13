using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Data;
using INCIS_SaaP.Models;
using INCIS_SaaP.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace INCIS_SaaP.Tests.Controllers;

/// <summary>
/// Unit tests for the secure ApiController (public + data endpoints).
/// </summary>
public class ApiControllerTests : IDisposable
{
    private readonly INCISDbContext _context;
    private readonly ApiController _controller;

    public ApiControllerTests()
    {
        _context = TestDbContextFactory.Create();
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        _controller = new ApiController(_context, config, NullLogger<ApiController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public void Health_ReturnsOk() => _controller.Health().Should().BeOfType<OkObjectResult>();

    [Fact]
    public void GetSecurityInfo_ReturnsOk() => _controller.GetSecurityInfo().Should().BeOfType<OkObjectResult>();

    [Fact]
    public async Task GetCurrentUser_WithoutClaim_ReturnsUnauthorized()
    {
        (await _controller.GetCurrentUser()).Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetCurrentUser_WithValidClaim_ReturnsOk_WithMaskedPhone()
    {
        const string userId = "user-abc-123";
        _context.Users.Add(new ApplicationUser
        {
            Id = userId,
            Email = "user@incis.local",
            FullName = "Test User",
            IsActive = true,
            PhoneNumber = "6971234567"
        });
        await _context.SaveChangesAsync();

        var identity = new ClaimsIdentity(new[] { new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, userId) }, "test");
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        (await _controller.GetCurrentUser()).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCurrentUser_WithClaimButMissingUser_ReturnsNotFound()
    {
        var identity = new ClaimsIdentity(new[] { new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, "ghost") }, "test");
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        (await _controller.GetCurrentUser()).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void EncryptData_EmptyData_ReturnsBadRequest()
    {
        _controller.EncryptData(new EncryptRequest { Data = "" }).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void EncryptData_ValidData_ReturnsOk()
    {
        _controller.EncryptData(new EncryptRequest { Data = "secret" }).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void DecryptData_EmptyData_ReturnsBadRequest()
    {
        _controller.DecryptData(new DecryptRequest { Data = "" }).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void DecryptData_InvalidData_ReturnsBadRequest()
    {
        _controller.DecryptData(new DecryptRequest { Data = "not-valid" }).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetFormulas_ReturnsOk()
    {
        _context.Formulas.Add(new Formula { FormulaCode = "F-100" });
        await _context.SaveChangesAsync();

        (await _controller.GetFormulas()).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SearchFormulas_EmptyQuery_ReturnsBadRequest()
    {
        (await _controller.SearchFormulas("", 10)).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SearchFormulas_WithQuery_ReturnsOk()
    {
        _context.Formulas.Add(new Formula { FormulaCode = "ABC-1", FormulaName = "Cream" });
        await _context.SaveChangesAsync();

        (await _controller.SearchFormulas("Cream", 10)).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFormula_Unknown_ReturnsNotFound()
    {
        (await _controller.GetFormula(999)).Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetFormulaIngredients_Unknown_ReturnsNotFound()
    {
        (await _controller.GetFormulaIngredients(999)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetFormulaAllergens_ReturnsOk()
    {
        (await _controller.GetFormulaAllergens(1)).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFormulaSafety_ReturnsOk()
    {
        (await _controller.GetFormulaSafety(1)).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCertifications_ReturnsOk()
    {
        (await _controller.GetCertifications()).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GenerateToken_DisabledByDefault_Returns501()
    {
        // Ασφάλεια: το demo token endpoint είναι απενεργοποιημένο εξ ορισμού (δεν είναι πραγματικός μηχανισμός).
        var result = await _controller.GenerateToken(new TokenRequest { Email = "x@y.z", Password = "pw" });
        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(501);
    }

    private ApiController ControllerWithDemoTokenEnabled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Api:DemoToken:Enabled"] = "true" })
            .Build();
        return new ApiController(_context, config, NullLogger<ApiController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task GenerateToken_MissingCredentials_ReturnsBadRequest()
    {
        var controller = ControllerWithDemoTokenEnabled();
        (await controller.GenerateToken(new TokenRequest { Email = "", Password = "" }))
            .Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GenerateToken_UnknownUser_ReturnsUnauthorized()
    {
        var controller = ControllerWithDemoTokenEnabled();
        (await controller.GenerateToken(new TokenRequest { Email = "nobody@x.com", Password = "pw" }))
            .Should().BeOfType<UnauthorizedObjectResult>();
    }

    public void Dispose() => _context.Dispose();
}
