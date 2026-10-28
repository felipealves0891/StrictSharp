using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf014AnalyzerTests
{
    [Fact]
    public async Task ReportsLinearListSearchInsideLoop()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Match(List<int> processed, IEnumerable<int> items)
                {
                    foreach (var item in items)
                        if (processed.Contains(item)) { }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf014Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF014", diagnostic.Id);
    }

    [Fact]
    public async Task DoesNotReportHashSetSearch()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Match(HashSet<int> processed, IEnumerable<int> items)
                {
                    foreach (var item in items)
                        if (processed.Contains(item)) { }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf014Analyzer());

        Assert.Empty(diagnostics);
    }
}
