using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Lifecycle;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf111Analyzer : LifecycleAnalyzerBase
{
    public Perf111Analyzer()
        : base("PERF111")
    {
    }

    protected override bool ShouldReport(
        LifecycleContext lifecycle,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return LifecycleAnalysis.HasGrowingParallelism(lifecycle.Loop);
    }
}
