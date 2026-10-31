namespace StrictSharp.Analyzers.Interprocedural;

public enum EvidenceKind
{
    RepeatedCall,
    Allocation,
    Materialization,
    RedundantWork,
    BlockingCall,
    RecursiveCall
}
