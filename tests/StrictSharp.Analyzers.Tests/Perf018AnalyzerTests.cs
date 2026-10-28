using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf018AnalyzerTests
{
    [Fact]
    public async Task ReportsStringAccumulationInsideLoop()
    {
        const string source = """
            class Sample
            {
                string Build()
                {
                    var result = "";
                    for (var i = 0; i < 2; i++)
                        result += i;
                    return result;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf018Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF018", diagnostic.Id);
    }

    [Fact]
    public async Task DoesNotReportStringAccumulationOutsideLoop()
    {
        const string source = """
            class Sample
            {
                string Build(string value) => value += "suffix";
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf018Analyzer());

        Assert.Empty(diagnostics);
    }
}
