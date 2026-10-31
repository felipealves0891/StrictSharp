using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace StrictSharp.Analyzers.Interprocedural;

public static class InterproceduralGraphBuilder
{
    public static InterproceduralGraph Build(
        Compilation compilation,
        IMethodSymbol root,
        CancellationToken cancellationToken)
    {
        var callSites = ImmutableArray.CreateBuilder<CallSite>();
        var analyzed = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        var visiting = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        var bodies = new Dictionary<IMethodSymbol, ImmutableArray<InvocationExpressionSyntax>>(SymbolEqualityComparer.Default);
        var containsCycle = false;

        Visit(root);

        return new InterproceduralGraph(root, callSites.ToImmutable(), containsCycle);

        void Visit(IMethodSymbol method)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!analyzed.Add(method))
                return;

            visiting.Add(method);

            foreach (var invocation in GetInvocations(method))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var target = compilation.GetSemanticModel(invocation.SyntaxTree)
                    .GetSymbolInfo(invocation, cancellationToken)
                    .Symbol as IMethodSymbol;

                if (target is null || !HasSourceBody(target))
                {
                    callSites.Add(new CallSite(method, target, invocation.GetLocation(), CallTargetState.Unknown, ClassifyFlow(invocation, target), invocation));
                    continue;
                }

                callSites.Add(new CallSite(method, target, invocation.GetLocation(), CallTargetState.Known, ClassifyFlow(invocation, target), invocation));
                if (visiting.Contains(target))
                {
                    containsCycle = true;
                    continue;
                }

                Visit(target);
            }

            visiting.Remove(method);
        }

        ImmutableArray<InvocationExpressionSyntax> GetInvocations(IMethodSymbol method)
        {
            if (bodies.TryGetValue(method, out var cached))
                return cached;

            var builder = ImmutableArray.CreateBuilder<InvocationExpressionSyntax>();
            foreach (var syntaxReference in method.DeclaringSyntaxReferences)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (syntaxReference.GetSyntax(cancellationToken) is not SyntaxNode declaration)
                    continue;

                builder.AddRange(declaration.DescendantNodes().OfType<InvocationExpressionSyntax>());
            }

            cached = builder.ToImmutable();
            bodies.Add(method, cached);
            return cached;
        }

        static bool HasSourceBody(IMethodSymbol method)
        {
            foreach (var syntaxReference in method.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is BaseMethodDeclarationSyntax declaration
                    && (declaration.Body is not null || declaration.ExpressionBody is not null))
                    return true;
            }

            return false;
        }

        static CallFlowKind ClassifyFlow(InvocationExpressionSyntax invocation, IMethodSymbol? target)
        {
            if (target is null)
                return CallFlowKind.Unknown;

            if (invocation.Ancestors().Any(static node => node is AwaitExpressionSyntax))
                return CallFlowKind.Awaited;

            if (IsBlocking(target))
                return CallFlowKind.Blocking;

            return CallFlowKind.Synchronous;
        }

        static bool IsBlocking(IMethodSymbol method)
        {
            if (method.Name is "Sleep" or "Wait" or "WaitOne" or "Join")
                return true;

            if (method.ContainingType?.ToDisplayString() == "System.Net.Http.HttpClient")
                return method.Name == "Send";

            if (method.ContainingType?.ToDisplayString() == "System.IO.File")
                return method.Name is "ReadAllText" or "ReadAllBytes" or "WriteAllText" or "WriteAllBytes"
                    or "AppendAllText" or "OpenRead" or "OpenWrite";

            return method.ContainingType is INamedTypeSymbol containingType
                && (IsDerivedFrom(containingType, "System.IO.Stream")
                    || IsDerivedFrom(containingType, "System.IO.TextReader")
                    || IsDerivedFrom(containingType, "System.IO.TextWriter"))
                && method.Name is "Read" or "ReadByte" or "ReadLine" or "ReadToEnd" or "ReadBlock"
                    or "Write" or "WriteLine" or "WriteByte" or "Flush" or "CopyTo";
        }

        static bool IsDerivedFrom(INamedTypeSymbol type, string fullName)
        {
            for (var current = type; current is not null; current = current.BaseType)
            {
                if (current.ToDisplayString() == fullName)
                    return true;
            }

            return false;
        }
    }
}
