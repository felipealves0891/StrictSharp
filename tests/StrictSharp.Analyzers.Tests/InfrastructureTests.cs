using Microsoft.CodeAnalysis.CSharp;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class InfrastructureTests
{
    [Fact]
    public void DiagnosticUtilityPreservesRuleAndSourceLocation()
    {
        var tree = CSharpSyntaxTree.ParseText("public sealed class Sample { }");
        var node = tree.GetRoot().DescendantNodes().Single();
        var rule = RuleCatalog.Get("PERF001");
        var diagnostic = RuleAnalysis.CreateDiagnostic(rule, node.GetLocation());

        Assert.Equal("PERF001", diagnostic.Id);
        Assert.True(RuleAnalysis.IsSourceLocation(diagnostic.Location));
        Assert.Equal(rule.Descriptor.Title.ToString(), diagnostic.GetMessage());
    }
}
