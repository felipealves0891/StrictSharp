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
        InvocationExpressionSyntax? recognizedInvocation = null;
        foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsRepeatedStableListSearch(invocation, semanticModel, method))
            {
                if (recognizedInvocation is not null)
                    return ComplexityOpportunity.None;

                recognizedInvocation = invocation;
            }
        }

        if (recognizedInvocation is null || HasUnrelatedLoop(method, recognizedInvocation))
            return ComplexityOpportunity.None;

        return ComplexityOpportunity.RepeatedListSearch;
    }

    private static bool HasUnrelatedLoop(SyntaxNode method, InvocationExpressionSyntax recognizedInvocation)
    {
        return method.DescendantNodes().Any(node => (node is ForStatementSyntax
            or ForEachStatementSyntax
            or ForEachVariableStatementSyntax
            or WhileStatementSyntax
            or DoStatementSyntax)
            && !node.Span.Contains(recognizedInvocation.Span));
    }

    private static bool IsRepeatedStableListSearch(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        SyntaxNode containingMethod)
    {
        if (!RuleAnalysis.IsInsideLoop(invocation)
            || invocation.Expression is not MemberAccessExpressionSyntax member
            || semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method
            || method.Name != "Contains"
            || method.ContainingType is not INamedTypeSymbol listType
            || listType.Name != "List"
            || listType.ContainingNamespace.ToDisplayString() != "System.Collections.Generic"
            || listType.TypeArguments.Length != 1
            || invocation.ArgumentList.Arguments.Count != 1
            || member.Expression is not IdentifierNameSyntax
            || !RuleAnalysis.IsStableSourceInContainingLoop(member.Expression, invocation, semanticModel)
            || HasOtherSourceUse(member.Expression, containingMethod, semanticModel))
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

    private static bool HasOtherSourceUse(
        ExpressionSyntax source,
        SyntaxNode containingMethod,
        SemanticModel semanticModel)
    {
        if (semanticModel.GetSymbolInfo(source).Symbol is not { } sourceSymbol)
            return true;

        foreach (var identifier in containingMethod.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (!SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(identifier).Symbol, sourceSymbol)
                || identifier.Span == source.Span)
                continue;

            return true;
        }

        return false;
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
