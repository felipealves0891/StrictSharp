using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Lifecycle;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf112Analyzer : LifecycleAnalyzerBase
{
    public Perf112Analyzer()
        : base("PERF112")
    {
    }

    protected override bool ShouldReport(
        LifecycleContext lifecycle,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return !lifecycle.ObservesCancellation
            || LifecycleAnalysis.HasUncancelableWait(lifecycle.Loop);
    }
}
