namespace StrictSharp.Analyzers.Lifecycle;

public enum PerformanceContextKind
{
    BackgroundWorker,
    EventDriven,
    Polling,
    Retry,
    Batch,
    GameLoop
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class PerformanceContextAttribute : Attribute
{
    public PerformanceContextKind Kind { get; set; }

    public double ExpectedIterationsPerSecond { get; set; }
}
