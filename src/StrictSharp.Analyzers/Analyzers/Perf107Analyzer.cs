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
}
