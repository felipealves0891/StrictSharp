using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using StrictSharp.Analyzers.Rules;
using System.Collections.Immutable;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class RuleConfigurationTests
{
    [Fact]
    public void DiagnosticSeverityCanBeConfiguredWithoutChangingRuleIdentity()
    {
        var rule = RuleCatalog.Get("PERF001");
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var configured = options.WithSpecificDiagnosticOptions(
            ImmutableDictionary<string, ReportDiagnostic>.Empty.Add(rule.Id, ReportDiagnostic.Error));

        Assert.Equal(ReportDiagnostic.Error, configured.SpecificDiagnosticOptions[rule.Id]);
        Assert.Equal("PERF001", rule.Descriptor.Id);
    }

    [Fact]
    public void DiagnosticCanBeDisabledWithNone()
    {
        var rule = RuleCatalog.Get("PERF001");
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var configured = options.WithSpecificDiagnosticOptions(
            ImmutableDictionary<string, ReportDiagnostic>.Empty.Add(rule.Id, ReportDiagnostic.Suppress));

        Assert.Equal(ReportDiagnostic.Suppress, configured.SpecificDiagnosticOptions[rule.Id]);
        Assert.Equal("PERF001", rule.Descriptor.Id);
    }
}
