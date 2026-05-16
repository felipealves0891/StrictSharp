using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class BigOAnalyzerStage4Tests
{
    [Fact]
    public async Task NestedForLoopsWithSameLimit_ShouldReportQuadraticComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int n)
                {
                    var total = 0;
                    for (var i = 0; i < n; i++)
                    {
                        for (var j = 0; j < n; j++)
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
        Assert.Contains("O(n^2)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task NestedForLoopsWithDifferentLimits_ShouldReportProductComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int n, int m)
                {
                    var total = 0;
                    for (var i = 0; i < n; i++)
                    {
                        for (var j = 0; j < m; j++)
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
        Assert.Contains("O(n*m)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task ThreeNestedForLoopsWithSameLimit_ShouldReportPolynomialComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int n)
                {
                    var total = 0;
                    for (var i = 0; i < n; i++)
                    {
                        for (var j = 0; j < n; j++)
                        {
                            for (var k = 0; k < n; k++)
                            {
                                total++;
                            }
                        }
                    }

                    return total;
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(n^c)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task NestedLoopInsideIf_ShouldReportHighestBranchComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int n, bool enabled)
                {
                    var total = 0;
                    if (enabled)
                    {
                        for (var i = 0; i < n; i++)
                        {
                            for (var j = 0; j < n; j++)
                            {
                                total++;
                            }
                        }
                    }

                    return total;
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(n^2)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task SequentialLoops_ShouldReportLinearComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int n)
                {
                    var total = 0;
                    for (var i = 0; i < n; i++)
                    {
                        total++;
                    }

                    for (var j = 0; j < n; j++)
                    {
                        total++;
                    }

                    return total;
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(n)", diagnostic.GetMessage());
        Assert.DoesNotContain("O(n^2)", diagnostic.GetMessage());
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
            "BigOAnalyzerStage4Tests",
            new[] { CSharpSyntaxTree.ParseText(code) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
