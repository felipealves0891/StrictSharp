namespace StrictSharp.Analyzers.V5;

public readonly struct CostComparison
{
    private CostComparison(
        bool isComparable,
        string complexityChange,
        string workChange,
        string limitations)
    {
        IsComparable = isComparable;
        ComplexityChange = complexityChange;
        WorkChange = workChange;
        Limitations = limitations;
    }

    public bool IsComparable { get; }
    public string ComplexityChange { get; }
    public string WorkChange { get; }
    public string Limitations { get; }

    public static CostComparison Compare(CostEvidence left, CostEvidence right)
    {
        var complexityChange = left.Complexity.IsKnown && right.Complexity.IsKnown
            ? $"{left.Complexity.Label} -> {right.Complexity.Label}"
            : "complexidade desconhecida em uma das alternativas";
        var workChange = left.Count.IsKnown && right.Count.IsKnown
            ? $"contagem estrutural {left.Count.Value} -> {right.Count.Value}"
            : "contagem estrutural desconhecida em uma das alternativas";
        var comparable = left.Dimension == right.Dimension
            && (left.Complexity.IsKnown && right.Complexity.IsKnown
                || left.Count.IsKnown && right.Count.IsKnown);

        return new CostComparison(
            comparable,
            complexityChange,
            workChange,
            "A comparação não estima percentual, custo monetário, latência ou resultado de benchmark.");
    }
}
