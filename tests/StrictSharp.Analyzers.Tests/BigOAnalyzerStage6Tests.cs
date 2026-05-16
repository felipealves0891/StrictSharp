using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class BigOAnalyzerStage6Tests
{
    [Fact]
    public async Task MethodCallingConstantLocalMethod_ShouldNotReportDiagnostic()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int value)
                {
                    return Normalize(value);
                }

                private int Normalize(int value)
                {
                    return value + 1;
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(code);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task LinearLoopCallingLinearLocalMethod_ShouldReportQuadraticComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int n)
                {
                    var total = 0;
                    for (var i = 0; i < n; i++)
                    {
                        total += Sum(n);
                    }

                    return total;
                }

                private int Sum(int n)
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

        var diagnostics = await GetAnalyzerDiagnosticsAsync(code);

        Assert.Contains(diagnostics, diagnostic => diagnostic.GetMessage().Contains("O(n^2)"));
    }

    [Fact]
    public async Task ExternalMethodCall_ShouldNotReportDiagnostic()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int value)
                {
                    return System.Math.Abs(value);
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(code);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DirectRecursion_ShouldNotBeAnalyzedInStage6()
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

        var diagnostics = await GetAnalyzerDiagnosticsAsync(code);

        Assert.Empty(diagnostics);
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
            "BigOAnalyzerStage6Tests",
            new[] { CSharpSyntaxTree.ParseText(code) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
