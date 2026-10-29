using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class ConcurrencyV2AnalyzerTests
{
    [Fact]
    public async Task Perf002ReportsWhenAllOverAnUnboundedSelect()
    {
        const string source = """
            using System.Linq;
            using System.Threading.Tasks;
            class Sample
            {
                async Task RunAsync(IEnumerable<int> values)
                    => await Task.WhenAll(values.Select(ProcessAsync));
                Task ProcessAsync(int value) => Task.CompletedTask;
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf002Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF002", diagnostic.Id);
    }

    [Fact]
    public async Task Perf002StaysSilentForAnExplicitlyBoundedArray()
    {
        const string source = """
            using System.Threading.Tasks;
            class Sample
            {
                async Task RunAsync()
                    => await Task.WhenAll(Task.CompletedTask, Task.CompletedTask);
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf002Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf004ReportsBlockingWorkInsideLock()
    {
        const string source = """
            using System.Threading;
            class Sample
            {
                void Run(object gate)
                {
                    lock (gate)
                        Thread.Sleep(10);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf004Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF004", diagnostic.Id);
    }

    [Fact]
    public async Task Perf004StaysSilentForShortStateUpdate()
    {
        const string source = """
            class Sample
            {
                private int _value;
                void Run(object gate)
                {
                    lock (gate)
                        _value++;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf004Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf005ReportsPollingWithoutWait()
    {
        const string source = """
            class Sample
            {
                bool IsReady() => false;
                void Run()
                {
                    while (!IsReady())
                    {
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf005Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF005", diagnostic.Id);
    }

    [Fact]
    public async Task Perf005StaysSilentWhenPollingWaits()
    {
        const string source = """
            using System.Threading;
            class Sample
            {
                bool IsReady() => false;
                void Run()
                {
                    while (!IsReady())
                        Thread.Sleep(100);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf005Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf006ReportsTaskRunCreatedInsideLoop()
    {
        const string source = """
            using System.Threading.Tasks;
            class Sample
            {
                void Run(int[] values)
                {
                    foreach (var value in values)
                        Task.Run(() => Process(value));
                }
                void Process(int value) { }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf006Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF006", diagnostic.Id);
    }

    [Fact]
    public async Task Perf006StaysSilentForAStandaloneTask()
    {
        const string source = """
            using System.Threading.Tasks;
            class Sample
            {
                void Run() => Task.Run(Process);
                void Process() { }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf006Analyzer());

        Assert.Empty(diagnostics);
    }
}
