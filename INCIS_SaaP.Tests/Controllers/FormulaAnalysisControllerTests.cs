using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using INCIS_SaaP.Controllers;
using INCIS_SaaP.Data;
using INCIS_SaaP.ML.FormulaIntelligence;
using INCIS_SaaP.Tests.Helpers;
using Moq;
using Xunit;

namespace INCIS_SaaP.Tests.Controllers;

/// <summary>
/// Unit tests for FormulaAnalysisController (not-found paths + pure calculation endpoints).
/// </summary>
public class FormulaAnalysisControllerTests : IDisposable
{
    private readonly INCISDbContext _context;
    private readonly FormulaAnalysisController _controller;

    public FormulaAnalysisControllerTests()
    {
        _context = TestDbContextFactory.Create();
        _controller = new FormulaAnalysisController(
            _context,
            Mock.Of<IFormulaIntelligenceService>(),
            Mock.Of<IFormulaSuccessModel>());
    }

    [Fact]
    public async Task PredictSuccess_UnknownFormula_ReturnsNotFound()
    {
        var result = await _controller.PredictSuccess(999, CancellationToken.None);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AnalyzeFormula_UnknownFormula_ReturnsNotFound()
    {
        var result = await _controller.AnalyzeFormula(999);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void GetEUAllergens_ReturnsOk()
    {
        _controller.GetEUAllergens().Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void GetHeavyMetalLimits_ReturnsOk()
    {
        _controller.GetHeavyMetalLimits().Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void CalculatePrice_ReturnsOk_WithComputedValues()
    {
        var request = new PriceCalculationRequest
        {
            CostPerUnit = 10m,
            MarginPercentage = 30m,
            CountryCode = "GR"
        };

        var result = _controller.CalculatePrice(request);
        result.Should().BeOfType<OkObjectResult>();
    }

    public void Dispose() => _context.Dispose();
}
