using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace StrictSharp.Analyzers.Analyzers;

public static class V4AnalyzerCatalog
{
    public static ImmutableArray<DiagnosticAnalyzer> All { get; } =
    [
        new Perf100Analyzer(),
        new Perf108Analyzer(),
        new Perf109Analyzer(),
        new Perf110Analyzer(),
        new Perf111Analyzer(),
        new Perf112Analyzer()
    ];
}
