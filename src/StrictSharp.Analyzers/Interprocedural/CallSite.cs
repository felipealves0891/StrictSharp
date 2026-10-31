using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StrictSharp.Analyzers.Interprocedural;

public readonly struct CallSite
{
    public CallSite(
        IMethodSymbol caller,
        IMethodSymbol? target,
        Location location,
        CallTargetState state,
        CallFlowKind flow,
        InvocationExpressionSyntax invocation)
    {
        Caller = caller;
        Target = target;
        Location = location;
        State = state;
        Flow = flow;
        Invocation = invocation;
    }

    public IMethodSymbol Caller { get; }
    public IMethodSymbol? Target { get; }
    public Location Location { get; }
    public CallTargetState State { get; }
    public CallFlowKind Flow { get; }
    public InvocationExpressionSyntax Invocation { get; }
}
