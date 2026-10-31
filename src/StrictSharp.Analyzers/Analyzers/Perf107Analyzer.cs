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

        var left = LoopCostInference.Analyze(conditional.Statement, context.CancellationToken);
        var right = LoopCostInference.Analyze(alternative, context.CancellationToken);
        if (left.IsEmpty || right.IsEmpty)
            return;

        var comparison = CostComparison.Compare(left.Items[0], right.Items[0]);
        if (!comparison.IsComparable)
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
            if (firstBody.WithoutTrivia().ToString() != secondBody.WithoutTrivia().ToString())
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
