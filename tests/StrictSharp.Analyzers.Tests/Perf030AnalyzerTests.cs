using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf030AnalyzerTests
{
    [Fact]
    public async Task Perf030ReportsRepeatedComplexConstructionWithInvariantValue()
    {
        const string source = """
            class Expensive
            {
                private readonly string _value;

                public Expensive(string value)
                {
                    _value = value.ToUpperInvariant();
                }
            }

            class Sample
            {
                void Run(string[] values)
                {
                    foreach (var value in values)
                        _ = new Expensive("fixed");
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf030Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF030", diagnostic.Id);
    }

    [Fact]
    public async Task Perf030StaysSilentWhenConstructionDependsOnLoopValue()
    {
        const string source = """
            class Expensive
            {
                private readonly string _normalized;

                public Expensive(string value)
                {
                    _normalized = value.ToUpperInvariant();
                }
            }

            class Sample
            {
                void Run(string[] values)
                {
                    foreach (var value in values)
                        _ = new Expensive(value);
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf030Analyzer()));
    }

    [Fact]
    public async Task Perf030StaysSilentWhenConstructionIsOutsideLoop()
    {
        const string source = """
            class Expensive
            {
                public Expensive(string value) { }
            }

            class Sample
            {
                private readonly Expensive _value = new("fixed");

                void Run(string[] values)
                {
                    foreach (var value in values)
                        _ = value;
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf030Analyzer()));
    }

    [Fact]
    public async Task Perf030StaysSilentWhenLoopInvarianceCannotBeProved()
    {
        const string source = """
            class Expensive
            {
                public Expensive(string value) { }
            }

            class Sample
            {
                void Run(string value)
                {
                    while (value.Length > 0)
                        _ = new Expensive(value);
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf030Analyzer()));
    }

    [Fact]
    public async Task Perf030ReportsInvariantConstructionInForLoop()
    {
        const string source = """
            class Expensive
            {
                private readonly string _normalized;

                public Expensive(string value)
                {
                    _normalized = value.ToUpperInvariant();
                }
            }

            class Sample
            {
                void Run(int count)
                {
                    for (var index = 0; index < count; index++)
                        _ = new Expensive("fixed");
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf030Analyzer());

        Assert.Single(diagnostics);
        Assert.Equal("PERF030", diagnostics[0].Id);
    }

    [Fact]
    public async Task Perf030ReportsInvariantObjectInitializerInLoop()
    {
        const string source = """
            class Descriptor
            {
                public string Name { get; init; }
            }

            class Sample
            {
                void Run(string[] values)
                {
                    foreach (var value in values)
                        _ = new Descriptor { Name = "fixed".ToUpperInvariant() };
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf030Analyzer());

        Assert.Single(diagnostics);
        Assert.Equal("PERF030", diagnostics[0].Id);
    }

    [Fact]
    public async Task Perf030StaysSilentForSimpleParameterizedConstruction()
    {
        const string source = """
            class Simple
            {
                public Simple(string value) { }
            }

            class Sample
            {
                void Run(string[] values)
                {
                    foreach (var value in values)
                        _ = new Simple("fixed");
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf030Analyzer()));
    }

    [Fact]
    public async Task Perf030StaysSilentForTrivialObjectInitializerInLoop()
    {
        const string source = """
            class Descriptor
            {
                public string Name { get; init; }
            }

            class Sample
            {
                void Run(string[] values)
                {
                    foreach (var value in values)
                        _ = new Descriptor { Name = "fixed" };
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf030Analyzer()));
    }

    [Fact]
    public async Task Perf030StaysSilentForSimpleParameterlessConstruction()
    {
        const string source = """
            class Simple { }

            class Sample
            {
                void Run(string[] values)
                {
                    foreach (var value in values)
                        _ = new Simple();
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf030Analyzer()));
    }
}
