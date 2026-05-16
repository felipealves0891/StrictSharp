using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class BigOAnalyzerStage9Tests
{
    [Fact]
    public async Task Switch_ShouldUseHighestSectionComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int n, int mode)
                {
                    var total = 0;
                    switch (mode)
                    {
                        case 0:
                            return total;
                        default:
                            for (var i = 0; i < n; i++)
                            {
                                total++;
                            }

                            return total;
                    }
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(n)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task LinqWhereSelect_ShouldReportLinearComplexity()
    {
        const string code = """
            using System.Collections.Generic;
            using System.Linq;

            public sealed class Sample
            {
                public IEnumerable<int> Filter(int[] values)
                {
                    return values.Where(value => value > 0).Select(value => value + 1);
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(n)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task LinqAny_ShouldReportWorstCaseLinearComplexity()
    {
        const string code = """
            using System.Linq;

            public sealed class Sample
            {
                public bool HasPositive(int[] values)
                {
                    return values.Any(value => value > 0);
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(n)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task LinqFirstOrDefault_ShouldReportWorstCaseLinearComplexity()
    {
        const string code = """
            using System.Linq;

            public sealed class Sample
            {
                public int Find(int[] values)
                {
                    return values.FirstOrDefault(value => value > 0);
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(n)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task CollectionCountProperty_ShouldNotReportDiagnostic()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int[] values)
                {
                    return values.Length;
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(code);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task LinqCountExtension_ShouldReportLinearComplexity()
    {
        const string code = """
            using System.Collections.Generic;
            using System.Linq;

            public sealed class Sample
            {
                public int Count(IEnumerable<int> values)
                {
                    return values.Count();
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(n)", diagnostic.GetMessage());
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
            "BigOAnalyzerStage9Tests",
            new[] { CSharpSyntaxTree.ParseText(code) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
