using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class BigOAnalyzerStage7Tests
{
    [Fact]
    public async Task DirectRecursionWithSingleDecrement_ShouldReportLinearComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int n)
                {
                    if (n <= 0)
                    {
                        return 0;
                    }

                    return Count(n - 1) + 1;
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(n)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task BinaryRecursiveFibonacci_ShouldReportExponentialComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Fibonacci(int n)
                {
                    if (n <= 1)
                    {
                        return n;
                    }

                    return Fibonacci(n - 1) + Fibonacci(n - 2);
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(c^n)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task DirectRecursionDividingInputByTwo_ShouldReportLogarithmicComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int n)
                {
                    if (n <= 1)
                    {
                        return 0;
                    }

                    return Count(n / 2) + 1;
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(log n)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task MutualRecursion_ShouldNotReportDiagnostic()
    {
        const string code = """
            public sealed class Sample
            {
                public int Even(int n)
                {
                    if (n == 0)
                    {
                        return 1;
                    }

                    return Odd(n - 1);
                }

                public int Odd(int n)
                {
                    if (n == 0)
                    {
                        return 0;
                    }

                    return Even(n - 1);
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
            "BigOAnalyzerStage7Tests",
            new[] { CSharpSyntaxTree.ParseText(code) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
