using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace StrictSharp.Analyzers.Analyzers;

public static class V5AnalyzerCatalog
{
    public static ImmutableArray<DiagnosticAnalyzer> All { get; } =
    [
        new Perf101Analyzer(),
        new Perf102Analyzer(),
        new Perf103Analyzer(),
        new Perf104Analyzer(),
        new Perf105Analyzer(),
        new Perf106Analyzer(),
        new Perf107Analyzer()
    ];
}
