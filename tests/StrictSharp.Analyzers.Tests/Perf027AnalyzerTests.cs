using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf027AnalyzerTests
{
    [Fact]
    public async Task ReportsInterpolatedLoggingMessage()
    {
        const string source = """
            interface ILogger
            {
                void LogDebug(string message);
                void LogDebug(string template, object value);
            }
            class Sample
            {
                void Log(ILogger logger, string workflow) => logger.LogDebug($"Workflow: {workflow}");
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf027Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF027", diagnostic.Id);
    }

    [Fact]
    public async Task DoesNotReportStructuredLoggingTemplate()
    {
        const string source = """
            interface ILogger
            {
                void LogDebug(string message);
                void LogDebug(string template, object value);
            }
            class Sample
            {
                void Log(ILogger logger, string workflow) => logger.LogDebug("Workflow: {Workflow}", workflow);
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf027Analyzer());

        Assert.Empty(diagnostics);
    }
}
