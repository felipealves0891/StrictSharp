using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using StrictSharp.Analyzers.V5;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class V5AllocationPayloadInferenceTests
{
    [Fact]
    public void CalculatesKnownPayloadAndLoopMultiplier()
    {
        var (method, model) = ParseMethod("void M() { for (var i = 0; i < 1000; i++) { var buffer = new byte[100_000]; } }");

        var evidence = AllocationPayloadInference.Analyze(method, model, CancellationToken.None);
        var item = Assert.Single(evidence.Items);

        Assert.Equal(1000, item.Count.Value);
        Assert.Equal(100_000_000, item.Payload.Bytes);
        Assert.True(item.Payload.IsKnown);
        Assert.False(item.Payload.IncludesObjectOverhead);
    }

    [Fact]
    public void KeepsTotalPayloadUnknownWhenLoopBoundIsDynamic()
    {
        var (method, model) = ParseMethod("void M(int count) { for (var i = 0; i < count; i++) { var buffer = new byte[100]; } }");

        var evidence = AllocationPayloadInference.Analyze(method, model, CancellationToken.None);
        var item = Assert.Single(evidence.Items);

        Assert.False(item.Count.IsKnown);
        Assert.False(item.Payload.IsKnown);
    }

    [Fact]
    public void ReportsUnknownForReferenceArrayAndDynamicSize()
    {
        var (method, model) = ParseMethod("void M(int count) { var names = new string[count]; var values = new int[count]; }");

        var evidence = AllocationPayloadInference.Analyze(method, model, CancellationToken.None);

        Assert.Equal(2, evidence.Items.Length);
        Assert.All(evidence.Items, item => Assert.False(item.Payload.IsKnown));
    }

    [Fact]
    public void CoversSupportedPrimitivePayloadSizes()
    {
        var (method, model) = ParseMethod("void M() { var a = new bool[1]; var b = new char[1]; var c = new int[1]; var d = new long[1]; var e = new decimal[1]; }");

        var evidence = AllocationPayloadInference.Analyze(method, model, CancellationToken.None);

        Assert.Equal(5, evidence.Items.Length);
        Assert.All(evidence.Items, item => Assert.True(item.Payload.IsKnown));
    }

    [Fact]
    public void KeepsPayloadUnknownWhenLoopHasZeroIterations()
    {
        var (method, model) = ParseMethod("void M() { for (var i = 0; i < 0; i++) { var buffer = new byte[10]; } }");

        var evidence = AllocationPayloadInference.Analyze(method, model, CancellationToken.None);
        var item = Assert.Single(evidence.Items);

        Assert.False(item.Payload.IsKnown);
    }

    [Fact]
    public void KeepsOverflowingArrayPayloadUnknown()
    {
        var (method, model) = ParseMethod("void M() { var buffer = new decimal[9223372036854775807]; }");

        var evidence = AllocationPayloadInference.Analyze(method, model, CancellationToken.None);
        var item = Assert.Single(evidence.Items);

        Assert.False(item.Payload.IsKnown);
    }

    private static (Microsoft.CodeAnalysis.SyntaxNode Method, SemanticModel Model) ParseMethod(string methodSource)
    {
        var tree = CSharpSyntaxTree.ParseText($"class Sample {{ {methodSource} }}", new CSharpParseOptions(LanguageVersion.CSharp14));
        var compilation = CSharpCompilation.Create(
            "AllocationTestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var model = compilation.GetSemanticModel(tree);
        var method = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().Single();
        return (method, model);
    }
}
