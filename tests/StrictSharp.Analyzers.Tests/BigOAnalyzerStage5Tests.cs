using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class BigOAnalyzerStage5Tests
{
    [Fact]
    public async Task ForLoopDividingIndex_ShouldReportLogarithmicComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int n)
                {
                    var total = 0;
                    for (var i = n; i > 0; i /= 2)
                    {
                        total++;
                    }

                    return total;
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(log n)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task WhileLoopDividingInput_ShouldReportLogarithmicComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int n)
                {
                    var total = 0;
                    while (n > 1)
                    {
                        total++;
                        n /= 2;
                    }

                    return total;
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(log n)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ForLoopMultiplyingIndex_ShouldReportLogarithmicComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int n)
                {
                    var total = 0;
                    for (var i = 1; i < n; i *= 2)
                    {
                        total++;
                    }

                    return total;
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(log n)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task LogarithmicLoopInsideLinearLoop_ShouldReportLinearithmicComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int n)
                {
                    var total = 0;
                    for (var i = 0; i < n; i++)
                    {
                        for (var j = n; j > 0; j /= 2)
                        {
                            total++;
                        }
                    }

                    return total;
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(n log n)", diagnostic.GetMessage());
    }

    private static async Task<Diagnostic> GetSingleAnalyzerDiagnosticAsync(string code)
    {
        var diagnostics = await GetAnalyzerDiagnosticsAsync(code);

        return Assert.Single(diagnostics);
    }

    private static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(string code)
    {
        var compilation = CreateCompilation(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BigOAnalyzer()));

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private static CSharpCompilation CreateCompilation(string code)
    {
        var references = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location));

        return CSharpCompilation.Create(
            "BigOAnalyzerStage5Tests",
            new[] { CSharpSyntaxTree.ParseText(code) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
