using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Analyzers;
using StrictSharp.Analyzers.Rules;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class V5TestMatrixTests
{
    [Fact]
    public void V5CatalogContainsExactlyTheApprovedRules()
    {
        var actualIds = V5AnalyzerCatalog.All
            .SelectMany(analyzer => analyzer.SupportedDiagnostics)
            .Select(descriptor => descriptor.Id);

        Assert.Equal(RuleCatalog.V5.Select(rule => rule.Id), actualIds);
        Assert.Equal(new[] { "PERF101", "PERF102", "PERF103", "PERF104", "PERF105", "PERF106", "PERF107" }, actualIds);
        Assert.All(V5AnalyzerCatalog.All, analyzer => Assert.Single(analyzer.SupportedDiagnostics));
    }

    [Fact]
    public async Task V5CatalogStaysSilentOnNeutralCode()
    {
        const string source = "class Neutral { int Value => 42; }";
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
        var compilation = CSharpCompilation.Create(
            "V5MatrixAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = await compilation.WithAnalyzers(V5AnalyzerCatalog.All).GetAnalyzerDiagnosticsAsync();

        Assert.Empty(diagnostics);
    }
}
