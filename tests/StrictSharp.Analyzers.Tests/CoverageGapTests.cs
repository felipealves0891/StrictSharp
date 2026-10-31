using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using StrictSharp.Analyzers.Analyzers;
using StrictSharp.Analyzers.Interprocedural;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class CoverageGapTests
{
    [Fact]
    public async Task Perf011CoversCustomRetainingCollectionAndNonRetainingOperations()
    {
        const string source = """
            using System.Collections;
            using System.Collections.Generic;
            class Retaining : List<string> { }
            class Sample
            {
                private readonly Retaining _values = new();
                private readonly string _text = "value";

                void Run(string[] values)
                {
                    foreach (var value in values)
                    {
                        _values.Add(value);
                        _text.ToString();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer());

        Assert.Single(diagnostics);
        Assert.Equal("PERF011", diagnostics[0].Id);
    }

    [Fact]
    public async Task Perf011CoversObservableImplementedThroughInterfaceAndNonIdentifierReceiver()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            class Observable : IObservable<string>
            {
                public IDisposable Subscribe(IObserver<string> observer) => null!;
            }
            class Sample
            {
                private readonly Observable _source = new();

                void Run(string[] values)
                {
                    foreach (var value in values)
                    {
                        _source.Subscribe(null!);
                        new List<string>().Add(value);
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer());

        Assert.Single(diagnostics);
        Assert.Equal("PERF011", diagnostics[0].Id);
    }

    [Fact]
    public async Task Perf011CoversReleaseMethodsAndNonFieldReceivers()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                private readonly Queue<string> _queue = new();
                private readonly Stack<string> _stack = new();

                void Run(string[] values)
                {
                    foreach (var value in values)
                    {
                        _queue.Enqueue(value);
                        _queue.Clear();
                        _stack.Push(value);
                        _stack.Pop();
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf030CoversForWithoutDeclarationAndComplexInitializerBranches()
    {
        const string source = """
            class Expensive
            {
                public Expensive(string value) { }
            }

            class Sample
            {
                string GetValue() => "fixed";

                void Run(int count)
                {
                    for (; count > 0; count--)
                        _ = new Expensive(GetValue());

                    for (var index = 0; index < count; index++)
                        _ = new Holder { Value = new Expensive("fixed") };
                }
            }

            class Holder
            {
                public Expensive Value { get; init; }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf030Analyzer());

        Assert.Single(diagnostics);
        Assert.Equal("PERF030", diagnostics[0].Id);
    }

    [Fact]
    public async Task Perf030CoversConditionalConstructorInitialization()
    {
        const string source = """
            class Expensive
            {
                public Expensive(string value)
                {
                    var normalized = value.Length > 0 ? new string(value) : value.ToString();
                }
            }

            class Sample
            {
                void Run(string[] values)
                {
                    foreach (var value in values)
                        _ = new Expensive("fixed");
                }
            }
            """;

        Assert.Single(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf030Analyzer()));
    }

    [Fact]
    public async Task Perf030CoversDeconstructionLoopsAndUnresolvedConstruction()
    {
        const string source = """
            class Expensive
            {
                public Expensive(string value) { }
            }

            class Sample
            {
                void Run((int Id, string Name)[] values)
                {
                    foreach (var (id, name) in values)
                        _ = new Expensive("fixed");

                    for (var index = 0; index < values.Length; index++)
                        _ = new MissingType();
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf030Analyzer()));
    }

    [Fact]
    public void GraphClassifiesSynchronousFileAndDerivedReaderWriterCalls()
    {
        const string source = """
            using System.IO;
            class Reader : TextReader { public override int Read(char[] buffer, int index, int count) => 0; }
            class Writer : TextWriter { public override void Write(char value) { } }
            class Sample
            {
                void Root()
                {
                    for (var index = 0; index < 1; index++)
                    {
                        File.ReadAllText("file");
                        new Reader().Read(null!, 0, 0);
                        new Writer().Write('x');
                    }
                }
            }
            """;

        var compilation = CreateCompilation(source, typeof(File).Assembly, typeof(TextReader).Assembly);
        var graph = InterproceduralGraphBuilder.Build(compilation, GetMethod(compilation, "Root"), CancellationToken.None);

        Assert.Contains(graph.CallSites, site => site.Target?.Name == "ReadAllText" && site.Flow == CallFlowKind.Blocking);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "Read" && site.Flow == CallFlowKind.Blocking);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "Write" && site.Flow == CallFlowKind.Blocking);
    }

    [Fact]
    public void GraphClassifiesAllKnownFileAndStreamBlockingCalls()
    {
        const string source = """
            using System.IO;
            class Sample
            {
                void Root(Stream stream)
                {
                    for (var index = 0; index < 1; index++)
                    {
                        File.ReadAllBytes("file");
                        File.WriteAllText("file", "value");
                        File.WriteAllBytes("file", new byte[0]);
                        File.AppendAllText("file", "value");
                        File.OpenRead("file");
                        File.OpenWrite("file");
                        stream.ReadByte();
                        stream.WriteByte(1);
                        stream.Flush();
                        stream.CopyTo(Stream.Null);
                    }
                }
            }
            """;

        var compilation = CreateCompilation(source, typeof(File).Assembly, typeof(Stream).Assembly);
        var graph = InterproceduralGraphBuilder.Build(compilation, GetMethod(compilation, "Root"), CancellationToken.None);

        Assert.Contains(graph.CallSites, site => site.Target?.Name == "ReadAllBytes" && site.Flow == CallFlowKind.Blocking);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "WriteAllText" && site.Flow == CallFlowKind.Blocking);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "WriteAllBytes" && site.Flow == CallFlowKind.Blocking);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "AppendAllText" && site.Flow == CallFlowKind.Blocking);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "OpenRead" && site.Flow == CallFlowKind.Blocking);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "OpenWrite" && site.Flow == CallFlowKind.Blocking);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "ReadByte" && site.Flow == CallFlowKind.Blocking);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "WriteByte" && site.Flow == CallFlowKind.Blocking);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "Flush" && site.Flow == CallFlowKind.Blocking);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "CopyTo" && site.Flow == CallFlowKind.Blocking);
    }

    [Fact]
    public void GraphClassifiesReaderAndWriterBlockingCalls()
    {
        const string source = """
            using System.IO;
            class Sample
            {
                void Root(TextReader reader, TextWriter writer)
                {
                    for (var index = 0; index < 1; index++)
                    {
                        reader.Read(null!, 0, 0);
                        reader.ReadLine();
                        reader.ReadToEnd();
                        reader.ReadBlock(null!, 0, 0);
                        writer.Write('x');
                        writer.WriteLine('x');
                        writer.Flush();
                    }
                }
            }
            """;

        var compilation = CreateCompilation(source, typeof(TextReader).Assembly);
        var graph = InterproceduralGraphBuilder.Build(compilation, GetMethod(compilation, "Root"), CancellationToken.None);

        Assert.Contains(graph.CallSites, site => site.Target?.Name == "ReadLine" && site.Flow == CallFlowKind.Blocking);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "ReadToEnd" && site.Flow == CallFlowKind.Blocking);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "ReadBlock" && site.Flow == CallFlowKind.Blocking);
        Assert.Contains(graph.CallSites, site => site.Target?.Name == "WriteLine" && site.Flow == CallFlowKind.Blocking);
    }

    [Fact]
    public void EvidenceCoversCycleAndMaterializationVariants()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            class Sample
            {
                void Root(List<int> values)
                {
                    foreach (var value in values)
                    {
                        Work(values);
                        Work(values);
                    }
                }

                List<int> Work(List<int> values)
                {
                    var buffer = new int[values.Count];
                    var a = values.ToArray();
                    var b = values.ToDictionary(value => value);
                    var c = values.OrderBy(value => value).ThenBy(value => value).ToLookup(value => value);
                    return a.ToList();
                }
            }
            """;

        var compilation = CreateCompilation(source, typeof(Enumerable).Assembly, typeof(List<>).Assembly);
        var graph = InterproceduralGraphBuilder.Build(compilation, GetMethod(compilation, "Root"), CancellationToken.None);
        var evidence = InterproceduralEvidenceAnalyzer.Analyze(compilation, graph, CancellationToken.None);

        Assert.Contains(evidence, item => item.Kind == EvidenceKind.RedundantWork);
        Assert.Contains(evidence, item => item.Kind == EvidenceKind.Materialization);
        Assert.Contains(evidence, item => item.Kind == EvidenceKind.Allocation);
    }

    [Fact]
    public void EvidenceReportsRecursiveCalls()
    {
        const string source = """
            class Sample
            {
                void Root()
                {
                    for (var index = 0; index < 1; index++)
                        Next();
                }

                void Next() => Root();
            }
            """;

        var compilation = CreateCompilation(source);
        var graph = InterproceduralGraphBuilder.Build(compilation, GetMethod(compilation, "Root"), CancellationToken.None);
        var evidence = InterproceduralEvidenceAnalyzer.Analyze(compilation, graph, CancellationToken.None);

        Assert.Contains(evidence, item => item.Kind == EvidenceKind.RecursiveCall);
    }

    private static CSharpCompilation CreateCompilation(string source, params System.Reflection.Assembly[] assemblies)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location)
        };

        foreach (var assembly in assemblies)
            references.Add(MetadataReference.CreateFromFile(assembly.Location));

        return CSharpCompilation.Create(
            "CoverageGapTests",
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
