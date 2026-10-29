using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace StrictSharp.Analyzers.Tests;

internal static class AnalyzerTestHarness
{
    public static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
        string source,
        DiagnosticAnalyzer analyzer)
    {
        var tree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.CSharp14));
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(File).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Net.Http.HttpClient).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Text.RegularExpressions.Regex).Assembly.Location)
        };
        var compilation = CSharpCompilation.Create(
            "AnalyzerTestAssembly",
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return await compilation
            .WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync();
    }
}
