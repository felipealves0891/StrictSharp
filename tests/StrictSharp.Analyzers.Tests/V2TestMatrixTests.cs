using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Analyzers;
using StrictSharp.Analyzers.Rules;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class V2TestMatrixTests
{
    [Fact]
    public void V2CatalogHasExactlyTheThirteenStableIds()
    {
        var actualIds = V2AnalyzerCatalog.All
            .SelectMany(analyzer => analyzer.SupportedDiagnostics)
            .Select(descriptor => descriptor.Id);

        Assert.Equal(RuleCatalog.V2.Select(rule => rule.Id), actualIds);
        Assert.Equal(13, actualIds.Distinct(StringComparer.Ordinal).Count());
        Assert.All(V2AnalyzerCatalog.All, analyzer => Assert.Single(analyzer.SupportedDiagnostics));
    }

    [Fact]
    public async Task V2CatalogAcceptsCSharp14AndStaysSilentOnNeutralCode()
    {
        const string source = "class Neutral { int Value => 42; }";
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
        var compilation = CSharpCompilation.Create(
            "V2MatrixAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = await compilation.WithAnalyzers(V2AnalyzerCatalog.All).GetAnalyzerDiagnosticsAsync();

        Assert.Empty(diagnostics);
    }
}
