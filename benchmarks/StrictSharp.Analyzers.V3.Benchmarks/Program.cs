using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;

var config = ManualConfig.Create(DefaultConfig.Instance)
    .AddExporter(JsonExporter.Full)
    .WithArtifactsPath(Environment.GetEnvironmentVariable("STRICTSHARP_BENCHMARK_ARTIFACTS") ?? "BenchmarkDotNet.Artifacts");

BenchmarkSwitcher.FromAssembly(typeof(Perf011Benchmarks).Assembly).Run(args, config);

[MemoryDiagnoser]
public class Perf011Benchmarks
{
    [Params(5_000)]
    public int Iterations { get; set; }

    [Params(256)]
    public int PayloadSize { get; set; }

    private List<byte[]> _retained = null!;
    private Queue<byte[]> _bounded = null!;

    [GlobalSetup]
    public void Setup()
    {
        _retained = new List<byte[]>(Iterations);
        _bounded = new Queue<byte[]>(64);
    }

    [Benchmark(Description = "PERF011: retaining collection")]
    public int Retaining()
    {
        for (var index = 0; index < Iterations; index++)
            _retained.Add(CreatePayload(PayloadSize, index));
        return _retained.Count;
    }

    [Benchmark(Baseline = true, Description = "PERF011: bounded collection")]
    public int Bounded()
    {
        for (var index = 0; index < Iterations; index++)
        {
            _bounded.Enqueue(CreatePayload(PayloadSize, index));
            if (_bounded.Count > 64)
                _bounded.Dequeue();
        }
        return _bounded.Count;
    }

    private static byte[] CreatePayload(int size, int seed)
    {
        var payload = new byte[size];
        payload[0] = unchecked((byte)seed);
        return payload;
    }
}

[MemoryDiagnoser]
public class Perf030Benchmarks
{
    [Params(5_000)]
    public int Iterations { get; set; }

    [Params(256)]
    public int PayloadSize { get; set; }

    private ComplexValue _reusable = null!;

    [GlobalSetup]
    public void Setup()
    {
        _reusable = new ComplexValue(PayloadSize, 17);
    }

    [Benchmark(Description = "PERF030: rebuilding invariant value")]
    public long Rebuilding()
    {
        long result = 0;
        for (var index = 0; index < Iterations; index++)
            result += new ComplexValue(PayloadSize, 17).Checksum;
        return result;
    }

    [Benchmark(Baseline = true, Description = "PERF030: reusing immutable value")]
    public long Reusing()
    {
        long result = 0;
        for (var index = 0; index < Iterations; index++)
            result += _reusable.Checksum;
        return result;
    }

    private sealed class ComplexValue
    {
        private readonly byte[] _payload;

        public ComplexValue(int payloadSize, int seed)
        {
            _payload = new byte[payloadSize];
            for (var index = 0; index < _payload.Length; index++)
                _payload[index] = unchecked((byte)(seed + index));
        }

        public long Checksum
        {
            get
            {
                long checksum = 0;
                for (var index = 0; index < _payload.Length; index++)
                    checksum += _payload[index];
                return checksum;
            }
        }
    }
}
