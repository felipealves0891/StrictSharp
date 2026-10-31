using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using StrictSharp.Analyzers.Interprocedural;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class InterproceduralGraphTests
{
    [Fact]
    public void BuildsKnownEdgesAndMemoizesSharedCallees()
    {
        const string source = """
            class Sample
            {
                void Root()
                {
                    First();
                    Second();
                }

                void First() => Shared();
                void Second() => Shared();
                void Shared() { }
            }
            """;

        var compilation = CreateCompilation(source);
        var root = GetMethod(compilation, "Root");
        var graph = InterproceduralGraphBuilder.Build(compilation, root, CancellationToken.None);

        Assert.False(graph.ContainsCycle);
        Assert.Equal(4, graph.CallSites.Length);
        Assert.All(graph.CallSites, site => Assert.Equal(CallTargetState.Known, site.State));
            Assert.All(graph.CallSites, site => Assert.Equal(CallFlowKind.Synchronous, site.Flow));
        Assert.Equal(2, graph.CallSites.Count(site => site.Target?.Name == "Shared"));
    }

    [Fact]
    public void KeepsExternalBodiesUnknownInsteadOfTreatingThemAsZeroCost()
    {
        const string source = """
            using System;
            class Sample
            {
                void Root(string value) => Console.WriteLine(value);
            }
            """;

        var compilation = CreateCompilation(source);
        var graph = InterproceduralGraphBuilder.Build(compilation, GetMethod(compilation, "Root"), CancellationToken.None);

        var site = Assert.Single(graph.CallSites);
        Assert.Equal(CallTargetState.Unknown, site.State);
        Assert.NotNull(site.Target);
        Assert.Equal(CallFlowKind.Synchronous, site.Flow);
    }

    [Fact]
    public void DetectsRecursiveCallCyclesWithoutRecursingForever()
    {
        const string source = """
            class Sample
            {
                void First() => Second();
                void Second() => First();
            }
            """;

        var compilation = CreateCompilation(source);
        var graph = InterproceduralGraphBuilder.Build(compilation, GetMethod(compilation, "First"), CancellationToken.None);

        Assert.True(graph.ContainsCycle);
        Assert.Equal(2, graph.CallSites.Length);
    }

    [Fact]
    public void DistinguishesAwaitedAndBlockingCalls()
    {
        const string source = """
            using System.Threading.Tasks;
            class Sample
            {
                async Task Root()
                {
                    await Work();
                    Work().Wait();
                }

                async Task Work() => await Task.CompletedTask;
            }
            """;

        var compilation = CreateCompilation(source);
        var graph = InterproceduralGraphBuilder.Build(compilation, GetMethod(compilation, "Root"), CancellationToken.None);

        Assert.Contains(graph.CallSites, site => site.Target?.Name == "Work" && site.Flow == CallFlowKind.Awaited);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "Wait" && site.Flow == CallFlowKind.Blocking);
    }

    [Fact]
    public void KeepsInterfaceAndDelegateDispatchUnknownWithoutAResolvedBody()
    {
        const string source = """
            using System;
            interface IWorker
            {
                void Execute();
            }

            class Sample
            {
                void Root(IWorker worker, Action callback)
                {
                    worker.Execute();
                    callback();
                }
            }
            """;

        var compilation = CreateCompilation(source);
        var graph = InterproceduralGraphBuilder.Build(compilation, GetMethod(compilation, "Root"), CancellationToken.None);

        Assert.Equal(2, graph.CallSites.Length);
        Assert.All(graph.CallSites, site => Assert.Equal(CallTargetState.Unknown, site.State));
    }

    [Fact]
    public void ResolvesAConcreteVirtualImplementationWhenItsBodyIsAvailable()
    {
        const string source = """
            class Base
            {
                public virtual void Execute() { }
            }

            class Sample : Base
            {
                public override void Execute() { }
                public void Root() => Execute();
            }
            """;

        var compilation = CreateCompilation(source);
        var graph = InterproceduralGraphBuilder.Build(compilation, GetMethod(compilation, "Root", "Sample"), CancellationToken.None);

        var site = Assert.Single(graph.CallSites);
        Assert.Equal("Execute", site.Target?.Name);
        Assert.Equal(CallTargetState.Known, site.State);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location)
        };
        return CSharpCompilation.Create(
            "InterproceduralGraphTests",
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static IMethodSymbol GetMethod(CSharpCompilation compilation, string name, string typeName = "Sample")
    {
        var type = compilation.GetTypeByMetadataName(typeName);
        return Assert.Single(type!.GetMembers(name).OfType<IMethodSymbol>());
    }
}
