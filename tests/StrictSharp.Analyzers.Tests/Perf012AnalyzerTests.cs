using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf012AnalyzerTests
{
    [Fact]
    public async Task ReportsCollectionMaterializationAndClone()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class Sample
            {
                void Copy(IEnumerable<int> values, string text)
                {
                    var list = values.ToList();
                    var array = values.ToArray();
                    var clone = text.Clone();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf012Analyzer());

        Assert.Equal(3, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("PERF012", diagnostic.Id));
    }

    [Fact]
    public async Task DoesNotReportUnrelatedLinqOperation()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            class Sample
            {
                bool HasValue(IEnumerable<int> values) => values.Any();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf012Analyzer());

        Assert.Empty(diagnostics);
    }
}
