using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.V5;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf102Analyzer : RuleAnalyzerBase
{
    public Perf102Analyzer()
        : base("PERF102")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeMethod, Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var evidence = AllocationPayloadInference.Analyze(context.Node, context.SemanticModel, context.CancellationToken);
        var method = (IMethodSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node, context.CancellationToken)!;
        evidence = InterproceduralCostInference.Merge(evidence, InterproceduralCostInference.Analyze(context.SemanticModel.Compilation, method, context.CancellationToken));
        V5DiagnosticAnalysis.Report(Rule, context, evidence, item => item.Dimension == CostDimension.Memory);
    }
}
