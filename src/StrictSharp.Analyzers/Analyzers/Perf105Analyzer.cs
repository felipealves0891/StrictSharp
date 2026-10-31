using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.V5;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf105Analyzer : RuleAnalyzerBase
{
    public Perf105Analyzer()
        : base("PERF105")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeMethod, Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var evidence = IoConcurrencyInference.Analyze(context.Node, context.SemanticModel, context.CancellationToken);
        V5DiagnosticAnalysis.Report(Rule, context, evidence, item => item.Dimension == CostDimension.Io);
    }
}
