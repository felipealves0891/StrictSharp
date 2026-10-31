using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.V5;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf101Analyzer : RuleAnalyzerBase
{
    public Perf101Analyzer()
        : base("PERF101")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeMethod, Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var evidence = LoopCostInference.Analyze(context.Node, context.CancellationToken);
        var method = (IMethodSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node, context.CancellationToken)!;
        evidence = InterproceduralCostInference.Merge(evidence, InterproceduralCostInference.Analyze(context.SemanticModel.Compilation, method, context.CancellationToken));
        V5DiagnosticAnalysis.Report(Rule, context, evidence, item => item.Complexity.IsKnown);
    }
}
