using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class V5AnalyzerTests
{
    [Fact]
    public async Task Perf101ReportsKnownLoopComplexity()
    {
        const string source = "class Sample { void M(int[] items) { foreach (var item in items) _ = item; } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf101Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF101", diagnostic.Id);
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task Perf103ReportsKnownArrayPayload()
    {
        const string source = "class Sample { void M() { var buffer = new byte[100]; } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf103Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF103", diagnostic.Id);
    }

    [Fact]
    public async Task Perf106ReportsTaskCreationWithoutRuntimeClaims()
    {
        const string source = "class Sample { void M() { System.Threading.Tasks.Task.Run(() => { }); } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf106Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF106", diagnostic.Id);
    }

    [Fact]
    public async Task Perf104ReportsKnownInterproceduralRepeatedCall()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Root(List<int> ids)
                {
                    foreach (var id in ids)
                        GetItems(id);
                }

                byte[] GetItems(int id) => new byte[8];
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf104Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF104", diagnostic.Id);
    }

    [Fact]
    public async Task Perf107ReportsStructuralComparisonBetweenConditionalPaths()
    {
        const string source = "class Sample { void M(bool flag, int[] items) { if (flag) { foreach (var item in items) _ = item; } else { for (var i = 0; i < 10; i++) _ = i; } } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF107", diagnostic.Id);
        Assert.Contains("Alternativas", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.DoesNotContain("%", diagnostic.GetMessage(), StringComparison.Ordinal);
    }
}
