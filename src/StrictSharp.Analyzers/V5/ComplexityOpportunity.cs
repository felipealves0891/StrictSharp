namespace StrictSharp.Analyzers.V5;

public readonly struct ComplexityOpportunity
{
    private ComplexityOpportunity(bool isKnown, string explanation, string limitations)
    {
        IsKnown = isKnown;
        Explanation = explanation;
        Limitations = limitations;
    }

    public bool IsKnown { get; }
    public string Explanation { get; }
    public string Limitations { get; }

    public static ComplexityOpportunity None => new(
        isKnown: false,
        string.Empty,
        "Nenhuma oportunidade de melhoria estrutural foi demonstrada.");

    public static ComplexityOpportunity RepeatedListSearch => new(
        isKnown: true,
        "Busca linear repetida em List<T> com chave estável; uma estrutura baseada em hash pode reduzir a forma de busca quando a equivalência semântica for preservada.",
        "A análise não mede frequência real, custo de construção, CPU ou ganho percentual.");
}
