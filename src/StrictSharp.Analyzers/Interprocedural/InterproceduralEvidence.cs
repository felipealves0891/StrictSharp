using Microsoft.CodeAnalysis;
using StrictSharp.Analyzers.V5;

namespace StrictSharp.Analyzers.Interprocedural;

public readonly struct InterproceduralEvidence
{
    public InterproceduralEvidence(
        EvidenceKind kind,
        string dimension,
        IMethodSymbol caller,
        IMethodSymbol? target,
        Location location,
        Location operationLocation,
        KnownCount count,
        KnownCount multiplier)
    {
        Kind = kind;
        Dimension = dimension;
        Caller = caller;
        Target = target;
        Location = location;
        OperationLocation = operationLocation;
        Count = count;
        Multiplier = multiplier;
    }

    public EvidenceKind Kind { get; }
    public string Dimension { get; }
    public IMethodSymbol Caller { get; }
    public IMethodSymbol? Target { get; }
    public Location Location { get; }
    public Location OperationLocation { get; }
    public KnownCount Count { get; }
    public KnownCount Multiplier { get; }
}
