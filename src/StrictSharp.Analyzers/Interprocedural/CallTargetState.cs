namespace StrictSharp.Analyzers.Interprocedural;

public enum CallTargetState
{
    Known,
    Unknown
}

public enum CallFlowKind
{
    Synchronous,
    Awaited,
    Blocking,
    Unknown
}
