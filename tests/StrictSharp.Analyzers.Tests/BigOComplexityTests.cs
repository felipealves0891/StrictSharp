using System.Reflection;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class BigOComplexityTests
{
    private static readonly Assembly AnalyzerAssembly = typeof(AllocationInsideLoopAnalyzer).Assembly;

    [Fact]
    public void Constant_ShouldBeLessThanLinear()
    {
        var constant = GetComplexity("Constant");
        var linear = GetComplexity("Linear");

        Assert.True(Compare(constant, linear) < 0);
    }

    [Fact]
    public void Linear_ShouldBeLessThanQuadratic()
    {
        var linear = GetComplexity("Linear");
        var quadratic = GetComplexity("Quadratic");

        Assert.True(Compare(linear, quadratic) < 0);
    }

    [Fact]
    public void AddSequential_ShouldKeepHighestTerm()
    {
        var constant = GetComplexity("Constant");
        var linear = GetComplexity("Linear");

        var result = InvokeStatic("AddSequential", constant, linear);

        Assert.Equal("O(n)", result.ToString());
    }

    [Fact]
    public void Multiply_ShouldRepresentNestedLoops()
    {
        var linear = GetComplexity("Linear");

        var result = InvokeStatic("Multiply", linear, linear);

        Assert.Equal("O(n^2)", result.ToString());
    }

    [Fact]
    public void Unknown_ShouldFormatAsUnknown()
    {
        var unknown = GetComplexity("Unknown");

        Assert.Equal("O(?)", unknown.ToString());
    }

    private static object GetComplexity(string name)
    {
        var type = GetComplexityType();
        var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(property);

        return property.GetValue(null)!;
    }

    private static int Compare(object left, object right)
    {
        var type = GetComplexityType();
        var method = type.GetMethod("CompareTo", new[] { type });

        Assert.NotNull(method);

        return (int)method.Invoke(left, new[] { right })!;
    }

    private static object InvokeStatic(string name, object left, object right)
    {
        var type = GetComplexityType();
        var method = type.GetMethod(
            name,
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { type, type },
            null);

        Assert.NotNull(method);

        return method.Invoke(null, new[] { left, right })!;
    }

    private static Type GetComplexityType()
    {
        var type = AnalyzerAssembly.GetType("StrictSharp.Analyzers.BigOComplexity");

        Assert.NotNull(type);

        return type;
    }
}
