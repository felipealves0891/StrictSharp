```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-FGEKWY : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

IterationCount=3  LaunchCount=1  WarmupCount=1  

```
| Method                          | Iterations | PayloadSize | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0     | Gen1     | Gen2   | Allocated | Alloc Ratio |
|-------------------------------- |----------- |------------ |-----------:|----------:|---------:|------:|--------:|---------:|---------:|-------:|----------:|------------:|
| &#39;PERF011: retaining collection&#39; | 5000       | 256         | 1,542.4 μs | 329.07 μs | 18.04 μs | 11.80 |    0.12 | 226.5625 | 113.2813 | 3.9063 |   1.34 MB |        1.00 |
| &#39;PERF011: bounded collection&#39;   | 5000       | 256         |   130.7 μs |   8.47 μs |  0.46 μs |  1.00 |    0.00 | 446.2891 |        - |      - |   1.34 MB |        1.00 |
