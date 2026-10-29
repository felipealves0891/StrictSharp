using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class V2BranchCoverageTests
{
    [Fact]
    public async Task Perf002CoversNonSelectAndWrongCallShapes()
    {
        const string source = """
            using System.Threading.Tasks;
            class Sample
            {
                async Task Run(Task[] tasks)
                {
                    await Task.WhenAll(tasks);
                    await Task.WhenAll(Task.CompletedTask, Task.CompletedTask);
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf002Analyzer()));
    }

    [Fact]
    public async Task Perf004CoversIoAndNonBlockingLockBranches()
    {
        const string source = """
            using System.IO;
            class Sample
            {
                void Run(object gate)
                {
                    lock (gate) { var value = 1; }
                    lock (gate) { File.ReadAllText("file.txt"); }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf004Analyzer());

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Perf005CoversForDoAndWaitBranches()
    {
        const string source = """
            using System.Threading;
            class Sample
            {
                bool Ready() => true;
                void Run()
                {
                    for (; !Ready(); ) { Thread.Sleep(1); }
                    do { Thread.Sleep(1); } while (!Ready());
                    while (!Ready()) { Thread.Sleep(1); }
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf005Analyzer()));
    }

    [Fact]
    public async Task Perf006ReportsThreadCreationAndIgnoresOrdinaryInvocation()
    {
        const string source = """
            using System.Threading;
            class Sample
            {
                void Run(int[] values)
                {
                    foreach (var value in values)
                    {
                        new Thread(() => Process(value));
                        Process(value);
                    }
                }
                void Process(int value) { }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf006Analyzer());

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Perf009CoversInterfaceBoxing()
    {
        const string source = """
            using System;
            class Sample
            {
                IComparable Run(int value)
                {
                    IComparable boxed = value;
                    return boxed;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf009Analyzer());

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Perf010CoversNonCapturingNonStaticLambda()
    {
        const string source = """
            using System;
            class Sample
            {
                void Run(int[] values)
                {
                    foreach (var value in values)
                        Action work = () => Process();
                }
                void Process() { }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf010Analyzer()));
    }

    [Fact]
    public async Task Perf016CoversAllSupportedOrderingNames()
    {
        const string source = """
            using System.Linq;
            class Sample
            {
                int[] Run(int[][] values)
                {
                    foreach (var value in values)
                        return value.OrderByDescending(item => item).ThenBy(item => item).ToArray();
                    return [];
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf016Analyzer());

        Assert.Equal(2, diagnostics.Length);
    }

    [Fact]
    public async Task Perf017CoversProjectionAndTerminalBranches()
    {
        const string source = """
            using System.Linq;
            class Sample
            {
                int Run(int[] values, int[] batches)
                {
                    foreach (var batch in batches)
                        return values.Select(value => value + 1).ToArray().Length;
                    return 0;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf017Analyzer());

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Perf021IgnoresOtherObjectsInLoops()
    {
        const string source = """
            class Sample
            {
                void Run(int[] values)
                {
                    foreach (var value in values)
                        var item = new object();
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf021Analyzer()));
    }

    [Fact]
    public async Task Perf023ReportsContainsInListLoop()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Run(List<int> values, int[] inputs)
                {
                    foreach (var item in inputs)
                        values.Contains(item);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf023Analyzer());

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Perf024CoversListWithoutGrowthAndCapacityBranch()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                List<int> Run() => new List<int>();
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf024Analyzer()));
    }

    [Fact]
    public async Task Perf028CoversUsingStatementAndNonDisposableObject()
    {
        const string source = """
            using System;
            class Resource : IDisposable { public void Dispose() { } }
            class Sample
            {
                void Run()
                {
                    using (var resource = new Resource()) { }
                    var value = new object();
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf028Analyzer()));
    }

    [Fact]
    public async Task Perf029IgnoresParseOutsideLoopAndOtherCatchTypes()
    {
        const string source = """
            using System;
            class Sample
            {
                int Run(string value)
                {
                    try { return int.Parse(value); }
                    catch (Exception) { return 0; }
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf029Analyzer()));
    }
}
