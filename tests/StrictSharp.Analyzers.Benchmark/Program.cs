using BenchmarkDotNet.Running;
using StrictSharp.Analyzers.Benchmark;

var summary = BenchmarkRunner.Run<StringConcatenationBenchmark>();
