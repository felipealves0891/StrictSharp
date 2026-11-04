using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StrictSharp.Analyzers.Lifecycle;

public enum LifecycleWaitKind
{
    Unknown,
    Temporal,
    Event,
    Synchronization,
    NoSuspension,
    YieldOnly,
    Blocking
}

public enum LifecycleCostUnit
{
    CostPerIteration,
    CostPerWorkItem,
    CostPerTime
}

public enum LifecycleFrequencySource
{
    None,
    DeclaredHypothesis,
    RequestedDelay
}

public enum LifecycleExecutionContextKind
{
    Unknown,
    ServiceLifecycle,
    EventDriven,
    Polling,
    Retry,
    Batch
}

public readonly struct LifecycleContext
{
    public LifecycleContext(
        MethodDeclarationSyntax method,
        WhileStatementSyntax loop,
        LifecycleWaitKind waitKind,
        bool observesCancellation,
        LifecycleCostUnit unit,
        double? requestedIterationsPerSecond,
        LifecycleExecutionContextKind executionKind = LifecycleExecutionContextKind.ServiceLifecycle,
        bool isDeclared = false,
        LifecycleFrequencySource frequencySource = LifecycleFrequencySource.None)
    {
        Method = method;
        Loop = loop;
        WaitKind = waitKind;
        ObservesCancellation = observesCancellation;
        Unit = unit;
        RequestedIterationsPerSecond = requestedIterationsPerSecond;
        ExecutionKind = executionKind;
        IsDeclared = isDeclared;
        FrequencySource = frequencySource;
    }

    public MethodDeclarationSyntax Method { get; }
    public WhileStatementSyntax Loop { get; }
    public LifecycleWaitKind WaitKind { get; }
    public bool ObservesCancellation { get; }
    public LifecycleCostUnit Unit { get; }
    public double? RequestedIterationsPerSecond { get; }
    public LifecycleExecutionContextKind ExecutionKind { get; }
    public bool IsDeclared { get; }
    public LifecycleFrequencySource FrequencySource { get; }
    public bool IsFrequencyHypothesis => FrequencySource == LifecycleFrequencySource.DeclaredHypothesis
        && RequestedIterationsPerSecond.HasValue;
}
