using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace StrictSharp.Analyzers.Rules;

public enum RulePhase
{
    V1,
    V2,
    V3
}

public enum RiskSeverity
{
    Critical,
    High,
    Medium,
    Low
}

public readonly struct RuleDefinition
{
    public RuleDefinition(
        string id,
        string title,
        RiskSeverity severity,
        string category,
        string confidence,
        string impact,
        string scalability,
        string correction,
        string exceptions,
        string reference,
        RulePhase phase)
    {
        Id = id;
        Title = title;
        Severity = severity;
        Category = category;
        Confidence = confidence;
        Impact = impact;
        Scalability = scalability;
        Correction = correction;
        Exceptions = exceptions;
        Reference = reference;
        Phase = phase;
        var messageFormat = string.Join(
            " ",
            $"[{id}] {title}.",
            $"Severidade: {severity}.",
            $"Categoria: {category}.",
            "Evidência: padrão estrutural identificado nesta localização.",
            $"Impacto: {impact}",
            $"Escalabilidade: {scalability}",
            $"Correção: {correction}",
            $"Exceções: {exceptions}",
            $"Confiança: {confidence}.",
            "Exemplo: consulte o exemplo correspondente na documentação da regra.",
            $"Referência: {reference}.",
            "Limitação: custo efetivo, frequência e ganho percentual não são medidos pela análise estática.");
        Descriptor = new DiagnosticDescriptor(
            id,
            title,
            messageFormat,
            category,
            ToDiagnosticSeverity(severity),
            phase != RulePhase.V3,
            impact,
            reference);
        SupportedDiagnostics = ImmutableArray.Create(Descriptor);
    }

    public string Id { get; }
    public string Title { get; }
    public RiskSeverity Severity { get; }
    public string Category { get; }
    public string Confidence { get; }
    public string Impact { get; }
    public string Scalability { get; }
    public string Correction { get; }
    public string Exceptions { get; }
    public string Reference { get; }
    public RulePhase Phase { get; }
    public DiagnosticDescriptor Descriptor { get; }
    public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
    public bool EnabledInV1 => Phase == RulePhase.V1;
    public bool EnabledInV2 => Phase == RulePhase.V2;

    private static DiagnosticSeverity ToDiagnosticSeverity(RiskSeverity severity)
    {
        return severity switch
        {
            RiskSeverity.Critical => DiagnosticSeverity.Error,
            RiskSeverity.High => DiagnosticSeverity.Warning,
            RiskSeverity.Medium => DiagnosticSeverity.Info,
            _ => DiagnosticSeverity.Hidden
        };
    }
}
