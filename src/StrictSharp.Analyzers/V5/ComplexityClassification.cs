namespace StrictSharp.Analyzers.V5;

public enum ComplexityKind
{
    Constant,
    Linear,
    Linearithmic,
    Quadratic,
    Unknown
}

public readonly struct ComplexityClassification
{
    private ComplexityClassification(ComplexityKind kind)
    {
        Kind = kind;
    }

    public ComplexityKind Kind { get; }
    public bool IsKnown => Kind != ComplexityKind.Unknown;

    public string Label => Kind switch
    {
        ComplexityKind.Constant => "O(1)",
        ComplexityKind.Linear => "O(n)",
        ComplexityKind.Linearithmic => "O(n log n)",
        ComplexityKind.Quadratic => "O(n²)",
        _ => "desconhecida"
    };

    public static ComplexityClassification Constant => new(ComplexityKind.Constant);
    public static ComplexityClassification Linear => new(ComplexityKind.Linear);
    public static ComplexityClassification Linearithmic => new(ComplexityKind.Linearithmic);
    public static ComplexityClassification Quadratic => new(ComplexityKind.Quadratic);
    public static ComplexityClassification Unknown => new(ComplexityKind.Unknown);
}
