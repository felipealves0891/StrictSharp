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

    [Fact]
    public void ComparesKnownCountsWhenComplexityIsUnknown()
    {
        var left = new CostEvidence(CostDimension.Memory, ComplexityClassification.Unknown, KnownCount.Exact(2), KnownCount.Exact(2), PayloadEstimate.Unknown, "left", "known", "unknown");
        var right = new CostEvidence(CostDimension.Memory, ComplexityClassification.Unknown, KnownCount.Exact(4), KnownCount.Exact(4), PayloadEstimate.Unknown, "right", "known", "unknown");

        var comparison = CostComparison.Compare(left, right);

        Assert.True(comparison.IsComparable);
        Assert.Equal("contagem estrutural 2 -> 4", comparison.WorkChange);
        Assert.Contains("desconhecida", comparison.ComplexityChange, StringComparison.Ordinal);
    }

    [Fact]
    public void KeepsUnknownWorkWhenCountsAreUnavailable()
    {
        var left = CostEvidence.Unknown(CostDimension.Memory, "left", "unknown");
        var right = CostEvidence.Unknown(CostDimension.Memory, "right", "unknown");

        var comparison = CostComparison.Compare(left, right);

        Assert.False(comparison.IsComparable);
        Assert.Contains("contagem estrutural desconhecida", comparison.WorkChange, StringComparison.Ordinal);
    }
}
