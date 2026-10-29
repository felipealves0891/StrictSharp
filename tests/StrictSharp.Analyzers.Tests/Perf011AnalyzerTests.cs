using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf011AnalyzerTests
{
    [Fact]
    public async Task Perf011ReportsFieldCollectionRetainedByRepeatedAdd()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                private readonly List<string> _values = new();

                void Run(string[] values)
                {
                    foreach (var value in values)
                        _values.Add(value);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF011", diagnostic.Id);
    }

    [Fact]
    public async Task Perf011StaysSilentForLocalCollection()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Run(string[] values)
                {
                    var local = new List<string>();
                    foreach (var value in values)
                        local.Add(value);
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011StaysSilentWhenFieldCollectionIsCleared()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                private readonly List<string> _values = new();

                void Run(string[] values)
                {
                    foreach (var value in values)
                    {
                        _values.Add(value);
                        _values.Clear();
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }
}
