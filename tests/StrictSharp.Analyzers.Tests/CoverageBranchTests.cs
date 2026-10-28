using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class CoverageBranchTests
{
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
