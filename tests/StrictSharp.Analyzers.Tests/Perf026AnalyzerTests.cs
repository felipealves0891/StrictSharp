using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf026AnalyzerTests
{
    [Fact]
    public async Task ReportsSynchronousFileIoInAsyncFlow()
    {
        const string source = """
            using System.IO;
            using System.Threading.Tasks;
            class Sample
            {
                async Task<string> ReadAsync(string path) => File.ReadAllText(path);
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf026Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF026", diagnostic.Id);
    }

    [Fact]
    public async Task DoesNotReportSynchronousFileIoInSynchronousFlowOrAsyncApi()
    {
        const string source = """
            using System.IO;
            using System.Threading.Tasks;
            class Sample
            {
                string Read(string path) => File.ReadAllText(path);
                async Task<string> ReadAsync(string path) => await File.ReadAllTextAsync(path);
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf026Analyzer());

        Assert.Empty(diagnostics);
    }
}
