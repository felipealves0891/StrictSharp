using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Lifecycle;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf108Analyzer : LifecycleAnalyzerBase
{
    public Perf108Analyzer()
        : base("PERF108")
    {
    }

    protected override bool ShouldReport(
        LifecycleContext lifecycle,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return lifecycle.WaitKind == LifecycleWaitKind.NoSuspension
            && LifecycleAnalysis.HasPollingOperation(lifecycle.Loop);
    }
}
