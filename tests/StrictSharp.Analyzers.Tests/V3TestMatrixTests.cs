using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Analyzers;
using StrictSharp.Analyzers.Rules;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class V3TestMatrixTests
{
    [Fact]
    public void V3CatalogHasExactlyTheTwoStableIds()
    {
        var actualIds = V3AnalyzerCatalog.All
            .SelectMany(analyzer => analyzer.SupportedDiagnostics)
            .Select(descriptor => descriptor.Id);

        Assert.Equal(RuleCatalog.V3.Select(rule => rule.Id), actualIds);
        Assert.Equal(new[] { "PERF011", "PERF030" }, actualIds);
        Assert.All(V3AnalyzerCatalog.All, analyzer => Assert.Single(analyzer.SupportedDiagnostics));
    }

    [Fact]
    public async Task V3CatalogAcceptsCSharp14AndStaysSilentOnNeutralCode()
    {
        const string source = "class Neutral { int Value => 42; }";
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
        var compilation = CSharpCompilation.Create(
            "V3MatrixAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = await compilation.WithAnalyzers(V3AnalyzerCatalog.All).GetAnalyzerDiagnosticsAsync();

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void V1AndV2CatalogsRemainSeparatedFromV3()
    {
        Assert.DoesNotContain(V1AnalyzerCatalog.All, analyzer =>
            RuleCatalog.Get(analyzer.SupportedDiagnostics[0].Id).Phase == RulePhase.V3);
        Assert.DoesNotContain(V2AnalyzerCatalog.All, analyzer =>
            RuleCatalog.Get(analyzer.SupportedDiagnostics[0].Id).Phase == RulePhase.V3);
    }
}
