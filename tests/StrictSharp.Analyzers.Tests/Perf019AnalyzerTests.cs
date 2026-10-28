using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf019AnalyzerTests
{
    [Fact]
    public async Task ReportsIntermediateStringCopies()
    {
        const string source = """
            class Sample
            {
                string Read(string text)
                {
                    var part = text.Substring(1);
                    var chars = text.ToCharArray();
                    return part + chars.Length;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf019Analyzer());

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("PERF019", diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReportSpanBasedInspection()
    {
        const string source = """
            using System;
            class Sample
            {
                ReadOnlySpan<char> Inspect(string text) => text.AsSpan(1);
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf019Analyzer());

        Assert.Empty(diagnostics);
    }
}
