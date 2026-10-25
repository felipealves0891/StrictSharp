using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf025AnalyzerTests
{
    [Fact]
    public async Task ReportsHttpClientCreatedInsideMethod()
    {
        const string source = """
            using System.Net.Http;
            class Sample
            {
                HttpClient Create() => new HttpClient();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf025Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF025", diagnostic.Id);
    }

    [Fact]
    public async Task DoesNotReportReusableFieldClient()
    {
        const string source = """
            using System.Net.Http;
            class Sample
            {
                private readonly HttpClient client = new HttpClient();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf025Analyzer());

        Assert.Empty(diagnostics);
    }
}
