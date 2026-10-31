using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace StrictSharp.Analyzers.Interprocedural;

public static class InterproceduralEvidenceAnalyzer
{
    public static ImmutableArray<InterproceduralEvidence> Analyze(
        Compilation compilation,
        InterproceduralGraph graph,
        CancellationToken cancellationToken)
    {
        var evidence = ImmutableArray.CreateBuilder<InterproceduralEvidence>();
        var callCounts = new Dictionary<(IMethodSymbol Caller, IMethodSymbol Target), int>(SymbolPairComparer.Instance);

        foreach (var callSite in graph.CallSites)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (callSite.Flow == CallFlowKind.Blocking && IsInsideLoop(callSite.Invocation))
                evidence.Add(new InterproceduralEvidence(
                    EvidenceKind.BlockingCall,
                    "Concurrency/I/O",
                    callSite.Caller,
                    callSite.Target,
                    callSite.Location,
                    callSite.Location));

            if (callSite.State != CallTargetState.Known || callSite.Target is null)
                continue;

            var key = (callSite.Caller, callSite.Target);
            if (!IsInsideLoop(callSite.Invocation))
                continue;

            callCounts[key] = callCounts.TryGetValue(key, out var count) ? count + 1 : 1;

            foreach (var operation in FindCostlyOperations(compilation, callSite.Target, cancellationToken))
            {
                var dimension = operation.Kind == EvidenceKind.Materialization ? "Memory/LINQ" : "Memory/CPU";
                if (IsInsideLoop(callSite.Invocation))
                    evidence.Add(new InterproceduralEvidence(
                        EvidenceKind.RepeatedCall,
                        dimension,
                        callSite.Caller,
                        callSite.Target,
                        callSite.Location,
                        operation.Location));

                evidence.Add(new InterproceduralEvidence(
                    operation.Kind,
                    dimension,
                    callSite.Caller,
                    callSite.Target,
                    callSite.Location,
                    operation.Location));
            }
        }

        foreach (var callCount in callCounts)
        {
            if (callCount.Value < 2)
                continue;

            var duplicate = graph.CallSites.First(site =>
                SymbolEqualityComparer.Default.Equals(site.Caller, callCount.Key.Caller)
                && SymbolEqualityComparer.Default.Equals(site.Target, callCount.Key.Target));
            evidence.Add(new InterproceduralEvidence(
                EvidenceKind.RedundantWork,
                "CPU/Memory",
                duplicate.Caller,
                duplicate.Target,
                duplicate.Location,
                duplicate.Location));
        }

        if (graph.ContainsCycle && graph.CallSites.Length > 0)
        {
            var recursive = graph.CallSites.First();
            evidence.Add(new InterproceduralEvidence(
                EvidenceKind.RecursiveCall,
                "CPU/Stack",
                recursive.Caller,
                recursive.Target,
                recursive.Location,
                recursive.Location));
        }

        return evidence.ToImmutable();
    }

    private static ImmutableArray<CostlyOperation> FindCostlyOperations(
        Compilation compilation,
        IMethodSymbol method,
        CancellationToken cancellationToken)
    {
        var operations = ImmutableArray.CreateBuilder<CostlyOperation>();
        foreach (var syntaxReference in method.DeclaringSyntaxReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (syntaxReference.GetSyntax(cancellationToken) is not SyntaxNode declaration)
                continue;

            foreach (var creation in declaration.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                operations.Add(new CostlyOperation(EvidenceKind.Allocation, creation.GetLocation()));

            foreach (var creation in declaration.DescendantNodes().OfType<ArrayCreationExpressionSyntax>())
                operations.Add(new CostlyOperation(EvidenceKind.Allocation, creation.GetLocation()));

            var model = compilation.GetSemanticModel(declaration.SyntaxTree);
            foreach (var invocation in declaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var methodSymbol = model.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
                var methodName = methodSymbol?.Name
                    ?? (invocation.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.ValueText;
                if (methodName is "ToList" or "ToArray" or "ToDictionary" or "ToLookup" or "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending")
                    operations.Add(new CostlyOperation(EvidenceKind.Materialization, invocation.GetLocation()));
            }
        }

        return operations.ToImmutable();
    }

    private static bool IsInsideLoop(SyntaxNode node)
    {
        return node.Ancestors().Any(ancestor => ancestor is ForStatementSyntax
            or ForEachStatementSyntax
            or ForEachVariableStatementSyntax
            or WhileStatementSyntax
            or DoStatementSyntax);
    }

    private readonly struct CostlyOperation
    {
        public CostlyOperation(EvidenceKind kind, Location location)
        {
            Kind = kind;
            Location = location;
        }

        public EvidenceKind Kind { get; }
        public Location Location { get; }
    }

    private sealed class SymbolPairComparer : IEqualityComparer<(IMethodSymbol Caller, IMethodSymbol Target)>
    {
        public static SymbolPairComparer Instance { get; } = new();

        public bool Equals((IMethodSymbol Caller, IMethodSymbol Target) x, (IMethodSymbol Caller, IMethodSymbol Target) y)
        {
            return SymbolEqualityComparer.Default.Equals(x.Caller, y.Caller)
                && SymbolEqualityComparer.Default.Equals(x.Target, y.Target);
        }

        public int GetHashCode((IMethodSymbol Caller, IMethodSymbol Target) obj)
        {
            return HashCode.Combine(
                SymbolEqualityComparer.Default.GetHashCode(obj.Caller),
                SymbolEqualityComparer.Default.GetHashCode(obj.Target));
        }
    }
}
