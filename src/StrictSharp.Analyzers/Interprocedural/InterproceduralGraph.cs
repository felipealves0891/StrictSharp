using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace StrictSharp.Analyzers.Interprocedural;

public sealed class InterproceduralGraph
{
    internal InterproceduralGraph(
        IMethodSymbol root,
        ImmutableArray<CallSite> callSites,
        bool containsCycle)
    {
        Root = root;
        CallSites = callSites;
        ContainsCycle = containsCycle;
    }

    public IMethodSymbol Root { get; }
    public ImmutableArray<CallSite> CallSites { get; }
    public bool ContainsCycle { get; }
}
