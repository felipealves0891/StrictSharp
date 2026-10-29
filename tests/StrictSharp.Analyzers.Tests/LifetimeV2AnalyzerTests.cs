using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class LifetimeV2AnalyzerTests
{
    [Fact]
    public async Task Perf028ReportsDisposableCreatedWithoutUsing()
    {
        const string source = """
            using System;
            class Resource : IDisposable
            {
                public void Dispose() { }
            }
            class Sample
            {
                Resource Run() => new Resource();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf028Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF028", diagnostic.Id);
    }

    [Fact]
    public async Task Perf028StaysSilentForUsingResource()
    {
        const string source = """
            using System;
            class Resource : IDisposable
            {
                public void Dispose() { }
            }
            class Sample
            {
                void Run()
                {
                    using var resource = new Resource();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf028Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf029ReportsParseExceptionUsedInsideLoop()
    {
        const string source = """
            using System;
            class Sample
            {
                int Run(string[] values)
                {
                    foreach (var value in values)
                    {
                        try { return int.Parse(value); }
                        catch (FormatException) { }
                    }
                    return 0;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf029Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF029", diagnostic.Id);
    }

    [Fact]
    public async Task Perf029StaysSilentForTryParse()
    {
        const string source = """
            using System;
            class Sample
            {
                int Run(string[] values)
                {
                    foreach (var value in values)
                        if (int.TryParse(value, out var result))
                            return result;
                    return 0;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf029Analyzer());

        Assert.Empty(diagnostics);
    }
}
