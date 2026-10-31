using Microsoft.CodeAnalysis.CSharp;
using StrictSharp.Analyzers.V5;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class V5LoopCostInferenceTests
{
    [Fact]
    public void ClassifiesSingleLoopAsLinear()
    {
        var method = ParseMethod("void M(int[] items) { foreach (var item in items) { _ = item; } }");

        var evidence = LoopCostInference.Analyze(method, CancellationToken.None);

        var item = Assert.Single(evidence.Items);
        Assert.Equal(ComplexityKind.Linear, item.Complexity.Kind);
        Assert.False(item.Count.IsKnown);
    }

    [Fact]
    public void ClassifiesNestedLoopsAsQuadratic()
    {
        var method = ParseMethod("void M(int[] items) { foreach (var outer in items) foreach (var inner in items) _ = outer + inner; }");

        var evidence = LoopCostInference.Analyze(method, CancellationToken.None);

        var item = Assert.Single(evidence.Items);
        Assert.Equal(ComplexityKind.Quadratic, item.Complexity.Kind);
    }

    [Fact]
    public void DerivesConstantForLoopMultiplierWhenBoundsAreLiteral()
    {
        var method = ParseMethod("void M() { for (var i = 0; i < 10; i++) _ = i; }");

        var evidence = LoopCostInference.Analyze(method, CancellationToken.None);

        var item = Assert.Single(evidence.Items);
        Assert.Equal(10, item.Count.Value);
        Assert.True(item.Count.IsKnown);
        Assert.Equal(10, item.Multiplier.Value);
    }

    [Fact]
    public void KeepsUnsupportedDepthUnknown()
    {
        var method = ParseMethod("void M(int[] items) { foreach (var a in items) foreach (var b in items) foreach (var c in items) _ = a + b + c; }");

        var evidence = LoopCostInference.Analyze(method, CancellationToken.None);

        var item = Assert.Single(evidence.Items);
        Assert.Equal(ComplexityKind.Unknown, item.Complexity.Kind);
    }

    [Fact]
    public void ReturnsEmptyForMethodWithoutLoopsOrSorts()
    {
        var method = ParseMethod("void M() { var value = 42; }");

        var evidence = LoopCostInference.Analyze(method, CancellationToken.None);

        Assert.True(evidence.IsEmpty);
        Assert.Equal(ComplexityKind.Constant, LoopCostInference.Classify(method).Kind);
    }

    [Fact]
    public void ClassifiesKnownSortAsLinearithmic()
    {
        var method = ParseMethod("void M(System.Collections.Generic.List<int> items) { items.Sort(); }");

        var evidence = LoopCostInference.Analyze(method, CancellationToken.None);
        var item = Assert.Single(evidence.Items);

        Assert.Equal(ComplexityKind.Linearithmic, item.Complexity.Kind);
        Assert.False(item.Count.IsKnown);
    }

    [Fact]
    public void KeepsAssignmentInitializerAndInvalidBoundsUnknown()
    {
        var method = ParseMethod("void M(int end) { for (var i = 1; i < end; i++) _ = i; }");

        var evidence = LoopCostInference.Analyze(method, CancellationToken.None);
        var item = Assert.Single(evidence.Items);

        Assert.Equal(ComplexityKind.Linear, item.Complexity.Kind);
        Assert.False(item.Count.IsKnown);
    }

    private static Microsoft.CodeAnalysis.SyntaxNode ParseMethod(string source)
    {
        return CSharpSyntaxTree.ParseText($"class Sample {{ {source} }}", new CSharpParseOptions(LanguageVersion.CSharp14))
            .GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Single();
    }
}
