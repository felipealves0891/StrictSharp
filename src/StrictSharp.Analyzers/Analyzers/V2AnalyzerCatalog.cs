using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace StrictSharp.Analyzers.Analyzers;

public static class V2AnalyzerCatalog
{
    public static ImmutableArray<DiagnosticAnalyzer> All { get; } =
    [
        new Perf002Analyzer(), new Perf004Analyzer(), new Perf005Analyzer(), new Perf006Analyzer(),
        new Perf009Analyzer(), new Perf010Analyzer(), new Perf016Analyzer(), new Perf017Analyzer(),
        new Perf021Analyzer(), new Perf023Analyzer(), new Perf024Analyzer(), new Perf028Analyzer(),
        new Perf029Analyzer()
    ];
}
