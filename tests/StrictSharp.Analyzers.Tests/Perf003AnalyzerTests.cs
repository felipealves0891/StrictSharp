using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf003AnalyzerTests
{
    [Fact]
    public async Task ReportsTaskRunOnlyWrappingTaskReturningOperation()
    {
        const string source = """
            using System.Threading.Tasks;
            class Sample
            {
                Task<int> GetAsync() => Task.FromResult(1);
                Task<int> Wrap() => Task.Run(() => GetAsync());
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf003Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF003", diagnostic.Id);
    }

    [Fact]
    public async Task DoesNotReportTaskRunForCpuBoundLambda()
    {
        const string source = """
            using System.Threading.Tasks;
            class Sample
            {
                Task<int> Compute() => Task.Run(() => 1);
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf003Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReportsAsyncBlockTaskRunWrapper()
    {
        const string source = """
            using System.Threading.Tasks;
            class Sample
            {
                async Task<int> GetAsync() => 1;
                Task<int> Compute() => Task.Run(async () =>
                {
                    await GetAsync();
                    return 1;
                });
            }
            """;

        Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf003Analyzer()));
    }
}
