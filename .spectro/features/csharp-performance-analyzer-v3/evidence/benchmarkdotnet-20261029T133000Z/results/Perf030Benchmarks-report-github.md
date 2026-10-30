```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-FGEKWY : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

IterationCount=3  LaunchCount=1  WarmupCount=1  

```
| Method                                | Iterations | PayloadSize | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0     | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |------------ |-----------:|----------:|---------:|------:|--------:|---------:|----------:|------------:|
| &#39;PERF030: rebuilding invariant value&#39; | 5000       | 256         | 1,470.5 μs | 237.95 μs | 13.04 μs |  2.39 |    0.02 | 484.3750 | 1520000 B |          NA |
| &#39;PERF030: reusing immutable value&#39;    | 5000       | 256         |   614.1 μs |  38.11 μs |  2.09 μs |  1.00 |    0.00 |        - |         - |          NA |
