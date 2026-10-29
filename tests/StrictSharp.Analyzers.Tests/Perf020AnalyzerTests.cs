using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf020AnalyzerTests
{
    [Fact]
    public async Task ReportsNormalizationUsedForComparison()
    {
        const string source = """
            class Sample
            {
                bool Matches(string value, string expected)
                {
                    return value.ToLower() == expected || value.ToUpperInvariant().Equals(expected);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf020Analyzer());

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("PERF020", diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReportNormalizationUsedForStorage()
    {
        const string source = """
            class Sample
            {
                string Normalize(string value) => value.ToLowerInvariant();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf020Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReportsNotEqualsAndCompareNormalization()
    {
        const string source = """
            class Sample
            {
                bool Matches(string value, string expected)
                    => value.ToUpper() != expected || value.ToLower().Equals(expected);
            }
            """;

        Assert.Equal(2, (await AnalyzerTestHarness.AnalyzeAsync(source, new Perf020Analyzer())).Length);
    }
}
