using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf001AnalyzerTests
{
    [Fact]
    public async Task ReportsSynchronousWaitsOnPotentiallyIncompleteTasks()
    {
        const string source = """
            using System.Threading.Tasks;
            class Sample
            {
                Task<int> GetAsync() => Task.FromResult(1);
                int FromResult() => Task.FromResult(1).Result;
                int Result() => GetAsync().Result;
                void Wait() => GetAsync().Wait();
                int Awaiter() => GetAsync().GetAwaiter().GetResult();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf001Analyzer());

        Assert.Equal(3, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("PERF001", diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReportKnownCompletedTask()
    {
        const string source = """
            using System.Threading.Tasks;
            class Sample
            {
                int FromResult() => Task.FromResult(1).Result;
                void Completed() => Task.CompletedTask.Wait();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf001Analyzer());

        Assert.Empty(diagnostics);
    }
}
