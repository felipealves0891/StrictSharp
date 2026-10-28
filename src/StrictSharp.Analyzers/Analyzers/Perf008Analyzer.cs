using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf008Analyzer : RuleAnalyzerBase
{
    private const int LargeObjectThreshold = 85_000;

    public Perf008Analyzer()
        : base("PERF008")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ArrayCreationExpression);
    }

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ArrayCreationExpressionSyntax creation
            || !RuleAnalysis.IsInsideLoop(creation)
            || !HasLargeConstantDimension(creation, context.SemanticModel))
            return;

        RuleAnalysis.Report(Rule, context, creation.Type.GetLocation());
    }

    private static bool HasLargeConstantDimension(ArrayCreationExpressionSyntax creation, SemanticModel model)
    {
        return creation.Type.RankSpecifiers
            .SelectMany(specifier => specifier.Sizes)
            .Any(size => model.GetConstantValue(size) is { HasValue: true, Value: int length }
                && length >= LargeObjectThreshold);
    }
}
