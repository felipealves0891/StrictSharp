using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;

var artifactsPath = Environment.GetEnvironmentVariable("STRICTSHARP_BENCHMARK_ARTIFACTS") ?? "BenchmarkDotNet.Artifacts";
var config = ManualConfig.Create(DefaultConfig.Instance)
    .AddExporter(JsonExporter.Full)
    .WithArtifactsPath(artifactsPath);

BenchmarkSwitcher.FromAssembly(typeof(Perf100Benchmarks).Assembly).Run(args, config);

[MemoryDiagnoser]
public class Perf100Benchmarks
{
    [Params(1_000)]
    public int Iterations { get; set; }

    [Benchmark(Baseline = true, Description = "PERF100: repeated interprocedural work")]
    public int RepeatedWork()
    {
        var total = 0;
        for (var index = 0; index < Iterations; index++)
            total += BuildItems(index).Count;

        return total;
    }

    [Benchmark(Description = "PERF100: reused work")]
    public int ReusedWork()
    {
        var items = BuildItems(0);
        var total = 0;
        for (var index = 0; index < Iterations; index++)
            total += items.Count;

        return total;
    }

    private static List<int> BuildItems(int seed)
    {
        return [seed, seed + 1, seed + 2, seed + 3];
    }
}
