using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.V5;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf104Analyzer : RuleAnalyzerBase
{
    public Perf104Analyzer()
        : base("PERF104")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeMethod, Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var evidence = LoopCostInference.Analyze(context.Node, context.CancellationToken);
        V5DiagnosticAnalysis.Report(Rule, context, evidence, item => item.Multiplier.IsKnown);
    }
}
