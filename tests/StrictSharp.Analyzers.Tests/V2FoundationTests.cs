using StrictSharp.Analyzers.Analyzers;
using StrictSharp.Analyzers.Rules;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class V2FoundationTests
{
    [Fact]
    public void V2CatalogMatchesTheThirteenImplementedRules()
    {
        var expectedIds = RuleCatalog.V2.Select(rule => rule.Id);
        var actualIds = V2AnalyzerCatalog.All
            .SelectMany(analyzer => analyzer.SupportedDiagnostics)
            .Select(descriptor => descriptor.Id);

        Assert.Equal(expectedIds, actualIds);
        Assert.Equal(13, V2AnalyzerCatalog.All.Length);
        Assert.All(V2AnalyzerCatalog.All, analyzer => Assert.Single(analyzer.SupportedDiagnostics));
    }

    [Fact]
    public void V3RulesRemainOutsideTheExecutableCatalogs()
    {
        Assert.DoesNotContain(V2AnalyzerCatalog.All, analyzer =>
            RuleCatalog.Get(analyzer.SupportedDiagnostics[0].Id).Phase == RulePhase.V3);
    }
}
