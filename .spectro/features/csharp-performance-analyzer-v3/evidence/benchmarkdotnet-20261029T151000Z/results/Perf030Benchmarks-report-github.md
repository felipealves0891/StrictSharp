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
| &#39;PERF030: rebuilding invariant value&#39; | 5000       | 256         | 1,496.7 μs | 508.33 μs | 27.86 μs |  2.46 |    0.04 | 484.3750 | 1520000 B |          NA |
| &#39;PERF030: reusing immutable value&#39;    | 5000       | 256         |   607.9 μs |  83.01 μs |  4.55 μs |  1.00 |    0.01 |        - |         - |          NA |
