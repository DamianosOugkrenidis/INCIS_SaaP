using FluentAssertions;
using INCIS_SaaP.Infrastructure.Privacy;
using Xunit;

namespace INCIS_SaaP.Tests.Infrastructure;

/// <summary>
/// Tests for the DifferentialPrivacy helper (Laplacian noise).
/// </summary>
public class DifferentialPrivacyTests
{
    [Fact]
    public void AddNoiseToCount_NeverReturnsNegative()
    {
        for (var i = 0; i < 50; i++)
        {
            DifferentialPrivacy.AddNoiseToCount(5).Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public void AddNoiseToCount_StaysNearOriginal_OnAverage()
    {
        const int original = 1000;
        var samples = Enumerable.Range(0, 200)
            .Select(_ => DifferentialPrivacy.AddNoiseToCount(original, epsilon: 1.0))
            .ToList();

        var average = samples.Average();
        // With epsilon=1 and sensitivity=1 the noise is small relative to 1000
        average.Should().BeInRange(original - 50, original + 50);
    }

    [Fact]
    public void AddLaplacianNoise_ReturnsFiniteValue()
    {
        var result = DifferentialPrivacy.AddLaplacianNoise(42.0, sensitivity: 1.0, epsilon: 0.5);
        double.IsFinite(result).Should().BeTrue();
    }
}
