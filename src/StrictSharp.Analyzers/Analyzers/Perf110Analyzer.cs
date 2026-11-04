using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Lifecycle;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf110Analyzer : LifecycleAnalyzerBase
{
    public Perf110Analyzer()
        : base("PERF110")
    {
    }

    protected override bool ShouldReport(
        LifecycleContext lifecycle,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return LifecycleAnalysis.HasBlockingOperationNotCoveredByPerf001(lifecycle.Loop, semanticModel);
    }
}
