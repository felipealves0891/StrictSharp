using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class MetadataV2AnalyzerTests
{
    [Fact]
    public async Task Perf021ReportsRegexCreatedInsideLoop()
    {
        const string source = """
            using System.Text.RegularExpressions;
            class Sample
            {
                void Run(string[] values)
                {
                    foreach (var value in values)
                    {
                        var regex = new Regex(value);
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf021Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF021", diagnostic.Id);
    }

    [Fact]
    public async Task Perf021StaysSilentForRegexCreatedOutsideLoop()
    {
        const string source = """
            using System.Text.RegularExpressions;
            class Sample
            {
                private static readonly Regex Pattern = new Regex("^x$");
                bool Run(string value) => Pattern.IsMatch(value);
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf021Analyzer());

        Assert.Empty(diagnostics);
    }
}
