using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.V5;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf103Analyzer : RuleAnalyzerBase
{
    public Perf103Analyzer()
        : base("PERF103")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeMethod, Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var evidence = AllocationPayloadInference.Analyze(context.Node, context.SemanticModel, context.CancellationToken);
        V5DiagnosticAnalysis.Report(Rule, context, evidence, item => item.Payload.IsKnown);
    }
}
