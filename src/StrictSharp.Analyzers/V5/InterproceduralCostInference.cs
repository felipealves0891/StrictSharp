using Microsoft.CodeAnalysis;
using StrictSharp.Analyzers.Interprocedural;
using System.Collections.Immutable;

namespace StrictSharp.Analyzers.V5;

public static class InterproceduralCostInference
{
    public static CostEvidenceSet Analyze(
        Compilation compilation,
        IMethodSymbol root,
        CancellationToken cancellationToken)
    {
        var graph = InterproceduralGraphBuilder.Build(compilation, root, cancellationToken);
        var interprocedural = InterproceduralEvidenceAnalyzer.Analyze(compilation, graph, cancellationToken);
        var evidence = ImmutableArray.CreateBuilder<CostEvidence>();

        foreach (var item in interprocedural)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dimension = GetDimension(item);
            var complexity = item.Kind == EvidenceKind.RecursiveCall
                ? ComplexityClassification.Unknown
                : item.Kind == EvidenceKind.RepeatedCall
                    ? ComplexityClassification.Linear
                    : ComplexityClassification.Constant;
            var target = item.Target?.Name ?? "alvo desconhecido";
            evidence.Add(new CostEvidence(
                dimension,
                complexity,
                KnownCount.Unknown,
                KnownCount.Unknown,
                PayloadEstimate.Unknown,
                $"Evidência {item.Kind} propagada por {item.Caller.Name} -> {target}.",
                "O grafo interprocedural V4 resolveu a relação de chamada para este caminho.",
                item.Target is null
                    ? "O corpo ou alvo externo permanece desconhecido; a análise não trata isso como custo zero."
                    : "Frequência efetiva, entrada e comportamento do runtime não são medidos."));
        }

        return CostEvidenceSet.Create(evidence.ToImmutable());
    }

    public static CostEvidenceSet Merge(CostEvidenceSet first, CostEvidenceSet second)
    {
        if (first.IsEmpty)
            return second;
        if (second.IsEmpty)
            return first;

        var items = ImmutableArray.CreateBuilder<CostEvidence>(first.Items.Length + second.Items.Length);
        items.AddRange(first.Items);
        items.AddRange(second.Items);
        return CostEvidenceSet.Create(items.ToImmutable());
    }

    private static CostDimension GetDimension(InterproceduralEvidence evidence)
    {
        if (evidence.Kind == EvidenceKind.BlockingCall)
            return evidence.Dimension.Contains("I/O", StringComparison.Ordinal) ? CostDimension.Io : CostDimension.Concurrency;
        if (evidence.Kind is EvidenceKind.Allocation or EvidenceKind.Materialization)
            return CostDimension.Memory;
        if (evidence.Kind is EvidenceKind.RepeatedCall or EvidenceKind.RedundantWork)
            return CostDimension.Repetition;
        return CostDimension.Complexity;
    }
}
