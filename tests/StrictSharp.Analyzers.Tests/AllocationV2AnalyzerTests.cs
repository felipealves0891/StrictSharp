using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class AllocationV2AnalyzerTests
{
    [Fact]
    public async Task Perf009ReportsValueTypeConvertedToObject()
    {
        const string source = """
            class Sample
            {
                void Run(int value)
                {
                    object boxed = value;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf009Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF009", diagnostic.Id);
    }

    [Fact]
    public async Task Perf009StaysSilentForGenericValueTypeStorage()
    {
        const string source = """
            class Sample
            {
                void Run(int value)
                {
                    var stored = value;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf009Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf009StaysSilentForObjectContractArgument()
    {
        const string source = """
            class Sample
            {
                void Run(int value) => Consume(value);
                void Consume(object value) { }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf009Analyzer()));
    }

    [Fact]
    public async Task Perf010ReportsCapturedLambdaCreatedInLoop()
    {
        const string source = """
            using System;
            class Sample
            {
                void Run(int[] values)
                {
                    foreach (var value in values)
                        Action work = () => Process(value);
                }
                void Process(int value) { }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf010Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF010", diagnostic.Id);
    }

    [Fact]
    public async Task Perf010StaysSilentForStaticLambda()
    {
        const string source = """
            using System;
            class Sample
            {
                void Run(int[] values)
                {
                    foreach (var value in values)
                        Action work = static () => Process();
                }
                static void Process() { }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf010Analyzer());

        Assert.Empty(diagnostics);
    }
}
