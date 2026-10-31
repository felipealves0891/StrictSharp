using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf013AnalyzerTests
{
    [Fact]
    public async Task ReportsRepeatedEnumerationOfSameSequence()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            class Sample
            {
                void Read(IEnumerable<int> values)
                {
                    var first = values.Count();
                    var second = values.Count();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf013Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF013", diagnostic.Id);
    }

    [Fact]
    public async Task DoesNotReportSingleEnumeration()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            class Sample
            {
                List<int> Read(IEnumerable<int> values) => values.Where(value => value > 0).ToList();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf013Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReportRepeatedStringSearches()
    {
        const string source = """
            class Sample
            {
                bool ContainsLineTerminator(string text) => text.Contains("\r") || text.Contains("\n");

                bool ContainsCharacters(string text) => text.Contains('x') || text.Contains('y');
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf013Analyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReportsRepeatedEnumerationOfStringCharacters()
    {
        const string source = """
            using System.Linq;
            class Sample
            {
                void Read(string text)
                {
                    var first = text.Count();
                    var second = text.Count();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf013Analyzer());

        Assert.Equal("PERF013", Assert.Single(diagnostics).Id);
    }
}
