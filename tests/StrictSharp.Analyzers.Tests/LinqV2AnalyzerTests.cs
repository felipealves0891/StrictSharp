using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class LinqV2AnalyzerTests
{
    [Fact]
    public async Task Perf016ReportsOrderingInsideLoop()
    {
        const string source = """
            using System.Linq;
            class Sample
            {
                int[] Run(int[][] values)
                {
                    foreach (var value in values)
                        return value.OrderBy(item => item).ToArray();
                    return [];
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf016Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF016", diagnostic.Id);
    }

    [Fact]
    public async Task Perf016StaysSilentWhenOrderingIsOutsideLoop()
    {
        const string source = """
            using System.Linq;
            class Sample
            {
                int[] Run(int[] values) => values.OrderBy(item => item).ToArray();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf016Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf017ReportsInvariantQueryInsideLoop()
    {
        const string source = """
            using System.Linq;
            class Sample
            {
                int Run(int[] values, int[] batches)
                {
                    foreach (var batch in batches)
                        return values.Where(value => value > 0).Count();
                    return 0;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf017Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF017", diagnostic.Id);
    }

    [Fact]
    public async Task Perf017StaysSilentWhenQueryUsesIterationValue()
    {
        const string source = """
            using System.Linq;
            class Sample
            {
                int Run(int[] values, int[] batches)
                {
                    foreach (var batch in batches)
                        return values.Where(value => value > batch).Count();
                    return 0;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf017Analyzer());

        Assert.Empty(diagnostics);
    }
}
