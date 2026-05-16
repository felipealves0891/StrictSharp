using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class BigOAnalyzerStage3Tests
{
    [Fact]
    public async Task ForLoopWithParameterLimit_ShouldReportLinearComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Sum(int n)
                {
                    var total = 0;
                    for (var i = 0; i < n; i++)
                    {
                        total += i;
                    }

                    return total;
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(n)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task WhileLoopWithParameterLimit_ShouldReportLinearComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Sum(int n)
                {
                    var total = 0;
                    var i = 0;
                    while (i < n)
                    {
                        total += i;
                        i++;
                    }

                    return total;
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(n)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ForeachOverArrayParameter_ShouldReportLinearComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Sum(int[] values)
                {
                    var total = 0;
                    foreach (var value in values)
                    {
                        total += value;
                    }

                    return total;
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(n)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ForLoopWithConstantLimit_ShouldNotReportDiagnostic()
    {
        const string code = """
            public sealed class Sample
            {
                public int Sum()
                {
                    var total = 0;
                    for (var i = 0; i < 4; i++)
                    {
                        total += i;
                    }

                    return total;
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(code);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task WhileLoopWithoutInferableLimit_ShouldNotReportDiagnostic()
    {
        const string code = """
            public sealed class Sample
            {
                public int Read()
                {
                    var total = 0;
                    while (HasNext())
                    {
                        total++;
                    }

                    return total;
                }

                private bool HasNext()
                {
                    return false;
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(code);

        Assert.Empty(diagnostics);
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
            "BigOAnalyzerStage3Tests",
            new[] { CSharpSyntaxTree.ParseText(code) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
