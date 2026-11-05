using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.V5;
using System.Collections.Immutable;

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
        var evidence = LoopCostInference.Analyze(context.Node, context.CancellationToken, context.SemanticModel);
        var method = (IMethodSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node, context.CancellationToken)!;
        evidence = InterproceduralCostInference.Merge(evidence, InterproceduralCostInference.Analyze(context.SemanticModel.Compilation, method, context.CancellationToken));
        var opportunity = ComplexityOpportunityInference.Analyze(context.Node, context.SemanticModel, context.CancellationToken);
        if (!opportunity.IsKnown || evidence.Items.Length != 1)
            return;

        var actionableEvidence = evidence.Items
            .Select(item => item.WithOpportunity(opportunity))
            .ToImmutableArray();
        V5DiagnosticAnalysis.Report(Rule, context, CostEvidenceSet.Create(actionableEvidence), item => item.Complexity.IsKnown);
    }
}
