using StrictSharp.Analyzers.Rules;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class RuleCatalogTests
{
    [Fact]
    public void CatalogContainsStableRulesForAllPlannedPhases()
    {
        var rules = RuleCatalog.All;

        Assert.Equal(30, rules.Count);
        Assert.Equal(30, rules.Select(rule => rule.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(15, rules.Count(rule => rule.Phase == RulePhase.V1));
        Assert.Equal(13, rules.Count(rule => rule.Phase == RulePhase.V2));
        Assert.Equal(2, rules.Count(rule => rule.Phase == RulePhase.V3));
    }

    [Fact]
    public void V1ContainsOnlyRulesEnabledForTheFirstDelivery()
    {
        Assert.Equal(15, RuleCatalog.V1.Count);
        Assert.All(RuleCatalog.V1, rule => Assert.True(rule.EnabledInV1));
        Assert.DoesNotContain(RuleCatalog.V1, rule => rule.Phase != RulePhase.V1);
    }

    [Fact]
    public void EveryRuleContainsTheRequiredDiagnosticMetadata()
    {
        Assert.All(RuleCatalog.All, rule =>
        {
            Assert.StartsWith("PERF", rule.Id, StringComparison.Ordinal);
            Assert.NotEmpty(rule.Title);
            Assert.NotEmpty(rule.Category);
            Assert.NotEmpty(rule.Confidence);
            Assert.NotEmpty(rule.Impact);
            Assert.NotEmpty(rule.Scalability);
            Assert.NotEmpty(rule.Correction);
            Assert.NotEmpty(rule.Exceptions);
            Assert.NotEmpty(rule.Reference);
            Assert.Equal(rule.Id, rule.Descriptor.Id);
            Assert.Equal(rule.Title, rule.Descriptor.Title.ToString());
        });
    }

    [Theory]
    [InlineData("PERF001", RiskSeverity.Critical)]
    [InlineData("PERF008", RiskSeverity.Critical)]
    [InlineData("PERF014", RiskSeverity.Critical)]
    [InlineData("PERF025", RiskSeverity.Critical)]
    [InlineData("PERF027", RiskSeverity.High)]
    public void PriorityRulesKeepDocumentedSeverity(string id, RiskSeverity expectedSeverity)
    {
        var rule = Assert.Single(RuleCatalog.All, rule => rule.Id == id);

        Assert.Equal(expectedSeverity, rule.Severity);
    }
}
