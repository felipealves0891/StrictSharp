using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf015AnalyzerTests
{
    [Fact]
    public async Task ReportsCountAndAnyOnCollectionWithDirectCount()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            class Sample
            {
                bool Check(ICollection<int> values)
                {
                    return values.Count() > 0 && values.Any();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf015Analyzer());

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("PERF015", diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReportPredicateAny()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            class Sample
            {
                bool Check(ICollection<int> values) => values.Any(value => value > 0);
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf015Analyzer());

        Assert.Empty(diagnostics);
    }
}
