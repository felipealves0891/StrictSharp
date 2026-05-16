; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0

### New Rules

Rule ID  | Category      | Severity | Notes
---------|---------------|----------|--------------------
PERF001  |  Performance  |  Warning | To ensure zero allocation, objects in loops should come from a pool.
PERF003  |  Performance  |  Warning | Boxing allocates memory on the Heap. Try using Generics <T> or Spans to avoid converting to object.
PERF005  |  Performance  |  Warning | Large objects have slow initialization.
PERF007  |  Performance  |  Warning | Traditional string handling in .NET is one of the \"silent villains\" of performance, especially in high-throughput systems (HFT, low-latency APIs, or Engines).
PERF020  |  Performance  |  Info    | This lambda expression captures variables from an outer scope, which results in a closure allocation on the heap. Captured variables extend their lifetime and may introduce unintended memory pressure, especially in high-frequency or performance-critical code paths. Consider avoiding captures by using static lambdas, passing variables as parameters, or restructuring the code to eliminate the closure.
PERF021  |  Performance  |  Warning | This lambda expression captures variables from an outer scope, which results in a closure allocation on the heap. Captured variables extend their lifetime and may introduce unintended memory pressure, especially in high-frequency or performance-critical code paths. Consider avoiding captures by using static lambdas, passing variables as parameters, or restructuring the code to eliminate the closure.
PERF022  |  Performance  |  Warning | This lambda expression captures variables from an outer scope, which results in a closure allocation on the heap. Captured variables extend their lifetime and may introduce unintended memory pressure, especially in high-frequency or performance-critical code paths. Consider avoiding captures by using static lambdas, passing variables as parameters, or restructuring the code to eliminate the closure.
PERF023  |  Performance  |  Warning | This lambda expression captures variables from an outer scope, which results in a closure allocation on the heap. Captured variables extend their lifetime and may introduce unintended memory pressure, especially in high-frequency or performance-critical code paths. Consider avoiding captures by using static lambdas, passing variables as parameters, or restructuring the code to eliminate the closure.


