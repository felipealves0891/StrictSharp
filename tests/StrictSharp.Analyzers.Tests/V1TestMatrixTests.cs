using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Analyzers;
using StrictSharp.Analyzers.Rules;
using System.Collections.Immutable;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class V1TestMatrixTests
{
    [Fact]
    public void V1CatalogMatchesTheFifteenImplementedRules()
    {
        var expectedIds = new List<string>();
        foreach (var rule in RuleCatalog.V1)
            expectedIds.Add(rule.Id);

        var actualIds = new List<string>();
        foreach (var analyzer in V1AnalyzerCatalog.All)
        foreach (var descriptor in analyzer.SupportedDiagnostics)
            actualIds.Add(descriptor.Id);

        Assert.Equal(expectedIds, actualIds);
        Assert.Equal(15, V1AnalyzerCatalog.All.Length);
        Assert.All(V1AnalyzerCatalog.All, analyzer => Assert.Single(analyzer.SupportedDiagnostics));
    }

    [Fact]
    public async Task AllV1AnalyzersAcceptCSharp14AndStaySilentOnNeutralCode()
    {
        const string source = """
            class Neutral
            {
                int Value => 42;
            }
            """;
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
        var compilation = CSharpCompilation.Create(
            "V1MatrixAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = await compilation
            .WithAnalyzers(V1AnalyzerCatalog.All)
            .GetAnalyzerDiagnosticsAsync();

        Assert.Equal(LanguageVersion.CSharp14, ((CSharpParseOptions)tree.Options).LanguageVersion);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void EveryV1RuleHasAStaticValidationEntryPoint()
    {
        var requiredIds = ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "PERF001", "PERF003", "PERF007", "PERF008", "PERF012", "PERF013", "PERF014", "PERF015",
            "PERF018", "PERF019", "PERF020", "PERF022", "PERF025", "PERF026", "PERF027");
        var actualIds = V1AnalyzerCatalog.All
            .SelectMany(analyzer => analyzer.SupportedDiagnostics)
            .Select(descriptor => descriptor.Id)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(requiredIds, actualIds);
    }
}
