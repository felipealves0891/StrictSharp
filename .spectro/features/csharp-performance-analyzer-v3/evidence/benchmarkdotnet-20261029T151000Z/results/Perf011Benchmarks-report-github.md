```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-TASYDQ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=3  LaunchCount=1  
UnrollFactor=1  WarmupCount=1  

```
| Method                          | Iterations | PayloadSize | Mean     | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |----------- |------------ |---------:|-----------:|----------:|------:|--------:|----------:|------------:|
| &#39;PERF011: retaining collection&#39; | 5000       | 256         | 748.0 μs | 1,751.7 μs |  96.02 μs |  1.28 |    0.33 |   1.34 MB |        1.00 |
| &#39;PERF011: bounded collection&#39;   | 5000       | 256         | 613.7 μs | 3,107.1 μs | 170.31 μs |  1.05 |    0.35 |   1.34 MB |        1.00 |
