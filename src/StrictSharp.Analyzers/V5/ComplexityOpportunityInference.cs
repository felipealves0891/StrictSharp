using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.V5;

public static class ComplexityOpportunityInference
{
    public static ComplexityOpportunity Analyze(
        SyntaxNode method,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsRepeatedStableListSearch(invocation, semanticModel))
                return ComplexityOpportunity.RepeatedListSearch;
        }

        return ComplexityOpportunity.None;
    }

    private static bool IsRepeatedStableListSearch(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        if (!RuleAnalysis.IsInsideLoop(invocation)
            || invocation.Expression is not MemberAccessExpressionSyntax member
            || semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method
            || method.Name is not ("Contains" or "IndexOf")
            || method.ContainingType is not INamedTypeSymbol listType
            || listType.Name != "List"
            || listType.ContainingNamespace.ToDisplayString() != "System.Collections.Generic"
            || listType.TypeArguments.Length != 1
            || invocation.ArgumentList.Arguments.Count != 1
            || !RuleAnalysis.IsStableSourceInContainingLoop(member.Expression, invocation, semanticModel))
            return false;

        var argument = invocation.ArgumentList.Arguments[0].Expression;
        if (argument is not IdentifierNameSyntax key)
            return false;

        var keySymbol = semanticModel.GetSymbolInfo(key).Symbol;
        if (keySymbol is not (IParameterSymbol or ILocalSymbol))
            return false;

        var sourceSymbol = GetSourceSymbol(member.Expression, semanticModel);
        if (sourceSymbol is null
            || SymbolEqualityComparer.Default.Equals(sourceSymbol, keySymbol))
            return false;

        var keyType = semanticModel.GetTypeInfo(argument).Type;
        return SymbolEqualityComparer.Default.Equals(keyType, listType.TypeArguments[0]);
    }

    private static ISymbol? GetSourceSymbol(ExpressionSyntax source, SemanticModel semanticModel)
    {
        while (true)
        {
            switch (source)
            {
                case IdentifierNameSyntax identifier:
                    return semanticModel.GetSymbolInfo(identifier).Symbol;
                case MemberAccessExpressionSyntax member:
                    source = member.Expression;
                    continue;
                case ParenthesizedExpressionSyntax parenthesized:
                    source = parenthesized.Expression;
                    continue;
                default:
                    return null;
            }
        }
    }
}
