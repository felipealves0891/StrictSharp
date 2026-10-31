using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using StrictSharp.Analyzers.V5;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class V5IoConcurrencyInferenceTests
{
    [Fact]
    public void ReportsKnownIoMultiplierInConstantLoop()
    {
        var (method, model) = ParseMethod("async void M(System.Net.Http.HttpClient client) { for (var i = 0; i < 3; i++) await client.GetAsync(\"https://example.test\"); }");

        var evidence = IoConcurrencyInference.Analyze(method, model, CancellationToken.None);
        var item = Assert.Single(evidence.Items);

        Assert.Equal(CostDimension.Io, item.Dimension);
        Assert.Equal(3, item.Count.Value);
        Assert.Contains("latência", item.Limitations, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReportsTaskCreationWithoutInferringRuntimeCapacity()
    {
        var (method, model) = ParseMethod("void M() { System.Threading.Tasks.Task.Run(() => { }); }");

        var evidence = IoConcurrencyInference.Analyze(method, model, CancellationToken.None);
        var item = Assert.Single(evidence.Items);

        Assert.Equal(CostDimension.Concurrency, item.Dimension);
        Assert.Equal(1, item.Count.Value);
        Assert.Contains("runtime", item.Limitations, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KeepsIoCountUnknownForDynamicLoopBounds()
    {
        var (method, model) = ParseMethod("void M(System.Net.Http.HttpClient client, int count) { for (var i = 0; i < count; i++) client.GetAsync(\"https://example.test\"); }");

        var evidence = IoConcurrencyInference.Analyze(method, model, CancellationToken.None);
        var item = Assert.Single(evidence.Items);

        Assert.False(item.Count.IsKnown);
        Assert.True(item.HasUnknownBoundary);
    }

    private static (Microsoft.CodeAnalysis.SyntaxNode Method, SemanticModel Model) ParseMethod(string methodSource)
    {
        var tree = CSharpSyntaxTree.ParseText($"class Sample {{ {methodSource} }}", new CSharpParseOptions(LanguageVersion.CSharp14));
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Net.Http.HttpClient).Assembly.Location)
        };
        var compilation = CSharpCompilation.Create(
            "IoConcurrencyTestAssembly",
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var model = compilation.GetSemanticModel(tree);
        var method = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().Single();
        return (method, model);
    }
}
