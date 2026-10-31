using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.V5;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf107Analyzer : RuleAnalyzerBase
{
    public Perf107Analyzer()
        : base("PERF107")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeConditional, SyntaxKind.IfStatement);
    }

    private void AnalyzeConditional(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not IfStatementSyntax conditional
            || conditional.Else?.Statement is not { } alternative)
            return;

        if (!HaveComparableLoopEffects(context.SemanticModel, conditional.Statement, alternative, context.CancellationToken))
            return;

        var left = LoopCostInference.Analyze(conditional.Statement, context.CancellationToken, context.SemanticModel);
        var right = LoopCostInference.Analyze(alternative, context.CancellationToken, context.SemanticModel);
        if (left.IsEmpty || right.IsEmpty)
            return;

        var comparison = CostComparison.Compare(left.Items[0], right.Items[0]);
        if (!comparison.IsComparable
            || left.Items[0].Complexity.Kind == right.Items[0].Complexity.Kind
                && (!left.Items[0].Count.IsKnown
                    || !right.Items[0].Count.IsKnown
                    || left.Items[0].Count.Value == right.Items[0].Count.Value))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule.Descriptor,
            conditional.GetLocation(),
            $"Alternativas: {comparison.ComplexityChange}; {comparison.WorkChange}.",
            CostDimension.Performance,
            comparison.Limitations));
    }

    private static bool HaveComparableLoopEffects(
        SemanticModel semanticModel,
        SyntaxNode first,
        SyntaxNode second,
        CancellationToken cancellationToken)
    {
        var firstLoops = first.DescendantNodesAndSelf().Where(IsLoop).ToArray();
        var secondLoops = second.DescendantNodesAndSelf().Where(IsLoop).ToArray();
        if (firstLoops.Length == 0 || firstLoops.Length != secondLoops.Length)
            return false;

        for (var index = 0; index < firstLoops.Length; index++)
        {
            var firstBody = GetBody(firstLoops[index]);
            var secondBody = GetBody(secondLoops[index]);
            if (!HaveComparableLoopHeaders(semanticModel, firstLoops[index], secondLoops[index], cancellationToken)
                || firstBody.WithoutTrivia().ToString() != secondBody.WithoutTrivia().ToString())
                return false;

            var firstIdentifiers = firstBody.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().ToArray();
            var secondIdentifiers = secondBody.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().ToArray();
            if (firstIdentifiers.Length != secondIdentifiers.Length)
                return false;

            for (var identifierIndex = 0; identifierIndex < firstIdentifiers.Length; identifierIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var firstSymbol = semanticModel.GetSymbolInfo(firstIdentifiers[identifierIndex], cancellationToken).Symbol;
                var secondSymbol = semanticModel.GetSymbolInfo(secondIdentifiers[identifierIndex], cancellationToken).Symbol;
                if (firstSymbol is null || !SymbolEqualityComparer.Default.Equals(firstSymbol, secondSymbol))
                    return false;
            }
        }

        return true;
    }

    private static bool HaveComparableLoopHeaders(
        SemanticModel semanticModel,
        SyntaxNode first,
        SyntaxNode second,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (first is ForStatementSyntax firstFor && second is ForStatementSyntax secondFor)
            return HaveComparableForHeaders(semanticModel, firstFor, secondFor, cancellationToken);
        if (first is ForEachStatementSyntax firstForEach && second is ForEachStatementSyntax secondForEach)
            return firstForEach.Type.WithoutTrivia().ToString() == secondForEach.Type.WithoutTrivia().ToString()
                && firstForEach.Identifier.ValueText == secondForEach.Identifier.ValueText
                && HaveSameBoundExpression(semanticModel, firstForEach.Expression, secondForEach.Expression, cancellationToken);
        if (first is ForEachVariableStatementSyntax firstForEachVariable
            && second is ForEachVariableStatementSyntax secondForEachVariable)
            return firstForEachVariable.Variable.WithoutTrivia().ToString() == secondForEachVariable.Variable.WithoutTrivia().ToString()
                && HaveSameBoundExpression(semanticModel, firstForEachVariable.Expression, secondForEachVariable.Expression, cancellationToken);
        if (first is WhileStatementSyntax firstWhile && second is WhileStatementSyntax secondWhile)
            return HaveSameBoundExpression(semanticModel, firstWhile.Condition, secondWhile.Condition, cancellationToken);
        if (first is DoStatementSyntax firstDo && second is DoStatementSyntax secondDo)
            return HaveSameBoundExpression(semanticModel, firstDo.Condition, secondDo.Condition, cancellationToken);
        return false;
    }

    private static bool HaveComparableForHeaders(
        SemanticModel semanticModel,
        ForStatementSyntax first,
        ForStatementSyntax second,
        CancellationToken cancellationToken)
    {
        if (first.Condition is not BinaryExpressionSyntax firstCondition
            || second.Condition is not BinaryExpressionSyntax secondCondition
            || firstCondition.Kind() != secondCondition.Kind()
            || firstCondition.Kind() is not (SyntaxKind.LessThanExpression or SyntaxKind.GreaterThanExpression)
            || firstCondition.Left is not IdentifierNameSyntax firstVariable
            || secondCondition.Left is not IdentifierNameSyntax secondVariable
            || firstVariable.Identifier.ValueText != secondVariable.Identifier.ValueText
            || !HaveComparableForInitializers(semanticModel, first, second, cancellationToken)
            || first.Incrementors.Count != 1
            || second.Incrementors.Count != 1
            || first.Incrementors[0].WithoutTrivia().ToString() != second.Incrementors[0].WithoutTrivia().ToString())
            return false;

        return LoopCostInference.TryGetKnownIterationCount(first, semanticModel, out _, cancellationToken)
            && LoopCostInference.TryGetKnownIterationCount(second, semanticModel, out _, cancellationToken);
    }

    private static bool HaveComparableForInitializers(
        SemanticModel semanticModel,
        ForStatementSyntax first,
        ForStatementSyntax second,
        CancellationToken cancellationToken)
    {
        if (first.Declaration is not null || second.Declaration is not null)
        {
            if (first.Declaration is not { } declaredFirst
                || second.Declaration is not { } declaredSecond
                || declaredFirst.Type.WithoutTrivia().ToString() != declaredSecond.Type.WithoutTrivia().ToString()
                || declaredFirst.Variables.Count != 1
                || declaredSecond.Variables.Count != 1
                || declaredFirst.Variables[0].Identifier.ValueText != declaredSecond.Variables[0].Identifier.ValueText)
                return false;

            if (semanticModel.GetDeclaredSymbol(declaredFirst.Variables[0], cancellationToken) is not ILocalSymbol firstSymbol
                || semanticModel.GetDeclaredSymbol(declaredSecond.Variables[0], cancellationToken) is not ILocalSymbol secondSymbol)
                return false;

            return SymbolEqualityComparer.Default.Equals(firstSymbol.Type, secondSymbol.Type);
        }

        if (first.Initializers.Count != second.Initializers.Count)
            return false;

        for (var index = 0; index < first.Initializers.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (first.Initializers[index].WithoutTrivia().ToString()
                != second.Initializers[index].WithoutTrivia().ToString())
                return false;
        }

        return true;
    }

    private static bool HaveSameBoundExpression(
        SemanticModel semanticModel,
        ExpressionSyntax first,
        ExpressionSyntax second,
        CancellationToken cancellationToken)
    {
        if (first.WithoutTrivia().ToString() != second.WithoutTrivia().ToString())
            return false;

        var firstIdentifiers = first.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().ToArray();
        var secondIdentifiers = second.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().ToArray();
        if (firstIdentifiers.Length != secondIdentifiers.Length)
            return false;

        for (var index = 0; index < firstIdentifiers.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var firstSymbol = semanticModel.GetSymbolInfo(firstIdentifiers[index], cancellationToken).Symbol;
            var secondSymbol = semanticModel.GetSymbolInfo(secondIdentifiers[index], cancellationToken).Symbol;
            if (firstSymbol is null || !SymbolEqualityComparer.Default.Equals(firstSymbol, secondSymbol))
                return false;
        }

        return true;
    }

    private static SyntaxNode GetBody(SyntaxNode loop)
    {
        return loop switch
        {
            ForStatementSyntax statement => statement.Statement,
            ForEachStatementSyntax statement => statement.Statement,
            ForEachVariableStatementSyntax statement => statement.Statement,
            WhileStatementSyntax statement => statement.Statement,
            DoStatementSyntax statement => statement.Statement,
            _ => loop
        };
    }

    private static bool IsLoop(SyntaxNode node)
    {
        return node is ForStatementSyntax
            or ForEachStatementSyntax
            or ForEachVariableStatementSyntax
            or WhileStatementSyntax
            or DoStatementSyntax;
    }
}
