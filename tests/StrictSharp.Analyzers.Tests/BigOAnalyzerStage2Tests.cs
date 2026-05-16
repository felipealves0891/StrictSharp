using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class BigOAnalyzerStage2Tests
{
    private static readonly Type AnalyzerType = typeof(AllocationInsideLoopAnalyzer)
        .Assembly
        .GetType("StrictSharp.Analyzers.BigOAnalyzer")!;

    [Fact]
    public async Task MethodWithOnlyConstantOperations_ShouldNotReportDiagnostic()
    {
        const string code = """
            public sealed class Sample
            {
                public int Calculate(int value)
                {
                    var next = value + 1;
                    var doubled = next * 2;
                    return doubled;
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(code);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task IfWithConstantBranches_ShouldNotReportDiagnostic()
    {
        const string code = """
            public sealed class Sample
            {
                public int Calculate(int value)
                {
                    if (value > 0)
                    {
                        return value + 1;
                    }

                    return value - 1;
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(code);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ElseIfWithOnlyConstantBranches_ShouldNotReportDiagnostic()
    {
        const string code = """
            public sealed class Sample
            {
                public int Calculate(int value)
                {
                    if (value > 10)
                    {
                        return value + 1;
                    }

                    if (value > 0)
                    {
                        return value;
                    }

                    return value - 1;
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(code);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ConstantMethod_ShouldCompileWithoutAnalyzerDiagnostic()
    {
        const string code = """
            public sealed class Sample
            {
                public bool IsPositive(int value)
                {
                    var result = value > 0;
                    return result;
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(code);

        Assert.Empty(diagnostics);
    }

    private static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(string code)
    {
        var analyzer = CreateAnalyzer();
        var compilation = CreateCompilation(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private static DiagnosticAnalyzer CreateAnalyzer()
    {
        Assert.NotNull(AnalyzerType);

        return Assert.IsAssignableFrom<DiagnosticAnalyzer>(Activator.CreateInstance(AnalyzerType));
    }

    private static CSharpCompilation CreateCompilation(string code)
    {
        var references = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location));

        return CSharpCompilation.Create(
            "BigOAnalyzerStage2Tests",
            new[] { CSharpSyntaxTree.ParseText(code) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
