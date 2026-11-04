using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Lifecycle;

public abstract class LifecycleAnalyzerBase : RuleAnalyzerBase
{
    protected LifecycleAnalyzerBase(string ruleId)
        : base(ruleId)
    {
    }

    protected sealed override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeMethod, Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration);
    }

    protected abstract bool ShouldReport(
        LifecycleContext lifecycle,
        SemanticModel semanticModel,
        CancellationToken cancellationToken);

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        if (!LifecycleAnalysis.TryCreateContext(context.Node, context.SemanticModel, context.CancellationToken, out var lifecycle)
            || !ShouldReport(lifecycle, context.SemanticModel, context.CancellationToken))
            return;

        var unit = $"{lifecycle.ExecutionKind}: {lifecycle.Unit}";
        if (lifecycle.IsFrequencyHypothesis)
            unit += $" (hipótese declarada: {lifecycle.RequestedIterationsPerSecond:0.###}/s)";
        else if (lifecycle.FrequencySource == LifecycleFrequencySource.RequestedDelay)
            unit += " (atraso solicitado; frequência efetiva desconhecida)";

        context.ReportDiagnostic(Diagnostic.Create(Rule.Descriptor, lifecycle.Loop.GetLocation(), unit));
    }
}
