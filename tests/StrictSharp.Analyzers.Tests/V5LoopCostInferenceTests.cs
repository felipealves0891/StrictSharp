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
        var (method, model) = CompileMethod("void M() { for (var i = 0; i < 10; i++) _ = i; }");

        var evidence = LoopCostInference.Analyze(method, CancellationToken.None, model);

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
        var (method, model) = CompileMethod("void M(int end) { for (var i = 1; i < end; i++) _ = i; }");

        var evidence = LoopCostInference.Analyze(method, CancellationToken.None, model);
        var item = Assert.Single(evidence.Items);

        Assert.Equal(ComplexityKind.Linear, item.Complexity.Kind);
        Assert.False(item.Count.IsKnown);
    }

    [Fact]
    public void SupportsAssignmentInitializerAndDescendingBounds()
    {
        var (assignmentMethod, assignmentModel) = CompileMethod("void M() { var i = 0; for (i = 0; i < 3; i++) _ = i; }");
        var assignment = assignmentMethod
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax>()
            .Single();
        var (emptyMethod, emptyModel) = CompileMethod("void M() { for (var i = 3; i < 1; i++) _ = i; }");
        var empty = emptyMethod
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax>()
            .Single();
        var (descendingMethod, descendingModel) = CompileMethod("void M() { for (var i = 5; i > 0; i--) _ = i; }");
        var descending = descendingMethod
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax>()
            .Single();
        var (emptyDescendingMethod, emptyDescendingModel) = CompileMethod("void M() { for (var i = 0; i > 5; i--) _ = i; }");
        var emptyDescending = emptyDescendingMethod
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax>()
            .Single();

        Assert.True(LoopCostInference.TryGetKnownIterationCount(assignment, assignmentModel, out var count));
        Assert.Equal(3, count);
        Assert.True(LoopCostInference.TryGetKnownIterationCount(empty, emptyModel, out var emptyCount));
        Assert.Equal(0, emptyCount);
        Assert.True(LoopCostInference.TryGetKnownIterationCount(descending, descendingModel, out var descendingCount));
        Assert.Equal(5, descendingCount);
        Assert.True(LoopCostInference.TryGetKnownIterationCount(emptyDescending, emptyDescendingModel, out var emptyDescendingCount));
        Assert.Equal(0, emptyDescendingCount);
    }

    [Theory]
    [InlineData("for (var i = 0; i <= 5; i++) _ = i;")]
    [InlineData("for (var i = 0; i < 10; i += 2) _ = i;")]
    [InlineData("for (var i = 0; i < 10; i--) _ = i;")]
    [InlineData("for (var i = 0; i < 10; i++, i++) _ = i;")]
    [InlineData("for (var i = 0; i < 10; j++) _ = i;")]
    [InlineData("for (var i = 0; j < 10; i++) _ = i;")]
    public void KeepsIterationCountUnknownWhenLoopShapeIsNotProven(string loop)
    {
        var (method, model) = CompileMethod($"void M() {{ {loop} }}");
        var statement = method
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax>()
            .Single();

        Assert.False(LoopCostInference.TryGetKnownIterationCount(statement, model, out _));
    }

    [Fact]
    public void KeepsCountUnknownWhenTheIntegralTypeCouldOverflowTheBound()
    {
        var (method, model) = CompileMethod("void M() { for (byte i = 0; i < 256; i++) _ = i; }");
        var statement = method
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax>()
            .Single();

        Assert.False(LoopCostInference.TryGetKnownIterationCount(statement, model, out _));
    }

    [Fact]
    public void SupportsLongBoundsAndNegativeStartingValues()
    {
        var (method, model) = CompileMethod("void M() { for (long i = -5; i < 2; ++i) _ = i; }");
        var statement = method
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax>()
            .Single();

        Assert.True(LoopCostInference.TryGetKnownIterationCount(statement, model, out var count));
        Assert.Equal(7, count);
    }

    [Fact]
    public void KeepsCountUnknownWhenIntBoundIsOutsideItsRange()
    {
        var (method, model) = CompileMethod("void M() { for (int i = 0; i < 2147483648; i++) _ = i; }");
        var statement = method
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax>()
            .Single();

        Assert.False(LoopCostInference.TryGetKnownIterationCount(statement, model, out _));
    }

    [Fact]
    public void KeepsCountUnknownWhenSubtractingLongBoundsWouldOverflow()
    {
        var (method, model) = CompileMethod("void M() { for (long i = -9223372036854775807L; i < 9223372036854775807L; i++) _ = i; }");
        var statement = method
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax>()
            .Single();

        Assert.False(LoopCostInference.TryGetKnownIterationCount(statement, model, out _));
    }

    [Theory]
    [InlineData("void M() { for (int i = 0, j = 0; i < 3; i++) _ = i + j; }")]
    [InlineData("void M() { int i; for (i++; i < 3; i++) _ = i; }")]
    [InlineData("void M(int[] values) { for (values[0] = 0; values[0] < 3; values[0]++) _ = values[0]; }")]
    [InlineData("void M() { for (int i; i < 3; i++) _ = i; }")]
    public void KeepsCountUnknownForUnsupportedInitializerShapes(string source)
    {
        var (method, model) = CompileMethod(source);
        var statement = method
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax>()
            .Single();

        Assert.False(LoopCostInference.TryGetKnownIterationCount(statement, model, out _));
    }

    [Fact]
    public void KeepsNestedMultiplierUnknownWhenTheProductOverflows()
    {
        var (method, model) = CompileMethod("void M() { for (long i = 0; i < 4000000000L; i++) for (long j = 0; j < 4000000000L; j++) _ = i + j; }");

        var item = Assert.Single(LoopCostInference.Analyze(method, CancellationToken.None, model).Items);

        Assert.False(item.Multiplier.IsKnown);
    }

    [Fact]
    public void KeepsNestedMultiplierUnknownWhenOneLoopHasDynamicBounds()
    {
        var (method, model) = CompileMethod("void M(int count) { for (var i = 0; i < 3; i++) for (var j = 0; j < count; j++) _ = i + j; }");

        var item = Assert.Single(LoopCostInference.Analyze(method, CancellationToken.None, model).Items);

        Assert.False(item.Multiplier.IsKnown);
    }

    [Fact]
    public void ReportsZeroNestedMultiplierWhenAConstantLoopIsEmpty()
    {
        var (method, model) = CompileMethod("void M() { for (var i = 0; i < 0; i++) for (var j = 0; j < 3; j++) _ = i + j; }");

        var item = Assert.Single(LoopCostInference.Analyze(method, CancellationToken.None, model).Items);

        Assert.True(item.Multiplier.IsKnown);
        Assert.Equal(0, item.Multiplier.Value);
    }

    [Theory]
    [InlineData("++i", 4)]
    [InlineData("i--", 4)]
    [InlineData("--i", 4)]
    public void SupportsPrefixAndPostfixUnitUpdates(string update, long expected)
    {
        var source = update.Contains("--", StringComparison.Ordinal)
            ? "void M() { for (var i = 4; i > 0; " + update + ") _ = i; }"
            : "void M() { for (var i = 0; i < 4; " + update + ") _ = i; }";
        var (method, model) = CompileMethod(source);
        var statement = method
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax>()
            .Single();

        Assert.True(LoopCostInference.TryGetKnownIterationCount(statement, model, out var count));
        Assert.Equal(expected, count);
    }

    [Fact]
    public void KeepsCountUnknownWhenInitializerIsNotALiteral()
    {
        var (method, model) = CompileMethod("void M(int start) { for (var i = start; i < 10; i++) _ = i; }");
        var statement = method
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax>()
            .Single();

        Assert.False(LoopCostInference.TryGetKnownIterationCount(statement, model, out _));
    }

    private static (Microsoft.CodeAnalysis.SyntaxNode Method, Microsoft.CodeAnalysis.SemanticModel Model) CompileMethod(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(
            $"class Sample {{ {source} }}",
            new CSharpParseOptions(LanguageVersion.CSharp14));
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            "V5LoopCostInferenceTests",
            [tree],
            [Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));
        var method = tree.GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Single();
        return (method, compilation.GetSemanticModel(tree));
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
