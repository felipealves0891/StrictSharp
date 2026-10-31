using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using StrictSharp.Analyzers.Interprocedural;
using StrictSharp.Analyzers.V5;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class InterproceduralEvidenceTests
{
    [Fact]
    public void PropagatesLoopRepetitionAndMaterializationFromCalledMethod()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            class Sample
            {
                private readonly List<int> _items = new();

                void Root(List<int> ids)
                {
                    foreach (var id in ids)
                        GetItems(id);
                }

                List<int> GetItems(int id)
                {
                    var buffer = new byte[8];
                    _ = buffer.Length;
                    return _items.Where(item => item == id).ToList();
                }
            }
            """;

        var compilation = CreateCompilation(source);
        var root = GetMethod(compilation, "Root");
        Assert.NotEmpty(root.DeclaringSyntaxReferences);
        Assert.NotEmpty(root.DeclaringSyntaxReferences[0].GetSyntax().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>());
        var graph = InterproceduralGraphBuilder.Build(compilation, root, CancellationToken.None);
        Assert.NotEmpty(graph.CallSites);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "GetItems" && site.State == CallTargetState.Known);
        var evidence = InterproceduralEvidenceAnalyzer.Analyze(compilation, graph, CancellationToken.None);

        Assert.Contains(evidence, item => item.Kind == EvidenceKind.RepeatedCall);
        Assert.Contains(evidence, item => item.Kind == EvidenceKind.Materialization || item.Kind == EvidenceKind.RepeatedCall);
        Assert.Contains(evidence, item => item.Kind == EvidenceKind.Allocation);
        Assert.Contains(evidence, item => item.Dimension.Contains("Memory", StringComparison.Ordinal));
    }

    [Fact]
    public void ReportsRedundantCallsAndBlockingEvidence()
    {
        const string source = """
            using System.Threading;
            class Sample
            {
                void Root()
                {
                    for (var index = 0; index < 2; index++)
                    {
                        Work();
                        Work();
                        Thread.Sleep(1);
                    }
                }

                void Work() { }
            }
            """;

        var compilation = CreateCompilation(source);
        var evidence = InterproceduralEvidenceAnalyzer.Analyze(
            compilation,
            InterproceduralGraphBuilder.Build(compilation, GetMethod(compilation, "Root"), CancellationToken.None),
            CancellationToken.None);

        Assert.Contains(evidence, item => item.Kind == EvidenceKind.RedundantWork);
        Assert.Contains(evidence, item => item.Kind == EvidenceKind.BlockingCall);
    }

    [Fact]
    public void PropagatesKnownLoopMultiplierToV5CostEvidence()
    {
        const string source = "class Sample { void Root() { for (var i = 0; i < 5; i++) Work(); } void Work() { _ = new byte[8]; } }";
        var compilation = CreateCompilation(source);

        var evidence = InterproceduralCostInference.Analyze(
            compilation,
            GetMethod(compilation, "Root"),
            CancellationToken.None);

        Assert.Contains(evidence.Items, item => item.Multiplier.IsKnown && item.Multiplier.Value == 5);
    }

    [Fact]
    public void PreservesUnknownLoopMultiplierForV5CostEvidence()
    {
        const string source = "class Sample { void Root(int limit) { for (var i = 0; i < limit; i++) Work(); } void Work() { _ = new byte[8]; } }";
        var compilation = CreateCompilation(source);

        var evidence = InterproceduralCostInference.Analyze(
            compilation,
            GetMethod(compilation, "Root"),
            CancellationToken.None);

        Assert.Contains(evidence.Items, item => !item.Multiplier.IsKnown);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Thread).Assembly.Location)
        };
        return CSharpCompilation.Create(
            "InterproceduralEvidenceTests",
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static IMethodSymbol GetMethod(CSharpCompilation compilation, string name)
    {
        var type = compilation.GetTypeByMetadataName("Sample");
        return Assert.Single(type!.GetMembers(name).OfType<IMethodSymbol>());
    }
}
