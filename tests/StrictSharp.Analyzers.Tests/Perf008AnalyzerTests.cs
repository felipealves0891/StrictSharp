using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf008AnalyzerTests
{
    [Fact]
    public async Task ReportsLargeConstantBufferInsideLoop()
    {
        const string source = """
            class Sample
            {
                void Process()
                {
                    for (var i = 0; i < 1; i++)
                    {
                        var buffer = new byte[100_000];
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf008Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF008", diagnostic.Id);
    }

    [Fact]
    public async Task DoesNotReportSmallBuffer()
    {
        const string source = """
            class Sample
            {
                void Process()
                {
                    for (var i = 0; i < 1; i++)
                    {
                        var buffer = new byte[128];
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf008Analyzer());

        Assert.Empty(diagnostics);
    }
}
