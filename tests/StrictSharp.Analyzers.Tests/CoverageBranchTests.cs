using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class CoverageBranchTests
{
    [Fact]
    public async Task Perf011CoversDerivedCollectionAndObservableSourceBranches()
    {
        const string source = """
            using System;
            using System.Collections.Generic;

            class DerivedCollection : List<string> { }

            class Source : IObservable<string>
            {
                public IDisposable Subscribe(IObserver<string> observer) => null!;
            }

            class Sample
            {
                private readonly DerivedCollection _values = new();
                private readonly Source _source = new();

                void Run(string[] values, IObserver<string> observer)
                {
                    foreach (var value in values)
                    {
                        _values.Add(value);
                        _source.Subscribe(observer);
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer());

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("PERF011", diagnostic.Id));
    }

    [Fact]
    public async Task Perf011CoversRecognizedCollectionTypeBranches()
    {
        const string source = """
            using System.Collections.Generic;

            static class CollectionExtensions
            {
                public static void Add(this LinkedList<string> values, string value) => values.AddLast(value);
                public static void Add(this Queue<string> values, string value) => values.Enqueue(value);
                public static void Add(this Stack<string> values, string value) => values.Push(value);
            }

            class Sample
            {
                private readonly HashSet<string> _hashSet = new();
                private readonly Dictionary<string, string> _dictionary = new();
                private readonly SortedDictionary<string, string> _sortedDictionary = new();
                private readonly SortedSet<string> _sortedSet = new();
                private readonly LinkedList<string> _linkedList = new();
                private readonly Queue<string> _queue = new();
                private readonly Stack<string> _stack = new();
                private readonly ICollection<string> _collection = new List<string>();
                private readonly IDictionary<string, string> _interfaceDictionary = new Dictionary<string, string>();
                private readonly ISet<string> _interfaceSet = new HashSet<string>();

                void Run(string[] values)
                {
                    foreach (var value in values)
                    {
                        _hashSet.Add(value);
                        _dictionary.Add(value, value);
                        _sortedDictionary.Add(value, value);
                        _sortedSet.Add(value);
                        _linkedList.Add(value);
                        _queue.Add(value);
                        _stack.Add(value);
                        _collection.Add(value);
                        _interfaceDictionary.Add(value, value);
                        _interfaceSet.Add(value);
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer());

        Assert.Equal(6, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("PERF011", diagnostic.Id));
    }

    [Fact]
    public async Task Perf011StaysSilentForDynamicReceivers()
    {
        const string source = """
            class Sample
            {
                private readonly dynamic _values = null!;
                private readonly dynamic _source = null!;

                void Run(string[] values, object observer)
                {
                    foreach (var value in values)
                    {
                        _values.Add(value);
                        _source.Subscribe(observer);
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf030CoversComplexInitializationFormsAndForWithoutDeclaration()
    {
        const string source = """
            class Helper { }

            class Complex
            {
                public Complex(string value)
                {
                    _ = new Helper();
                    _ = value.Length > 0 ? new Helper() : null;
                }
            }

            class Descriptor
            {
                public Helper Value { get; init; }
            }

            class Sample
            {
                void Run(int count, string[] values)
                {
                    for (; count > 0; count--)
                        _ = new Complex("fixed");

                    foreach (var value in values)
                    {
                        _ = new Descriptor { Value = new Helper() };
                        _ = new Descriptor { Value = value.Length > 0 ? new Helper() : null };
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf030Analyzer());

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("PERF030", diagnostic.Id));
    }

    [Fact]
    public async Task Perf030StaysSilentForLoopDependentInitializer()
    {
        const string source = """
            class Descriptor
            {
                public string Name { get; init; }
            }

            class Sample
            {
                void Run(string[] values)
                {
                    foreach (var value in values)
                        _ = new Descriptor { Name = value };
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf030Analyzer()));
    }

    [Fact]
    public async Task Perf013RecognizesSupportedEnumerationOperations()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            class Sample
            {
                void Read(IEnumerable<int> values, int item)
                {
                    values.Count(); values.Count();
                    values.LongCount(); values.LongCount();
                    values.Any(); values.Any();
                    values.First(); values.First();
                    values.FirstOrDefault(); values.FirstOrDefault();
                    values.Single(); values.Single();
                    values.SingleOrDefault(); values.SingleOrDefault();
                    values.Last(); values.Last();
                    values.LastOrDefault(); values.LastOrDefault();
                    values.ToList(); values.ToList();
                    values.ToArray(); values.ToArray();
                    values.ToDictionary(value => value); values.ToDictionary(value => value);
                    values.ToLookup(value => value); values.ToLookup(value => value);
                    values.Sum(); values.Sum();
                    values.Average(); values.Average();
                    values.Min(); values.Min();
                    values.Max(); values.Max();
                    values.Contains(item); values.Contains(item);
                    values.ElementAt(0); values.ElementAt(0);
                    values.ElementAtOrDefault(0); values.ElementAtOrDefault(0);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf013Analyzer());

        Assert.Equal(39, diagnostics.Length);
    }

    [Fact]
    public async Task Perf014RecognizesAllSupportedListSearchOperations()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            class Sample
            {
                void Read(List<int> values, int item)
                {
                    for (var index = 0; index < 1; index++)
                    {
                        values.Contains(item);
                        values.IndexOf(item);
                        values.Find(value => value == item);
                        values.First();
                        values.FirstOrDefault();
                        values.SingleOrDefault();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf014Analyzer());

        Assert.Equal(3, diagnostics.Length);
    }

    [Fact]
    public async Task Perf012CoversCloneAndMaterializationBranches()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class Sample
            {
                void Copy(IEnumerable<int> values, int[] array)
                {
                    var list = values.ToList();
                    var copy = values.ToArray();
                    var clone = (int[])array.Clone();
                    var count = values.Count();
                    Missing.ToArray();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf012Analyzer());

        Assert.Equal(3, diagnostics.Length);
    }

    [Fact]
    public async Task Perf027RecognizesConcatenationAndSerializationCosts()
    {
        const string source = """
            interface ILogger
            {
                void LogDebug(string message);
            }
            static class Json
            {
                public static string Serialize(object value) => "";
            }
            class Sample
            {
                void Log(ILogger logger, object value, string text)
                {
                    logger.LogDebug(text + value);
                    logger.LogDebug(Json.Serialize(value));
                    logger.LogDebug("constant");
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf027Analyzer());

        Assert.Equal(2, diagnostics.Length);
    }

    [Fact]
    public async Task Perf007RecognizesLoopAllocationsAcrossSupportedTypes()
    {
        const string source = """
            using System.Collections.Generic;
            using System.IO;
            using System.Text;
            class Sample
            {
                void Allocate()
                {
                    for (var index = 0; index < 1; index++)
                    {
                        _ = new List<int>();
                        _ = new Dictionary<int, int>();
                        _ = new HashSet<int>();
                        _ = new StringBuilder();
                        _ = new MemoryStream();
                        _ = new int[2];
                        _ = new[] { 1, 2 };
                        _ = new object();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf007Analyzer());

        Assert.Equal(7, diagnostics.Length);
    }

    [Fact]
    public async Task Perf008DistinguishesLargeConstantArrayDimensions()
    {
        const string source = """
            class Sample
            {
                void Allocate(int length)
                {
                    for (var index = 0; index < 1; index++)
                    {
                        _ = new byte[85000];
                        _ = new byte[84999];
                        _ = new byte[length];
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf008Analyzer());

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Perf015RecognizesEfficientCountSourcesAndArgumentExceptions()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            class Sample
            {
                void Count(ICollection<int> collection, int[] array)
                {
                    collection.Count();
                    collection.Any();
                    array.Count();
                    collection.Count(item => item > 0);
                    Missing.Count();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf015Analyzer());

        Assert.Equal(3, diagnostics.Length);
    }

    [Fact]
    public async Task Perf019AndPerf020CoverStringCopyAndComparisonAlternatives()
    {
        const string source = """
            class Sample
            {
                bool Check(string value)
                {
                    var chars = value.ToCharArray();
                    var part = value.Substring(0, 1);
                    var equal = value.ToUpperInvariant() == "X";
                    var method = value.ToLower().Equals("x");
                    return string.Compare(value.ToUpper(), "X") == 0 && chars.Length > part.Length && equal && method;
                }
            }
            """;

        var copyDiagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf019Analyzer());
        var comparisonDiagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf020Analyzer());

        Assert.Equal(2, copyDiagnostics.Length);
        Assert.Equal(2, comparisonDiagnostics.Length);
    }

    [Fact]
    public async Task Perf025ReportsMethodScopedHttpClientCreationOnly()
    {
        const string source = """
            using System.Net.Http;
            class Sample
            {
                private static readonly HttpClient Shared = new HttpClient();
                HttpClient Create() => new HttpClient();
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf025Analyzer());

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Perf003CoversAsyncAndBlockTaskWrappers()
    {
        const string source = """
            using System.Threading.Tasks;
            class Sample
            {
                Task<int> GetAsync() => Task.FromResult(1);
                Task<int> Expression() => Task.Run(() => GetAsync());
                Task<int> AsyncLambda() => Task.Run(async () => await GetAsync());
                Task<int> BlockLambda() => Task.Run(() => { return GetAsync(); });
                Task<int> NotWrapper()
                {
                    var task = Task.Run(() => GetAsync());
                    return task;
                }
                Task<int> CpuBound() => Task.Run(() => 1);
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf003Analyzer());

        Assert.Equal(3, diagnostics.Length);
    }

    [Fact]
    public async Task Perf022DistinguishesMatchingAndNonMatchingDictionaryKeys()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Read(Dictionary<string, int> values, Dictionary<string, int> other, string key, string otherKey, IReadOnlyDictionary<string, int> readOnly)
                {
                    if (values.ContainsKey(key))
                    {
                        _ = values[otherKey];
                    }
                    if (values.ContainsKey(key))
                        values.Remove(otherKey);
                    if (other.ContainsKey(key))
                        _ = other[key];
                    if (readOnly.ContainsKey(key))
                        _ = readOnly[key];
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf022Analyzer());

        Assert.Equal(2, diagnostics.Length);
    }
}
