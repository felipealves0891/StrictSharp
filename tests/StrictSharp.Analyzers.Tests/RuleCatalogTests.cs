using StrictSharp.Analyzers.Rules;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class RuleCatalogTests
{
    [Fact]
    public void CatalogContainsStableRulesForAllPlannedPhases()
    {
        var rules = RuleCatalog.All;

        Assert.Equal(31, rules.Count);
        Assert.Equal(31, rules.Select(rule => rule.Id).Distinct(StringComparer.Ordinal).Count());
        var v1 = 0;
        var v2 = 0;
        var v3 = 0;
        var v4 = 0;
        foreach (var rule in rules)
        {
            switch (rule.Phase)
            {
                case RulePhase.V1:
                    v1++;
                    break;
                case RulePhase.V2:
                    v2++;
                    break;
                case RulePhase.V3:
                    v3++;
                    break;
                case RulePhase.V4:
                    v4++;
                    break;
            }
        }

        Assert.Equal(15, v1);
        Assert.Equal(13, v2);
        Assert.Equal(2, v3);
        Assert.Equal(1, v4);
    }

    [Fact]
    public void V1ContainsOnlyRulesEnabledForTheFirstDelivery()
    {
        Assert.Equal(15, RuleCatalog.V1.Count);
        Assert.All(RuleCatalog.V1, rule => Assert.True(rule.EnabledInV1));
        Assert.DoesNotContain(RuleCatalog.V1, rule => rule.Phase != RulePhase.V1);
    }

    [Fact]
    public void V2ContainsExactlyTheApprovedRules()
    {
        var expectedIds = new[]
        {
            "PERF002", "PERF004", "PERF005", "PERF006", "PERF009", "PERF010", "PERF016",
            "PERF017", "PERF021", "PERF023", "PERF024", "PERF028", "PERF029"
        };

        Assert.Equal(expectedIds, RuleCatalog.V2.Select(rule => rule.Id));
        Assert.All(RuleCatalog.V2, rule => Assert.True(rule.EnabledInV2));
        Assert.DoesNotContain(RuleCatalog.V2, rule => rule.Phase != RulePhase.V2);
    }

    [Fact]
    public void V3ContainsExactlyTheApprovedRulesAndIsEnabledByDefault()
    {
        var expectedIds = new[] { "PERF011", "PERF030" };

        Assert.Equal(expectedIds, RuleCatalog.V3.Select(rule => rule.Id));
        Assert.All(RuleCatalog.V3, rule =>
        {
            Assert.Equal(RulePhase.V3, rule.Phase);
            Assert.True(rule.Descriptor.IsEnabledByDefault);
        });
    }

    [Fact]
    public void V4ContainsExactlyPerf100WithWarningSeverityAndPerformanceCategory()
    {
        var rule = Assert.Single(RuleCatalog.V4);

        Assert.Equal("PERF100", rule.Id);
        Assert.Equal(RulePhase.V4, rule.Phase);
        Assert.Equal(RiskSeverity.High, rule.Severity);
        Assert.Equal("Performance", rule.Category);
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, rule.Descriptor.DefaultSeverity);
        Assert.True(rule.EnabledInV4);
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

    [Fact]
    public void UnknownRuleIdsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RuleCatalog.Get("PERF999"));
    }

    [Fact]
    public void LowSeverityRulesUseHiddenDiagnosticSeverity()
    {
        var rule = new RuleDefinition(
            "PERF999",
            "Test rule",
            RiskSeverity.Low,
            "Test",
            "Baixa",
            "Impacto",
            "Escalabilidade",
            "Correção",
            "Exceções",
            "Referência",
            RulePhase.V2);

        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Hidden, rule.Descriptor.DefaultSeverity);
        Assert.False(rule.EnabledInV1);
    }
}
