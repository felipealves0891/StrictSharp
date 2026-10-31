using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Analyzers;
using StrictSharp.Analyzers.Rules;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class V4TestMatrixTests
{
    [Fact]
    public void V4CatalogHasExactlyPerf100()
    {
        var actualIds = V4AnalyzerCatalog.All
            .SelectMany(analyzer => analyzer.SupportedDiagnostics)
            .Select(descriptor => descriptor.Id);

        Assert.Equal(RuleCatalog.V4.Select(rule => rule.Id), actualIds);
        Assert.Equal(new[] { "PERF100" }, actualIds);
        Assert.All(V4AnalyzerCatalog.All, analyzer => Assert.Single(analyzer.SupportedDiagnostics));
    }

    [Fact]
    public async Task V4CatalogAcceptsCSharp14AndStaysSilentOnNeutralCode()
    {
        const string source = "class Neutral { int Value => 42; }";
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
        var compilation = CSharpCompilation.Create(
            "V4MatrixAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = await compilation.WithAnalyzers(V4AnalyzerCatalog.All).GetAnalyzerDiagnosticsAsync();

        Assert.Empty(diagnostics);
    }
}
