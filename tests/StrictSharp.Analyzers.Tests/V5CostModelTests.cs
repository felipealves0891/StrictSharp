using StrictSharp.Analyzers.V5;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class V5CostModelTests
{
    [Fact]
    public void KnownPayloadDoesNotIncludeObjectOverhead()
    {
        var payload = PayloadEstimate.Known(100_000);

        Assert.True(payload.IsKnown);
        Assert.Equal(100_000, payload.Bytes);
        Assert.False(payload.IncludesObjectOverhead);
    }

    [Fact]
    public void UnknownEvidenceKeepsEveryUnresolvedDimensionExplicit()
    {
        var evidence = CostEvidence.Unknown(CostDimension.Io, "external call", "latency depends on runtime");

        Assert.True(evidence.HasUnknownBoundary);
        Assert.False(evidence.Complexity.IsKnown);
        Assert.False(evidence.Count.IsKnown);
        Assert.False(evidence.Multiplier.IsKnown);
        Assert.False(evidence.Payload.IsKnown);
        Assert.Equal("latency depends on runtime", evidence.Limitations);
    }

    [Theory]
    [InlineData(ComplexityKind.Constant, "O(1)")]
    [InlineData(ComplexityKind.Linear, "O(n)")]
    [InlineData(ComplexityKind.Linearithmic, "O(n log n)")]
    [InlineData(ComplexityKind.Quadratic, "O(n²)")]
    [InlineData(ComplexityKind.Unknown, "desconhecida")]
    public void ComplexityLabelsAreDeterministic(ComplexityKind kind, string expected)
    {
        var classification = kind switch
        {
            ComplexityKind.Constant => ComplexityClassification.Constant,
            ComplexityKind.Linear => ComplexityClassification.Linear,
            ComplexityKind.Linearithmic => ComplexityClassification.Linearithmic,
            ComplexityKind.Quadratic => ComplexityClassification.Quadratic,
            _ => ComplexityClassification.Unknown
        };

        Assert.Equal(expected, classification.Label);
    }

    [Fact]
    public void CostEvidenceSetNormalizesDefaultArray()
    {
        var set = CostEvidenceSet.Create(default);

        Assert.True(set.IsEmpty);
        Assert.Empty(set.Items);
    }

    [Fact]
    public void RejectsNegativeCountsAndPayloads()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => KnownCount.Exact(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => KnownCount.Maximum(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PayloadEstimate.Known(-1));
    }

    [Fact]
    public void RepresentsMaximumCount()
    {
        var count = KnownCount.Maximum(5);

        Assert.True(count.IsKnown);
        Assert.True(count.IsMaximum);
        Assert.Equal(5, count.Value);
    }
}
