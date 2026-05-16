using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class BigOAnalyzerStage8Tests
{
    private const string MinimumComplexityOption = "strictsharp_big_o_minimum_complexity";

    [Fact]
    public async Task DefaultPolicy_ShouldNotReportConstantComplexity()
    {
        const string code = """
            public sealed class Sample
            {
                public int Count(int n)
                {
                    var value = n + 1;
                    return value;
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(code);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MinimumQuadraticConfiguration_ShouldSuppressLinearDiagnostic()
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

                    return total;
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(code, "O(n^2)");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MinimumLinearConfiguration_ShouldReportLinearDiagnostic()
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

                    return total;
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code, "O(n)");

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(n)", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Diagnostic_ShouldPointToLoopThatDefinesComplexity()
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

                    return total;
                }
            }
            """;

        var diagnostic = await GetSingleAnalyzerDiagnosticAsync(code, "O(n)");
        var locationText = diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan);

        Assert.Equal(BigOAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("O(n)", diagnostic.GetMessage());
        Assert.StartsWith("for", locationText.TrimStart());
    }

    private static async Task<Diagnostic> GetSingleAnalyzerDiagnosticAsync(
        string code,
        string? minimumComplexity = null)
    {
        var diagnostics = await GetAnalyzerDiagnosticsAsync(code, minimumComplexity);

        return Assert.Single(diagnostics);
    }

    private static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(
        string code,
        string? minimumComplexity = null)
    {
        var compilation = CreateCompilation(code);
        var analyzerOptions = CreateAnalyzerOptions(minimumComplexity);
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new BigOAnalyzer()),
            analyzerOptions);

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private static AnalyzerOptions CreateAnalyzerOptions(string? minimumComplexity)
    {
        if (minimumComplexity == null)
            return new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);

        var options = ImmutableDictionary<string, string>.Empty.Add(MinimumComplexityOption, minimumComplexity);
        return new AnalyzerOptions(
            ImmutableArray<AdditionalText>.Empty,
            new TestAnalyzerConfigOptionsProvider(options));
    }

    private static CSharpCompilation CreateCompilation(string code)
    {
        var references = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location));

        return CSharpCompilation.Create(
            "BigOAnalyzerStage8Tests",
            new[] { CSharpSyntaxTree.ParseText(code) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _options;

        public TestAnalyzerConfigOptionsProvider(ImmutableDictionary<string, string> options)
        {
            _options = new TestAnalyzerConfigOptions(options);
        }

        public override AnalyzerConfigOptions GlobalOptions
        {
            get { return _options; }
        }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return _options;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return _options;
        }
    }

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly ImmutableDictionary<string, string> _options;

        public TestAnalyzerConfigOptions(ImmutableDictionary<string, string> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, out string value)
        {
            if (_options.TryGetValue(key, out var configuredValue))
            {
                value = configuredValue;
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}
