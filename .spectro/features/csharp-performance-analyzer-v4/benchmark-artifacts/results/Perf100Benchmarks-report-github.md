```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                   | Iterations | Mean     | Error | Ratio | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|------:|------:|----------:|------------:|
| &#39;PERF100: repeated interprocedural work&#39; | 1000       | 661.7 μs |    NA |  1.00 |   72000 B |       1.000 |
| &#39;PERF100: reused work&#39;                   | 1000       | 779.7 μs |    NA |  1.18 |      72 B |       0.001 |
