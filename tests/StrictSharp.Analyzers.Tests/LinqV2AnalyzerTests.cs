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
    public async Task Perf016StaysSilentWhenSourceIsMutatedInLoop()
    {
        const string source = """
            using System.Linq;
            using System.Collections.Generic;
            class Sample
            {
                int[] Run(List<int> values)
                {
                    foreach (var item in values)
                    {
                        values.Add(item);
                        return values.OrderBy(value => value).ToArray();
                    }
                    return [];
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf016Analyzer()));
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

    [Fact]
    public async Task Perf017StaysSilentWhenQueryHasObservableEffect()
    {
        const string source = """
            using System.Linq;
            class Sample
            {
                int Run(int[] values, int[] batches)
                {
                    foreach (var batch in batches)
                        return values.Select(value => Observe(value)).Count();
                    return 0;
                }
                int Observe(int value) => value;
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf017Analyzer()));
    }

    [Fact]
    public async Task Perf017ReportsInvariantQueryInForLoopWithoutIterationVariable()
    {
        const string source = """
            using System.Linq;
            class Sample
            {
                int Run(int[] values)
                {
                    for (var index = 0; index < 1; index++)
                        return values.Where(value => value > 0).Count();
                    return 0;
                }
            }
            """;

        Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf017Analyzer()));
    }
}
