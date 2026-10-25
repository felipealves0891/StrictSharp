using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf007AnalyzerTests
{
    [Fact]
    public async Task ReportsAllocationsInsideLoops()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Process()
                {
                    for (var i = 0; i < 1; i++)
                    {
                        var values = new List<int>();
                        var buffer = new byte[16];
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf007Analyzer());

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("PERF007", diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReportAllocationOutsideLoop()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                List<int> Create() => new List<int>();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf007Analyzer());

        Assert.Empty(diagnostics);
    }
}
