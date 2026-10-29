using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class CollectionsV2AnalyzerTests
{
    [Fact]
    public async Task Perf023ReportsRemovingFromStartOfListInLoop()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Run(List<int> values)
                {
                    while (values.Count > 0)
                        values.RemoveAt(0);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf023Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF023", diagnostic.Id);
    }

    [Fact]
    public async Task Perf023StaysSilentForSingleListLookup()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                bool Run(List<int> values, int value) => values.Contains(value);
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf023Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf024ReportsListGrowthWithoutKnownCapacity()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                List<int> Run(int[] values)
                {
                    var result = new List<int>();
                    foreach (var value in values)
                        result.Add(value);
                    return result;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf024Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF024", diagnostic.Id);
    }

    [Fact]
    public async Task Perf024StaysSilentWhenCapacityIsProvided()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                List<int> Run(int[] values)
                {
                    var result = new List<int>(values.Length);
                    foreach (var value in values)
                        result.Add(value);
                    return result;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf024Analyzer());

        Assert.Empty(diagnostics);
    }
}
