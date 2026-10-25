using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace StrictSharp.Analyzers.Analyzers;

public static class V1AnalyzerCatalog
{
    public static ImmutableArray<DiagnosticAnalyzer> All { get; } =
    [
        new Perf001Analyzer(),
        new Perf003Analyzer(),
        new Perf007Analyzer(),
        new Perf008Analyzer(),
        new Perf012Analyzer(),
        new Perf013Analyzer(),
        new Perf014Analyzer(),
        new Perf015Analyzer(),
        new Perf018Analyzer(),
        new Perf019Analyzer(),
        new Perf020Analyzer(),
        new Perf022Analyzer(),
        new Perf025Analyzer(),
        new Perf026Analyzer(),
        new Perf027Analyzer()
    ];
}
