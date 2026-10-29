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

    [Fact]
    public async Task ReportsAllSupportedListSearchMethodsInsideLoop()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            class Sample
            {
                void Match(List<int> values, int item)
                {
                    foreach (var current in values)
                    {
                        values.IndexOf(item);
                        values.Find(value => value == item);
                        values.First();
                        values.FirstOrDefault();
                        values.SingleOrDefault();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf014Analyzer());

        Assert.Equal(2, diagnostics.Length);
    }
}
