using StrictSharp.Analyzers.V5;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class V5CostComparisonTests
{
    [Fact]
    public void ComparesKnownStructuralComplexityWithoutPercentage()
    {
        var left = new CostEvidence(
            CostDimension.Complexity,
            ComplexityClassification.Quadratic,
            KnownCount.Unknown,
            KnownCount.Unknown,
            PayloadEstimate.Unknown,
            "left",
            "known",
            "runtime unknown");
        var right = new CostEvidence(
            CostDimension.Complexity,
            ComplexityClassification.Linear,
            KnownCount.Unknown,
            KnownCount.Unknown,
            PayloadEstimate.Unknown,
            "right",
            "known",
            "runtime unknown");

        var comparison = CostComparison.Compare(left, right);

        Assert.True(comparison.IsComparable);
        Assert.Equal("O(n²) -> O(n)", comparison.ComplexityChange);
        Assert.DoesNotContain("%", comparison.Limitations, StringComparison.Ordinal);
    }

    [Fact]
    public void DoesNotCompareDifferentDimensions()
    {
        var left = CostEvidence.Unknown(CostDimension.Io, "left", "unknown");
        var right = CostEvidence.Unknown(CostDimension.Concurrency, "right", "unknown");

        var comparison = CostComparison.Compare(left, right);

        Assert.False(comparison.IsComparable);
        Assert.Contains("desconhecida", comparison.ComplexityChange, StringComparison.Ordinal);
    }
}
