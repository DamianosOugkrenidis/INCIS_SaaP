using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Data;
using INCIS_SaaP.Services;
using INCIS_SaaP.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace INCIS_SaaP.Tests.Controllers;

/// <summary>
/// Unit tests for ApiFeatureFlagsController.
/// </summary>
public class ApiFeatureFlagsControllerTests : IDisposable
{
    private readonly INCISDbContext _context;
    private readonly Mock<IFeatureFlagService> _service = new();
    private readonly ApiFeatureFlagsController _controller;

    public ApiFeatureFlagsControllerTests()
    {
        _context = TestDbContextFactory.Create();
        _controller = new ApiFeatureFlagsController(_service.Object, _context, NullLogger<ApiFeatureFlagsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public void GetAll_ReturnsOk()
    {
        _service.Setup(s => s.GetAll()).Returns(new Dictionary<string, bool> { ["x"] = true });
        _controller.GetAll().Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void IsEnabled_ReturnsOk()
    {
        _service.Setup(s => s.IsEnabled("x")).Returns(true);
        _controller.IsEnabled("x").Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDetails_ReturnsOk_WithSeededFlags()
    {
        _context.FeatureFlags.Add(new FeatureFlag { Key = "k1", IsEnabled = true, Description = "d" });
        await _context.SaveChangesAsync();

        (await _controller.GetDetails()).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void SetFlag_ReturnsOk()
    {
        var result = _controller.SetFlag("feature", new SetFeatureFlagRequest { IsEnabled = true });
        result.Should().BeOfType<OkObjectResult>();
        _service.Verify(s => s.SetEnabled("feature", true), Times.Once);
    }

    [Fact]
    public async Task CreateFlag_EmptyKey_ReturnsBadRequest()
    {
        var result = await _controller.CreateFlag(new CreateFeatureFlagRequest { Key = "" });
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateFlag_NewKey_ReturnsCreated()
    {
        var result = await _controller.CreateFlag(new CreateFeatureFlagRequest { Key = "brand-new", IsEnabled = true });
        result.Should().BeOfType<CreatedAtActionResult>();
        _context.FeatureFlags.Should().ContainSingle(f => f.Key == "brand-new");
    }

    [Fact]
    public async Task CreateFlag_Duplicate_ReturnsConflict()
    {
        _context.FeatureFlags.Add(new FeatureFlag { Key = "dup", IsEnabled = false });
        await _context.SaveChangesAsync();

        var result = await _controller.CreateFlag(new CreateFeatureFlagRequest { Key = "dup" });
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteFlag_Existing_ReturnsOk()
    {
        _context.FeatureFlags.Add(new FeatureFlag { Key = "todelete" });
        await _context.SaveChangesAsync();

        var result = await _controller.DeleteFlag("todelete");
        result.Should().BeOfType<OkObjectResult>();
        _context.FeatureFlags.Should().NotContain(f => f.Key == "todelete");
    }

    [Fact]
    public async Task DeleteFlag_Missing_ReturnsNotFound()
    {
        (await _controller.DeleteFlag("nope")).Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void BulkUpdate_ReturnsOk()
    {
        var flags = new Dictionary<string, bool> { ["a"] = true, ["b"] = false };
        _controller.BulkUpdate(flags).Should().BeOfType<OkObjectResult>();
        _service.Verify(s => s.SetEnabled(It.IsAny<string>(), It.IsAny<bool>()), Times.Exactly(2));
    }

    public void Dispose() => _context.Dispose();
}
