```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=3  LaunchCount=1  
UnrollFactor=1  WarmupCount=1  

```
| Method                          | Iterations | PayloadSize | Mean | Error |
|-------------------------------- |----------- |------------ |-----:|------:|
| &#39;PERF011: retaining collection&#39; | 5000       | 256         |   NA |    NA |

Benchmarks with issues:
  Perf011Benchmarks.'PERF011: retaining collection': Job-TASYDQ(InvocationCount=1, IterationCount=3, LaunchCount=1, UnrollFactor=1, WarmupCount=1) [Iterations=5000, PayloadSize=256]
