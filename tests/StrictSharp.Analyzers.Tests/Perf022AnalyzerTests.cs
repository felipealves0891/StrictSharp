using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf022AnalyzerTests
{
    [Fact]
    public async Task ReportsContainsKeyBeforeIndexerOrRemove()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Read(Dictionary<string, int> values, string key)
                {
                    if (values.ContainsKey(key))
                    {
                        var value = values[key];
                    }
                    if (values.ContainsKey(key))
                        values.Remove(key);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf022Analyzer());

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("PERF022", diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReportTryGetValue()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                int Read(Dictionary<string, int> values, string key)
                {
                    return values.TryGetValue(key, out var value) ? value : 0;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf022Analyzer());

        Assert.Empty(diagnostics);
    }
}
