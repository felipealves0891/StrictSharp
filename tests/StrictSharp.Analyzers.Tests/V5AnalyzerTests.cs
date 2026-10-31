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
    public async Task Perf104ReportsKnownInterproceduralRepeatedCallMultiplier()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Root()
                {
                    for (var i = 0; i < 5; i++)
                        GetItems(i);
                }

                byte[] GetItems(int id) => new byte[8];
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf104Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF104", diagnostic.Id);
        Assert.Contains("Multiplicador estrutural: 5", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Perf102ReportsInterproceduralAllocation()
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

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf102Analyzer());

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "PERF102"
            && diagnostic.GetMessage().Contains("propagada", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Perf107ReportsStructuralComparisonBetweenMatchingLoopEffects()
    {
        const string source = "class Sample { void M(bool flag) { if (flag) { for (var i = 0; i < 10; i++) Process(); } else { for (var i = 0; i < 20; i++) Process(); } } void Process() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF107", diagnostic.Id);
        Assert.Contains("Alternativas", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.DoesNotContain("%", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Perf107IgnoresConditionalWithoutTwoLoopPaths()
    {
        const string source = "class Sample { void M(bool flag, int[] items) { if (flag) foreach (var item in items) _ = item; } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107IgnoresConditionalPathsWithDifferentEffects()
    {
        const string source = "class Sample { void M(bool flag) { if (flag) { for (var i = 0; i < 10; i++) Process(); } else { for (var i = 0; i < 20; i++) Persist(); } } void Process() { } void Persist() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107IgnoresMatchingSyntaxThatResolvesToDifferentLocalFunctions()
    {
        const string source = "class Sample { void M(bool flag) { if (flag) { void Process() { } for (var i = 0; i < 10; i++) Process(); } else { void Process() { } for (var i = 0; i < 20; i++) Process(); } } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107IgnoresDifferentLoopKindsEvenWhenBodiesMatch()
    {
        const string source = "using System.Collections.Generic; class Sample { void M(bool flag, IEnumerable<int> values) { if (flag) { foreach (var value in values) Process(); } else { for (var i = 0; i < 10; i++) Process(); } } void Process() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107DoesNotReportIdenticalDeconstructingForeachWork()
    {
        const string source = "using System.Collections.Generic; class Sample { void M(bool flag, IEnumerable<(int, int)> pairs) { if (flag) { foreach (var (left, right) in pairs) Process(); } else { foreach (var (left, right) in pairs) Process(); } } void Process() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107IgnoresDifferentWhileLoopKinds()
    {
        const string source = "class Sample { void M(bool flag) { if (flag) { while (flag) { Process(); } } else { do { Process(); } while (flag); } } void Process() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107IgnoresLoopPathsWithDifferentLoopCounts()
    {
        const string source = "class Sample { void M(bool flag) { if (flag) { for (var i = 0; i < 2; i++) { for (var j = 0; j < 2; j++) Process(); } } else { for (var i = 0; i < 2; i++) Process(); } } void Process() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107IgnoresUnresolvedMatchingLoopEffects()
    {
        const string source = "class Sample { void M(bool flag) { if (flag) { for (var i = 0; i < 10; i++) Missing(); } else { for (var i = 0; i < 20; i++) Missing(); } } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107IgnoresMatchingEffectsWhenComplexityIsUnknown()
    {
        const string source = "class Sample { void M(bool flag) { if (flag) { for (var i = 0; i < 2; i++) { for (var j = 0; j < 2; j++) { for (var k = 0; k < 2; k++) Process(); } } } else { for (var i = 0; i < 2; i++) { for (var j = 0; j < 2; j++) { for (var k = 0; k < 2; k++) Process(); } } } } void Process() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107IgnoresAdditionalIncrementSideEffect()
    {
        const string source = "class Sample { void M(bool flag) { if (flag) { for (var i = 0; i < 10; i++) Process(); } else { for (var i = 0; i < 20; i++, Persist()) Process(); } } void Process() { } void Persist() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107IgnoresDifferentForeachSources()
    {
        const string source = "using System.Collections.Generic; class Sample { void M(bool flag, IEnumerable<(int, int)> first, IEnumerable<(int, int)> second) { if (flag) { foreach (var (left, right) in first) Process(); } else { foreach (var (left, right) in second) Process(); } } void Process() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107IgnoresDifferentWhileConditions()
    {
        const string source = "class Sample { void M(bool first, bool second) { if (first) { while (first) { Process(); } } else { while (second) { Process(); } } } void Process() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107DoesNotReportIdenticalForeachWork()
    {
        const string source = "using System.Collections.Generic; class Sample { void M(bool flag, IEnumerable<int> items) { if (flag) { foreach (var item in items) Process(); } else { foreach (var item in items) Process(); } } void Process() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107DoesNotReportIdenticalWhileWork()
    {
        const string source = "class Sample { void M(bool flag) { if (flag) { while (flag) { Process(); } } else { while (flag) { Process(); } } } void Process() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107DoesNotReportIdenticalDoWhileWork()
    {
        const string source = "class Sample { void M(bool flag) { if (flag) { do { Process(); } while (flag); } else { do { Process(); } while (flag); } } void Process() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107IgnoresForLoopWithMoreThanOneIncrementor()
    {
        const string source = "class Sample { void M(bool flag) { if (flag) { for (var i = 0; i < 10; i++) Process(); } else { for (var i = 0; i < 20; i++, i++) Process(); } } void Process() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107IgnoresForeachWhenTheBodyUsesDifferentIterationVariables()
    {
        const string source = "using System.Collections.Generic; class Sample { void M(bool flag, IEnumerable<int> items) { if (flag) { foreach (var item in items) Process(item); } else { foreach (var item in items) Process(item); } } void Process(int value) { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Perf107ComparesDescendingForLoopsWithDifferentKnownCounts()
    {
        const string source = "class Sample { void M(bool flag) { if (flag) { for (var i = 10; i > 0; i--) Process(); } else { for (var i = 20; i > 0; i--) Process(); } } void Process() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Equal("PERF107", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task Perf107IgnoresForLoopsWithInclusiveConditions()
    {
        const string source = "class Sample { void M(bool flag) { if (flag) { for (var i = 0; i <= 10; i++) Process(); } else { for (var i = 0; i <= 20; i++) Process(); } } void Process() { } }";

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf107Analyzer());

        Assert.Empty(diagnostics);
    }
}
