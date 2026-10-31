using StrictSharp.Analyzers.Analyzers;
using StrictSharp.Analyzers.Rules;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf100AnalyzerTests
{
    [Fact]
    public async Task ReportsInterproceduralCostWithDynamicEvidence()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            class Sample
            {
                private readonly List<int> _items = new();

                void Root(List<int> ids)
                {
                    foreach (var id in ids)
                        GetItems(id);
                }

                List<int> GetItems(int id)
                {
                    var buffer = new byte[8];
                    _ = buffer.Length;
                    return _items.Where(item => item == id).ToList();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf100Analyzer());

        var diagnostic = Assert.Single(diagnostics, item => item.Id == "PERF100");
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Matches("Performance.*Root -> GetItems", diagnostic.GetMessage());
    }

    [Fact]
    public async Task RemainsSilentWhenThereIsNoInterproceduralEvidence()
    {
        const string source = """
            class Sample
            {
                void Root() { }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf100Analyzer());

        Assert.Empty(diagnostics);
        Assert.Equal("Performance", RuleCatalog.Get("PERF100").Category);
    }
}
