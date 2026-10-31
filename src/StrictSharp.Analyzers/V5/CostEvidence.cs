using System.Collections.Immutable;

namespace StrictSharp.Analyzers.V5;

public readonly struct CostEvidence
{
    public CostEvidence(
        CostDimension dimension,
        ComplexityClassification complexity,
        KnownCount count,
        KnownCount multiplier,
        PayloadEstimate payload,
        string explanation,
        string assumptions,
        string limitations)
    {
        Dimension = dimension;
        Complexity = complexity;
        Count = count;
        Multiplier = multiplier;
        Payload = payload;
        Explanation = explanation;
        Assumptions = assumptions;
        Limitations = limitations;
    }

    public CostDimension Dimension { get; }
    public ComplexityClassification Complexity { get; }
    public KnownCount Count { get; }
    public KnownCount Multiplier { get; }
    public PayloadEstimate Payload { get; }
    public string Explanation { get; }
    public string Assumptions { get; }
    public string Limitations { get; }
    public bool HasUnknownBoundary => !Complexity.IsKnown || !Count.IsKnown || !Multiplier.IsKnown || !Payload.IsKnown;

    public static CostEvidence Unknown(CostDimension dimension, string explanation, string limitation)
    {
        return new CostEvidence(
            dimension,
            ComplexityClassification.Unknown,
            KnownCount.Unknown,
            KnownCount.Unknown,
            PayloadEstimate.Unknown,
            explanation,
            "A forma, o limite ou o alvo não foi resolvido estaticamente.",
            limitation);
    }
}

public readonly struct CostEvidenceSet
{
    public CostEvidenceSet(ImmutableArray<CostEvidence> items)
    {
        Items = items;
    }

    public ImmutableArray<CostEvidence> Items { get; }
    public bool IsEmpty => Items.IsDefaultOrEmpty;

    public static CostEvidenceSet Empty => new(ImmutableArray<CostEvidence>.Empty);

    public static CostEvidenceSet Create(ImmutableArray<CostEvidence> items)
    {
        return new CostEvidenceSet(items.IsDefault ? ImmutableArray<CostEvidence>.Empty : items);
    }
}
