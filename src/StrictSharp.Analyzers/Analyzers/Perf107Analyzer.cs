using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf107Analyzer : RuleAnalyzerBase
{
    public Perf107Analyzer()
        : base("PERF107")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
    }
}
