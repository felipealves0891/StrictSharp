using Microsoft.CodeAnalysis.CSharp;
using StrictSharp.Analyzers.Analyzers;
using StrictSharp.Analyzers.Lifecycle;
using System.Linq;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class LifecycleAnalyzerTests
{
    [Fact]
    public async Task V4CatalogDoesNotDuplicateTaskBlockingWithPerf001()
    {
        var source = Source("""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Hosting;
            class Worker : BackgroundService
            {
                protected override Task ExecuteAsync(CancellationToken token)
                {
                    while (!token.IsCancellationRequested)
                    {
                        Task.Delay(1).Wait();
                        Task.CompletedTask.GetAwaiter().GetResult();
                    }
                    return Task.CompletedTask;
                }
            }
            """);

        var analyzers = V1AnalyzerCatalog.All.AddRange(V4AnalyzerCatalog.All);
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, analyzers);

        var perf001Count = 0;
        var perf110Count = 0;
        foreach (var diagnostic in diagnostics)
        {
            perf001Count += diagnostic.Id == "PERF001" ? 1 : 0;
            perf110Count += diagnostic.Id == "PERF110" ? 1 : 0;
        }

        Assert.Equal(1, perf001Count);
        Assert.Equal(1, perf110Count);
    }

    [Fact]
    public async Task Perf108ReportsPollingWithoutSuspension()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(Source("""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Hosting;
            class Worker : BackgroundService
            {
                protected override Task ExecuteAsync(CancellationToken token)
                {
                    while (!token.IsCancellationRequested)
                    {
                        queue.TryDequeue(out var item);
                    }
                    return Task.CompletedTask;
                }
                Queue queue = new();
            }
            class Queue { public bool TryDequeue(out object item) { item = null; return false; } }
            """), new Perf108Analyzer());

        Assert.Equal("PERF108", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task LifecycleRulesRecognizeDistinctEvidence()
    {
        var source = Source("""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Hosting;
            class Worker : BackgroundService
            {
                protected override Task ExecuteAsync(CancellationToken token)
                {
                    while (!token.IsCancellationRequested)
                    {
                        var values = new[] { 1 }.ToList();
                        var task = Task.Run(() => values.Length);
                        var result = task.Result;
                        stream.Read();
                        _ = values.ToString();
                    }
                    return Task.CompletedTask;
                }
            Stream stream = new();
            }
            class Stream { public int Read() => 0; }
            """);

        Assert.Equal("PERF109", Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf109Analyzer())).Id);
        Assert.Equal("PERF110", Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf110Analyzer())).Id);
        Assert.Equal("PERF111", Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf111Analyzer())).Id);
    }

    [Fact]
    public async Task Perf112ReportsWaitThatDoesNotReceiveCancellation()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(Source("""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Hosting;
            class Worker : BackgroundService
            {
                protected override async Task ExecuteAsync(CancellationToken token)
                {
                    while (!token.IsCancellationRequested)
                        await Task.Delay(1000);
                }
            }
            """), new Perf112Analyzer());

        Assert.Equal("PERF112", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task LifecycleLoopWithDelayAndCancellationDoesNotReportBusyPollingOrInefficientShutdown()
    {
        var source = Source("""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Hosting;
            class Worker : BackgroundService
            {
                protected override async Task ExecuteAsync(CancellationToken token)
                {
                    while (!token.IsCancellationRequested)
                        await Task.Delay(1000, token);
                }
            }
            """);

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf108Analyzer()));
        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf112Analyzer()));
    }

    [Fact]
    public async Task ServiceLoopDoesNotBecomeV5ComplexityByItsOwnIterationCount()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(Source("""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Hosting;
            class Worker : BackgroundService
            {
                protected override async Task ExecuteAsync(CancellationToken token)
                {
                    while (!token.IsCancellationRequested)
                        await Task.Delay(1000, token);
                }
            }
            """), new Perf101Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task LifecycleWaitKindsDoNotBecomeBusyPolling()
    {
        var source = Source("""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Hosting;
            class Worker : BackgroundService
            {
                protected override async Task ExecuteAsync(CancellationToken token)
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(1, token);
                        await reader.ReadAsync(token);
                        await gate.WaitAsync(token);
                    }
                }
                Reader reader = new();
                Gate gate = new();
            }
            class Reader { public Task ReadAsync(CancellationToken token) => Task.CompletedTask; }
            class Gate { public Task WaitAsync(CancellationToken token) => Task.CompletedTask; }
            """);

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf108Analyzer()));
    }

    [Fact]
    public async Task BlockingAnalyzerRecognizesResultWaitAndSynchronousIo()
    {
        var source = Source("""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Hosting;
            class Worker : BackgroundService
            {
                protected override Task ExecuteAsync(CancellationToken token)
                {
                    while (!token.IsCancellationRequested)
                    {
                        var result = Task.CompletedTask.Result;
                        Task.CompletedTask.Wait();
                        System.Threading.Thread.Sleep(1);
                        stream.Read();
                    }
                    return Task.CompletedTask;
                }
                Stream stream = new();
            }
            class Stream { public int Read() => 0; }
            """);

        Assert.Equal("PERF110", Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf110Analyzer())).Id);
    }

    [Fact]
    public async Task ParallelismAnalyzerRecognizesTaskCreationShapes()
    {
        var source = Source("""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Hosting;
            class Worker : BackgroundService
            {
                protected override Task ExecuteAsync(CancellationToken token)
                {
                    while (!token.IsCancellationRequested)
                    {
                        Task.Run(() => { });
                        Task.Factory.StartNew(() => { });
                        Task.WhenAll(Task.CompletedTask);
                        new System.Threading.Thread(() => { }).Start();
                    }
                    return Task.CompletedTask;
                }
            }
            """);

        Assert.Equal("PERF111", Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf111Analyzer())).Id);
    }

    [Fact]
    public async Task ShutdownAnalyzerRecognizesUncancelledLoopAndAcceptsTokenizedWait()
    {
        var uncancelled = Source("""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Hosting;
            class Worker : BackgroundService
            {
                protected override async Task ExecuteAsync(CancellationToken token)
                {
                    while (true)
                        await Task.Delay(1, token);
                }
            }
            """);
        var cancelled = Source("""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Hosting;
            class Worker : BackgroundService
            {
                protected override async Task ExecuteAsync(CancellationToken token)
                {
                    while (!token.IsCancellationRequested)
                        await reader.ReceiveAsync(token);
                }
                Reader reader = new();
            }
            class Reader { public Task ReceiveAsync(CancellationToken token) => Task.CompletedTask; }
            """);

        Assert.Equal("PERF112", Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(uncancelled, new Perf112Analyzer())).Id);
        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(cancelled, new Perf112Analyzer()));
    }

    [Fact]
    public async Task SimilarMethodNameWithoutBackgroundServiceIsIgnored()
    {
        const string source = "using System.Threading; using System.Threading.Tasks; class Worker { protected async Task ExecuteAsync(CancellationToken token) { while (!token.IsCancellationRequested) await Task.Delay(1, token); } }";

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf108Analyzer()));
        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf112Analyzer()));
    }

    [Fact]
    public async Task InterproceduralCostInsideServiceLoopRemainsAnalyzable()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(Source("""
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.Hosting;
            class Worker : BackgroundService
            {
                protected override async Task ExecuteAsync(CancellationToken token)
                {
                    while (!token.IsCancellationRequested)
                    {
                        GetItems();
                        await Task.Delay(1, token);
                    }
                }
                void GetItems() { var values = new byte[8]; }
            }
            """), new Perf100Analyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF100");
    }

    [Fact]
    public async Task EventDrivenLoopIsRecognizedWithoutFrameworkSpecificBaseType()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(SourceWithoutHosting("""
            using System.Threading;
            using System.Threading.Tasks;
            class Consumer
            {
                async Task Consume(CancellationToken token)
                {
                    while (true)
                    {
                        await channel.ReadAsync(token);
                        var values = items.ToArray();
                    }
                }
                Channel channel = new();
                int[] items = new[1];
            }
            class Channel { public Task<object> ReadAsync(CancellationToken token) => Task.FromResult<object>(null); }
            """), new Perf109Analyzer());

        Assert.Equal("PERF109", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task BrokerAndTcpEventLoopsAreRecognizedByAvailabilityOperations()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(SourceWithoutHosting("""
            using System.Threading;
            using System.Threading.Tasks;
            class Consumers
            {
                async Task Consume(CancellationToken token)
                {
                    while (true)
                    {
                        await broker.ReceiveAsync(token);
                        var values = items.ToArray();
                    }
                }
                async Task Accept(CancellationToken token)
                {
                    while (true)
                    {
                        await listener.AcceptAsync(token);
                        var values = items.ToArray();
                    }
                }
                Broker broker = new();
                Listener listener = new();
                int[] items = new[1];
            }
            class Broker { public Task<object> ReceiveAsync(CancellationToken token) => Task.FromResult<object>(null); }
            class Listener { public Task<object> AcceptAsync(CancellationToken token) => Task.FromResult<object>(null); }
            """), new Perf109Analyzer());

        Assert.Equal(2, diagnostics.Length);
    }

    [Fact]
    public async Task UnsupportedContinuousLoopIsNotClassifiedByShapeAlone()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(SourceWithoutHosting("""
            using System.Threading;
            class Sample
            {
                void Run(CancellationToken token)
                {
                    while (true)
                    {
                        var values = items.ToArray();
                    }
                }
                int[] items = new[1];
            }
            """), new Perf109Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DeclaredPerformanceContextSuppliesLifecycleKindAndFrequencyHypothesis()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(SourceWithoutHosting("""
            using System;
            using System.Threading;
            namespace StrictSharp.Analyzers.Lifecycle
            {
                public enum PerformanceContextKind { BackgroundWorker, EventDriven, Polling, Retry, Batch, GameLoop }
                [AttributeUsage(AttributeTargets.Method)]
                public sealed class PerformanceContextAttribute : Attribute
                {
                    public PerformanceContextKind Kind { get; set; }
                    public double ExpectedIterationsPerSecond { get; set; }
                }
            }
            class Worker
            {
                [StrictSharp.Analyzers.Lifecycle.PerformanceContext(
                    Kind = StrictSharp.Analyzers.Lifecycle.PerformanceContextKind.GameLoop,
                    ExpectedIterationsPerSecond = 60)]
                void Run(CancellationToken token)
                {
                    while (true)
                    {
                        var values = items.ToArray();
                    }
                }
                int[] items = new[1];
            }
            """), new Perf109Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF109", diagnostic.Id);
        Assert.Contains("CostPerTime", diagnostic.GetMessage());
        Assert.Contains("hipótese declarada", diagnostic.GetMessage());
        Assert.Contains("60/s", diagnostic.GetMessage());
    }

    [Fact]
    public async Task TaskDelayFrequencyIsReportedAsRequestedDelayNotDeclaredHypothesis()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(SourceWithoutHosting("""
            using System.Threading.Tasks;
            class Scheduler
            {
                async Task Run()
                {
                    while (true)
                    {
                        await Task.Delay(500);
                        var values = items.ToArray();
                    }
                }
                int[] items = new int[1];
            }
            """), new Perf109Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("atraso solicitado; frequência efetiva desconhecida", diagnostic.GetMessage());
        Assert.DoesNotContain("hipótese declarada", diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("BackgroundWorker")]
    [InlineData("EventDriven")]
    [InlineData("Polling")]
    [InlineData("Retry")]
    [InlineData("Batch")]
    [InlineData("GameLoop")]
    public async Task EveryDeclaredPerformanceContextKindIsAccepted(string kind)
    {
        var source = SourceWithoutHosting("""
            using System;
            namespace StrictSharp.Analyzers.Lifecycle
            {
                public enum PerformanceContextKind { BackgroundWorker, EventDriven, Polling, Retry, Batch, GameLoop }
                [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
                public sealed class PerformanceContextAttribute : Attribute
                {
                    public PerformanceContextKind Kind { get; set; }
                    public double ExpectedIterationsPerSecond { get; set; }
                }
            }
            [StrictSharp.Analyzers.Lifecycle.PerformanceContext(
                Kind = StrictSharp.Analyzers.Lifecycle.PerformanceContextKind.KIND)]
            class Worker
            {
                void Run()
                {
                    while (true)
                    {
                        var values = items.ToArray();
                    }
                }
                int[] items = new[1];
            }
            """).Replace("KIND", kind, StringComparison.Ordinal);

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf109Analyzer());

        Assert.Equal("PERF109", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task InvalidFrequencyRemainsCostPerIteration()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(SourceWithoutHosting("""
            using System;
            namespace StrictSharp.Analyzers.Lifecycle
            {
                public enum PerformanceContextKind { BackgroundWorker, EventDriven, Polling, Retry, Batch, GameLoop }
                [AttributeUsage(AttributeTargets.Method)]
                public sealed class PerformanceContextAttribute : Attribute
                {
                    public PerformanceContextKind Kind { get; set; }
                    public double ExpectedIterationsPerSecond { get; set; }
                }
            }
            class Worker
            {
                [StrictSharp.Analyzers.Lifecycle.PerformanceContext(
                    Kind = StrictSharp.Analyzers.Lifecycle.PerformanceContextKind.Polling,
                    ExpectedIterationsPerSecond = -1)]
                void Run()
                {
                    while (true)
                    {
                        var values = items.ToArray();
                    }
                }
                int[] items = new[1];
            }
            """), new Perf109Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("CostPerIteration", diagnostic.GetMessage());
        Assert.DoesNotContain("CostPerTime", diagnostic.GetMessage());
    }

    [Fact]
    public async Task SchedulerRetryBatchAndGameLoopShapesAreRecognized()
    {
        var source = SourceWithoutHosting("""
            using System.Threading;
            using System.Threading.Tasks;
            class Scheduler
            {
                async Task Schedule(CancellationToken token)
                {
                    while (true)
                    {
                        await Task.Delay(1000, token);
                        var values = items.ToArray();
                    }
                }
                async Task Retry(CancellationToken token)
                {
                    var attempt = 0;
                    while (attempt < 3)
                    {
                        attempt++;
                        await BackoffAsync(token);
                        var values = items.ToArray();
                    }
                }
                void Batch(CancellationToken token)
                {
                    while (true)
                    {
                        foreach (var item in items)
                        {
                            var values = items.ToArray();
                        }
                    }
                }
                void GameLoop(CancellationToken token)
                {
                    while (true)
                    {
                        Update();
                        var values = items.ToArray();
                    }
                }
                Task BackoffAsync(CancellationToken token) => Task.CompletedTask;
                void Update() { }
                int[] items = new[1];
            }
            """);

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf109Analyzer());

        Assert.Equal(4, diagnostics.Length);
    }

    [Fact]
    public async Task HostedServiceEntryPointIsRecognizedByInterfaceSymbol()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(SourceWithoutHosting("""
            using System.Threading;
            using System.Threading.Tasks;
            namespace Microsoft.Extensions.Hosting
            {
                public interface IHostedService { Task StartAsync(CancellationToken token); }
            }
            class Worker : Microsoft.Extensions.Hosting.IHostedService
            {
                public async Task StartAsync(CancellationToken token)
                {
                    while (true)
                    {
                        await channel.ReadAsync(token);
                        var values = items.ToArray();
                    }
                }
                Channel channel = new();
                int[] items = new[1];
            }
            class Channel { public Task<object> ReadAsync(CancellationToken token) => Task.FromResult<object>(null); }
            """), new Perf109Analyzer());

        Assert.Equal("PERF109", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task InvalidOrConflictingPerformanceContextFallsBackToUnknown()
    {
        var invalid = await AnalyzerTestHarness.AnalyzeAsync(SourceWithoutHosting("""
            using System;
            namespace StrictSharp.Analyzers.Lifecycle
            {
                public enum PerformanceContextKind { BackgroundWorker, EventDriven, Polling, Retry, Batch, GameLoop }
                [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
                public sealed class PerformanceContextAttribute : Attribute
                {
                    public PerformanceContextKind Kind { get; set; }
                    public double ExpectedIterationsPerSecond { get; set; }
                }
            }
            class Worker
            {
                [StrictSharp.Analyzers.Lifecycle.PerformanceContext(
                    Kind = (StrictSharp.Analyzers.Lifecycle.PerformanceContextKind)999,
                    ExpectedIterationsPerSecond = -1)]
                void Run()
                {
                    while (true)
                    {
                        var values = items.ToArray();
                    }
                }
                int[] items = new[1];
            }
            """), new Perf109Analyzer());

        var conflicting = await AnalyzerTestHarness.AnalyzeAsync(SourceWithoutHosting("""
            using System;
            namespace StrictSharp.Analyzers.Lifecycle
            {
                public enum PerformanceContextKind { BackgroundWorker, EventDriven, Polling, Retry, Batch, GameLoop }
                [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
                public sealed class PerformanceContextAttribute : Attribute
                {
                    public PerformanceContextKind Kind { get; set; }
                    public double ExpectedIterationsPerSecond { get; set; }
                }
            }
            [StrictSharp.Analyzers.Lifecycle.PerformanceContext(
                Kind = StrictSharp.Analyzers.Lifecycle.PerformanceContextKind.EventDriven)]
            class Worker
            {
                [StrictSharp.Analyzers.Lifecycle.PerformanceContext(
                    Kind = StrictSharp.Analyzers.Lifecycle.PerformanceContextKind.Polling)]
                void Run()
                {
                    while (true)
                    {
                        var values = items.ToArray();
                    }
                }
                int[] items = new[1];
            }
            """), new Perf109Analyzer());

        Assert.Empty(invalid);
        Assert.Empty(conflicting);
    }

    [Fact]
    public async Task HomonymousPerformanceContextAttributeIsIgnored()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(SourceWithoutHosting("""
            using System;
            namespace Other
            {
                public enum PerformanceContextKind { Polling }
                [AttributeUsage(AttributeTargets.Method)]
                public sealed class PerformanceContextAttribute : Attribute
                {
                    public PerformanceContextKind Kind { get; set; }
                    public double ExpectedIterationsPerSecond { get; set; }
                }
            }
            class Worker
            {
                [Other.PerformanceContext(Kind = Other.PerformanceContextKind.Polling, ExpectedIterationsPerSecond = 60)]
                void Run()
                {
                    while (true)
                    {
                        var values = items.ToArray();
                    }
                }
                int[] items = new int[1];
            }
            """), new Perf109Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void WaitClassificationCoversKnownAndUnknownShapes()
    {
        Assert.Equal(LifecycleWaitKind.Temporal, Classify("Task.Delay(1);"));
        Assert.Equal(LifecycleWaitKind.Event, Classify("reader.ReadAsync();"));
        Assert.Equal(LifecycleWaitKind.Synchronization, Classify("gate.WaitAsync();"));
        Assert.Equal(LifecycleWaitKind.YieldOnly, Classify("Task.Yield();"));
        Assert.Equal(LifecycleWaitKind.Blocking, Classify("task.Wait();"));
        Assert.Equal(LifecycleWaitKind.Blocking, Classify("var value = task.Result;"));
        Assert.Equal(LifecycleWaitKind.Unknown, Classify("await ProcessAsync();"));
        Assert.Equal(LifecycleWaitKind.NoSuspension, Classify("Process();"));
    }

    [Fact]
    public void EvidencePredicatesCoverPositiveAndNegativeShapes()
    {
        Assert.True(LifecycleAnalysis.HasPollingOperation(Parse("queue.TryRead();")));
        Assert.True(LifecycleAnalysis.HasPollingOperation(Parse("queue.IsEmpty;")));
        Assert.False(LifecycleAnalysis.HasPollingOperation(Parse("Process();")));

        Assert.True(LifecycleAnalysis.HasRepeatedWork(Parse("items.ToArray();")));
        Assert.False(LifecycleAnalysis.HasRepeatedWork(Parse("new object();")));
        Assert.False(LifecycleAnalysis.HasRepeatedWork(Parse("new byte[8];")));
        Assert.False(LifecycleAnalysis.HasRepeatedWork(Parse("Process(item);")));

        Assert.True(LifecycleAnalysis.HasBlockingOperation(Parse("task.GetAwaiter().GetResult();")));
        Assert.True(LifecycleAnalysis.HasBlockingOperation(Parse("stream.Write();")));
        Assert.False(LifecycleAnalysis.HasBlockingOperation(Parse("await ProcessAsync();")));

        Assert.True(LifecycleAnalysis.HasGrowingParallelism(Parse("Task.WhenAll(task);")));
        Assert.True(LifecycleAnalysis.HasGrowingParallelism(Parse("new Task(() => { });")));
        Assert.False(LifecycleAnalysis.HasGrowingParallelism(Parse("Process();")));
    }

    [Fact]
    public void CostUnitsDistinguishIterationWorkItemAndHypotheticalTime()
    {
        var iteration = LifecycleAnalysis.InferCostUnit(Parse("Process();"), out var iterationRate);
        var workItem = LifecycleAnalysis.InferCostUnit(Parse("reader.ReadAsync();"), out var workItemRate);
        var time = LifecycleAnalysis.InferCostUnit(Parse("Task.Delay(500);"), out var timeRate);

        Assert.Equal(LifecycleCostUnit.CostPerIteration, iteration);
        Assert.Null(iterationRate);
        Assert.Equal(LifecycleCostUnit.CostPerWorkItem, workItem);
        Assert.Null(workItemRate);
        Assert.Equal(LifecycleCostUnit.CostPerTime, time);
        Assert.Equal(2d, timeRate);
    }

    [Fact]
    public void CancellationWaitPredicateDistinguishesTokenizedCalls()
    {
        Assert.True(LifecycleAnalysis.HasUncancelableWait(Parse("Task.Delay(1);")));
        Assert.True(LifecycleAnalysis.HasUncancelableWait(Parse("reader.ReadAsync();")));
        Assert.False(LifecycleAnalysis.HasUncancelableWait(Parse("Task.Delay(1, token);")));
        Assert.False(LifecycleAnalysis.HasUncancelableWait(Parse("Process();")));
    }

    [Fact]
    public void LifecycleContextDefaultsToServiceLifecycleWithoutDeclaration()
    {
        var context = new LifecycleContext(
            CSharpSyntaxTree.ParseText("class Sample { void M() { } }").GetRoot()
                .DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().Single(),
            CSharpSyntaxTree.ParseText("while (true) { }").GetRoot()
                .DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.WhileStatementSyntax>().Single(),
            LifecycleWaitKind.Unknown,
            observesCancellation: false,
            LifecycleCostUnit.CostPerIteration,
            requestedIterationsPerSecond: null);

        Assert.Equal(LifecycleExecutionContextKind.ServiceLifecycle, context.ExecutionKind);
        Assert.False(context.IsDeclared);
        Assert.False(context.IsFrequencyHypothesis);
    }

    [Fact]
    public void PerformanceContextContractExposesApprovedFields()
    {
        var context = new PerformanceContextAttribute
        {
            Kind = PerformanceContextKind.EventDriven,
            ExpectedIterationsPerSecond = 100
        };

        Assert.Equal(PerformanceContextKind.EventDriven, context.Kind);
        Assert.Equal(100, context.ExpectedIterationsPerSecond);
    }

    private static LifecycleWaitKind Classify(string statement)
    {
        return LifecycleAnalysis.ClassifyWait(Parse(statement), CancellationToken.None);
    }

    private static Microsoft.CodeAnalysis.SyntaxNode Parse(string statement)
    {
        return CSharpSyntaxTree.ParseText($"class Sample {{ void M() {{ {statement} }} }}").GetRoot();
    }

    private static string Source(string body)
    {
        return body + "\nnamespace Microsoft.Extensions.Hosting { public abstract class BackgroundService { protected abstract Task ExecuteAsync(CancellationToken token); } }";
    }

    private static string SourceWithoutHosting(string body)
    {
        return body;
    }
}
