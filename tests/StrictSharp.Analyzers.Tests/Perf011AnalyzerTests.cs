using StrictSharp.Analyzers.Analyzers;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public sealed class Perf011AnalyzerTests
{
    [Fact]
    public async Task Perf011ReportsFieldCollectionRetainedByRepeatedAdd()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                private readonly List<string> _values = new();

                void Run(string[] values)
                {
                    foreach (var value in values)
                        _values.Add(value);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer());

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("PERF011", diagnostic.Id);
    }

    [Fact]
    public async Task Perf011ReportsFieldCollectionRetainedByRepeatedAddRange()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                private readonly List<string> _values = new();

                void Run(string[] values)
                {
                    foreach (var value in values)
                        _values.AddRange(values);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer());

        Assert.Single(diagnostics);
        Assert.Equal("PERF011", diagnostics[0].Id);
    }

    [Fact]
    public async Task Perf011ReportsFieldObservableRetainedBySubscribe()
    {
        const string source = """
            using System;
            class Sample
            {
                private readonly IObservable<string> _source = null!;

                void Run(string[] values, IObserver<string> observer)
                {
                    foreach (var value in values)
                        _source.Subscribe(observer);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer());

        Assert.Single(diagnostics);
        Assert.Equal("PERF011", diagnostics[0].Id);
    }

    [Fact]
    public async Task Perf011ReportsInterfaceCollectionRetainedByAdd()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                private readonly ICollection<string> _values = new List<string>();

                void Run(string[] values)
                {
                    foreach (var value in values)
                        _values.Add(value);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer());

        Assert.Single(diagnostics);
        Assert.Equal("PERF011", diagnostics[0].Id);
    }

    [Fact]
    public async Task Perf011StaysSilentForLocalCollection()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                void Run(string[] values)
                {
                    var local = new List<string>();
                    foreach (var value in values)
                        local.Add(value);
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011StaysSilentWhenFieldCollectionIsCleared()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                private readonly List<string> _values = new();

                void Run(string[] values)
                {
                    foreach (var value in values)
                    {
                        _values.Add(value);
                        _values.Clear();
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011StaysSilentForNonRetainingInvocation()
    {
        const string source = """
            class Sample
            {
                void Run(string[] values)
                {
                    foreach (var value in values)
                        value.ToString();
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011StaysSilentForArbitraryFieldAddMethod()
    {
        const string source = """
            class Sink
            {
                public void Add(string value) { }
            }

            class Sample
            {
                private readonly Sink _sink = new();

                void Run(string[] values)
                {
                    foreach (var value in values)
                        _sink.Add(value);
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011StaysSilentForArbitraryFieldSubscribeMethod()
    {
        const string source = """
            class SubscriptionManager
            {
                public void Subscribe(string value) { }
            }

            class Sample
            {
                private readonly SubscriptionManager _manager = new();

                void Run(string[] values)
                {
                    foreach (var value in values)
                        _manager.Subscribe(value);
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011StaysSilentWhenFieldCollectionIsUpdatedOutsideLoop()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                private readonly List<string> _values = new();

                void Run(string value)
                {
                    _values.Add(value);
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011StaysSilentForEnumerableWithoutRetainingOperation()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                private readonly IEnumerable<string> _values = new List<string>();

                void Run(string[] values)
                {
                    foreach (var value in values)
                        _values.GetEnumerator();
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011ReportsWhenListCapacityIsOnlyAnInitialCapacity()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                private readonly List<string> _values = new(4);

                void Run(string[] values)
                {
                    foreach (var value in values)
                        _values.Add(value);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer());

        Assert.Single(diagnostics);
        Assert.Equal("PERF011", diagnostics[0].Id);
    }

    [Fact]
    public async Task Perf011StaysSilentWhenOwnerDisposesAndClearsCollection()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            class Sample : IDisposable
            {
                private readonly List<string> _values = new();

                void Run(string[] values)
                {
                    foreach (var value in values)
                        _values.Add(value);
                }

                public void Dispose()
                {
                    ReleaseMarker();
                    _values.Clear();
                }

                private static void ReleaseMarker() { }

                private void Helper() { }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011StaysSilentWhenInsertionHasAnExplicitBound()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                private readonly List<string> _values = new();

                void Run(string[] values)
                {
                    foreach (var value in values)
                    {
                        if (_values.Count < 4)
                            _values.Add(value);
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011StaysSilentWhenInsertionHasAnInclusiveExplicitBound()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                private readonly List<string> _values = new();

                void Run(string[] values)
                {
                    foreach (var value in values)
                    {
                        if (_values.Count <= 4)
                            _values.Add(value);
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011ReportsWhenBoundConditionIsNotProvable()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                private readonly List<string> _values = new();

                void Run(string[] values, int limit)
                {
                    foreach (var value in values)
                    {
                        if (true)
                        {
                            if (_values.Count == 4)
                                _values.Add(value);

                            if (_values.Count < limit)
                                _values.Add(value);
                        }
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer());

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("PERF011", diagnostic.Id));
    }

    [Fact]
    public async Task Perf011StaysSilentForPropertyWithUnresolvedOwnership()
    {
        const string source = """
            using System.Collections.Generic;
            class Sample
            {
                private readonly ICollection<string> _values = new List<string>();
                private ICollection<string> Values => GetValues();

                void Run(string[] values)
                {
                    foreach (var value in values)
                        Values.Add(value);
                }

                private static ICollection<string> GetValues() => new List<string>();
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011StaysSilentForLocallyCreatedOwnerWithoutEscape()
    {
        const string source = """
            using System.Collections.Generic;
            class Holder
            {
                internal readonly List<string> Values = new();
            }

            class Sample
            {
                void Run(string[] values)
                {
                    var holder = new Holder();
                    foreach (var value in values)
                        holder.Values.Add(value);
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011ReportsWhenLocallyCreatedOwnerEscapesToStaticOwner()
    {
        const string source = """
            using System.Collections.Generic;
            class Holder
            {
                internal readonly List<string> Values = new();
            }

            static class Registry
            {
                public static void Register(Holder holder) { }
            }

            class Sample
            {
                void Run(string[] values)
                {
                    var holder = new Holder();
                    foreach (var value in values)
                        holder.Values.Add(value);

                    Registry.Register(holder);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer());

        Assert.Single(diagnostics);
        Assert.Equal("PERF011", diagnostics[0].Id);
    }

    [Fact]
    public async Task Perf011StaysSilentWhenOwnerEscapeCannotBeResolved()
    {
        const string source = """
            using System.Collections.Generic;
            class Holder
            {
                internal readonly List<string> Values = new();
            }

            class Sample
            {
                private void Forward(Holder holder) { }

                void Run(string[] values)
                {
                    var holder = new Holder();
                    foreach (var value in values)
                        holder.Values.Add(value);

                    Forward(holder);
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011ReportsWhenLocallyCreatedOwnerIsReturned()
    {
        const string source = """
            using System.Collections.Generic;
            class Holder
            {
                internal readonly List<string> Values = new();
            }

            class Sample
            {
                Holder Run(string[] values)
                {
                    var holder = new Holder();
                    foreach (var value in values)
                        holder.Values.Add(value);

                    return holder;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer());

        Assert.Single(diagnostics);
        Assert.Equal("PERF011", diagnostics[0].Id);
    }

    [Fact]
    public async Task Perf011ReportsWhenLocallyCreatedOwnerIsStoredInStaticField()
    {
        const string source = """
            using System.Collections.Generic;
            class Holder
            {
                internal readonly List<string> Values = new();
            }

            class Sample
            {
                private static Holder? _current;

                void Run(string[] values)
                {
                    var holder = new Holder();
                    foreach (var value in values)
                        holder.Values.Add(value);

                    _current = holder;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer());

        Assert.Single(diagnostics);
        Assert.Equal("PERF011", diagnostics[0].Id);
    }

    [Fact]
    public async Task Perf011StaysSilentWhenOwnerLifetimeIsUnknown()
    {
        const string source = """
            using System.Collections.Generic;
            class Holder
            {
                internal readonly List<string> Values = new();
            }

            class Sample
            {
                void Run(Holder holder, string[] values)
                {
                    foreach (var value in values)
                        holder.Values.Add(value);
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011StaysSilentForParenthesizedTransientOwner()
    {
        const string source = """
            using System.Collections.Generic;
            class Holder
            {
                internal readonly List<string> Values = new();
            }

            class Sample
            {
                void Run(string[] values)
                {
                    var holder = new Holder();
                    foreach (var value in values)
                        (holder).Values.Add(value);
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }

    [Fact]
    public async Task Perf011StaysSilentWhenOwnerIsAssignedToAnInstanceField()
    {
        const string source = """
            using System.Collections.Generic;
            class Holder
            {
                internal readonly List<string> Values = new();
            }

            class Sample
            {
                private Holder? _current;

                void Run(string[] values)
                {
                    var holder = new Holder();
                    foreach (var value in values)
                        holder.Values.Add(value);

                    _current = holder;
                }
            }
            """;

        Assert.Empty(await AnalyzerTestHarness.AnalyzeAsync(source, new Perf011Analyzer()));
    }
}
