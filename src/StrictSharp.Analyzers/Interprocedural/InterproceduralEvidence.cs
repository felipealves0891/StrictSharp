using Microsoft.CodeAnalysis;

namespace StrictSharp.Analyzers.Interprocedural;

public readonly struct InterproceduralEvidence
{
    public InterproceduralEvidence(
        EvidenceKind kind,
        string dimension,
        IMethodSymbol caller,
        IMethodSymbol? target,
        Location location,
        Location operationLocation)
    {
        Kind = kind;
        Dimension = dimension;
        Caller = caller;
        Target = target;
        Location = location;
        OperationLocation = operationLocation;
    }

    public EvidenceKind Kind { get; }
    public string Dimension { get; }
    public IMethodSymbol Caller { get; }
    public IMethodSymbol? Target { get; }
    public Location Location { get; }
    public Location OperationLocation { get; }
}
