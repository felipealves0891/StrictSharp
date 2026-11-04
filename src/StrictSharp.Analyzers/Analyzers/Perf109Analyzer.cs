using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Lifecycle;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf109Analyzer : LifecycleAnalyzerBase
{
    public Perf109Analyzer()
        : base("PERF109")
    {
    }

    protected override bool ShouldReport(
        LifecycleContext lifecycle,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return LifecycleAnalysis.HasRepeatedWork(lifecycle.Loop);
    }
}
