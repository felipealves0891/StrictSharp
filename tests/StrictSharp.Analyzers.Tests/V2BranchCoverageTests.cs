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
    [Fact]
    public async Task Perf002ReportsUnboundedSelectSource()
    {
        const string source = """
            using System.Linq;
            using System.Threading.Tasks;
            class Sample
            {
                Task[] Run(int[] values) => Task.WhenAll(values.Select(value => Task.Delay(value)));
            }
            """;

        Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf002Analyzer()));
    }

    [Fact]
    public async Task Perf005ReportsPollingWithoutWait()
    {
        const string source = """
            class Sample
            {
                bool Ready() => true;
                void Run()
                {
                    while (!Ready()) { }
                }
            }
            """;

        Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf005Analyzer()));
    }

    [Fact]
    public async Task Perf004CoversStreamReaderWriterAndTaskWaitBranches()
    {
        const string source = """
            using System.IO;
            using System.Threading;
            using System.Threading.Tasks;
            class Sample
            {
                void Run(object gate, MemoryStream stream, StringReader reader, StringWriter writer, Task task)
                {
                    lock (gate) { stream.ReadByte(); }
                    lock (gate) { reader.ReadLine(); }
                    lock (gate) { writer.WriteLine("value"); }
                    lock (gate) { task.Wait(); }
                    lock (gate) { Thread.Sleep(1); }
                }
            }
            """;

        Assert.Equal(5, (await AnalyzerTestHarness.AnalyzeAsync(source, new Perf004Analyzer())).Length);
    }

    [Fact]
    public async Task Perf006ReportsTaskRunInsideLoop()
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

        Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf006Analyzer()));
    }

    [Fact]
    public async Task Perf006IgnoresWrongCreationAndCallsOutsideLoop()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;
            class Sample
            {
                void Run(int[] values)
                {
                    new object();
                    Task.Run(Process);
                    foreach (var value in values)
                        Process(value);
                }
                void Process() { }
                void Process(int value) { }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf006Analyzer()));
    }

    [Fact]
    public async Task Perf010CoversParenthesizedCapture()
    {
        const string source = """
            using System;
            class Sample
            {
                void Run(int[] values)
                {
                    foreach (var value in values)
                    {
                        Action work = (int ignored) => Process(value + ignored);
                    }
                }
                void Process(int value) { }
            }
            """;

        Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf010Analyzer()));
    }

    [Fact]
    public async Task Perf009IgnoresNumericAndReferenceConversions()
    {
        const string source = """
            interface IMarker { }
            class Reference : IMarker { }
            class Sample
            {
                void Run(int value, Reference reference)
                {
                    long numeric = value;
                    IMarker converted = reference;
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf009Analyzer()));
    }

    [Fact]
    public async Task Perf009IgnoresUserDefinedBoxingAndArgumentConversions()
    {
        const string source = """
            struct Value
            {
                public static implicit operator object(Value value) => value.ToString();
            }
            class Sample
            {
                void Run(Value value) => Consume(value);
                void Consume(object value) { }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf009Analyzer()));
    }

    [Fact]
    public async Task Perf016CoversAscendingOrdering()
    {
        const string source = """
            using System.Linq;
            class Sample
            {
                int[] Run(int[][] values)
                {
                    foreach (var value in values)
                        return value.OrderBy(item => item).ThenByDescending(item => item).ToArray();
                    return [];
                }
            }
            """;

        Assert.Equal(2, (await AnalyzerTestHarness.AnalyzeAsync(source, new Perf016Analyzer())).Length);
    }

    [Fact]
    public async Task Perf016IgnoresUnknownSourceAndNonMutatingCalls()
    {
        const string source = """
            using System.Linq;
            using System.Collections.Generic;
            class Sample
            {
                IEnumerable<int> GetValues() => [];
                int[] Run(List<int> values)
                {
                    foreach (var item in values)
                    {
                        values.Contains(item);
                        return GetValues().OrderBy(value => value).ToArray();
                    }
                    return [];
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf016Analyzer()));
    }

    [Fact]
    public async Task Perf016ReportsListSortWhenSourceIsStable()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Run(List<int> values, int[] batches)
                {
                    foreach (var batch in batches)
                        values.Sort();
                }
            }
            """;

        Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf016Analyzer()));
    }

    [Fact]
    public async Task Perf016StaysSilentWhenListSortSourceIsMutatedInLoop()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Run(List<int> values, int[] batches)
                {
                    foreach (var batch in batches)
                    {
                        values.Add(batch);
                        values.Sort();
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf016Analyzer()));
    }

    [Fact]
    public async Task Perf017IgnoresUnknownSource()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            class Sample
            {
                IEnumerable<int> GetValues() => [];
                int Run(int[] batches)
                {
                    foreach (var batch in batches)
                        return GetValues().Where(value => value > 0).Count();
                    return 0;
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf017Analyzer()));
    }

    [Fact]
    public async Task Perf017IgnoresPotentiallyMutablePropertySource()
    {
        const string source = """
            using System.Linq;
            class Sample
            {
                int[] Values => [];
                int Run(int[] batches)
                {
                    foreach (var batch in batches)
                        return Values.Where(value => value > 0).Count();
                    return 0;
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf017Analyzer()));
    }

    [Fact]
    public async Task Perf021CoversObjectCreationWithoutArgumentList()
    {
        const string source = """
            using System.Text.Json;
            class Sample
            {
                void Run(int[] values)
                {
                    foreach (var value in values)
                        _ = new JsonSerializerOptions;
                }
            }
            """;

        Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf021Analyzer()));
    }

    [Fact]
    public async Task Perf021IgnoresOptionsWithDynamicLoopConfiguration()
    {
        const string source = """
            using System.Text.Json;
            class Sample
            {
                void Run(JsonSerializerOptions[] options)
                {
                    foreach (var option in options)
                        _ = new JsonSerializerOptions(option);
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf021Analyzer()));
    }

    [Fact]
    public async Task Perf028ReportsUnmanagedLocalLifetime()
    {
        const string source = """
            using System;
            class Resource : IDisposable { public void Dispose() { } }
            class Sample
            {
                void Run()
                {
                    var resource = new Resource();
                }
            }
            """;

        Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf028Analyzer()));
    }

    [Fact]
    public async Task Perf023CoversNonZeroRemoveAndInterfaceList()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Run(List<int> values, IList<int> items)
                {
                    while (values.Count > 0)
                    {
                        values.RemoveAt(1);
                        items.Contains(1);
                    }
                }
            }
            """;

        Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf023Analyzer()));
    }

    [Fact]
    public async Task Perf023IgnoresUnsupportedCollectionAndNonZeroRemove()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Run(Queue<int> queue, List<int> values)
                {
                    foreach (var item in values)
                    {
                        values.RemoveAt(item);
                        queue.Enqueue(item);
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf023Analyzer()));
    }

    [Fact]
    public async Task Perf005CoversForIncrementAndDoQueryForms()
    {
        const string source = """
            class Sample
            {
                bool Ready() => true;
                void Run()
                {
                    for (; Ready(); Ready()) { }
                    do { } while (Ready());
                    for (var index = 0; index < 1; index++) { }
                }
            }
            """;

        Assert.Equal(2, (await AnalyzerTestHarness.AnalyzeAsync(source, new Perf005Analyzer())).Length);
    }

    [Fact]
    public async Task Perf012CoversUnresolvedMaterializationShape()
    {
        const string source = """
            class Sample
            {
                void Run()
                {
                    Unknown values = null;
                    values.ToArray();
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf012Analyzer()));
    }

    [Fact]
    public async Task Perf015CoversCustomCountAndArrayCount()
    {
        const string source = """
            using System.Linq;
            class Custom
            {
                public int Count() => 0;
            }
            class Sample
            {
                bool Run(Custom custom, int[] values, string text)
                    => custom.Count() > 0 && values.Count() > 0 && text.Any();
            }
            """;

        Assert.Equal(2, (await AnalyzerTestHarness.AnalyzeAsync(source, new Perf015Analyzer())).Length);
    }

    [Fact]
    public async Task Perf025IgnoresWrongTypeCreatedInsideMethod()
    {
        const string source = """
            class Sample
            {
                object Create() => new object();
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf025Analyzer()));
    }

    [Fact]
    public async Task Perf024IgnoresCapacityAndLoopLocalCollections()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Run(int[] values)
                {
                    foreach (var value in values)
                    {
                        var local = new List<int>();
                        local.Add(value);
                    }
                    var withCapacity = new List<int>(values.Length);
                    withCapacity.Add(1);
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf024Analyzer()));
    }

    [Fact]
    public async Task Perf028CoversAsyncDisposableAndArgumentTransfer()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;
            class AsyncResource : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => default;
            }
            class Sample
            {
                void Consume(IAsyncDisposable value) { }
                async Task Run()
                {
                    await using var managed = new AsyncResource();
                    Consume(new AsyncResource());
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf028Analyzer()));
    }

    [Fact]
    public async Task Perf029ReportsParseExactInsideLoop()
    {
        const string source = """
            using System;
            class Sample
            {
                DateTime Run(string[] values)
                {
                    foreach (var value in values)
                    {
                        try { return DateTime.ParseExact(value, "O"); }
                        catch (FormatException) { }
                    }
                    return default;
                }
            }
            """;

        Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf029Analyzer()));
    }
}
