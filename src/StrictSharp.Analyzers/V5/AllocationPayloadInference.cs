using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace StrictSharp.Analyzers.V5;

public static class AllocationPayloadInference
{
    public static CostEvidenceSet Analyze(
        SyntaxNode node,
        SemanticModel model,
        CancellationToken cancellationToken)
    {
        var evidence = ImmutableArray.CreateBuilder<CostEvidence>();
        foreach (var allocation in node.DescendantNodes().OfType<ArrayCreationExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var payload = TryGetPayload(allocation, model, out var bytes)
                ? PayloadEstimate.Known(bytes)
                : PayloadEstimate.Unknown;
            var allocationCount = TryGetLoopMultiplier(allocation, model, out var multiplier)
                ? KnownCount.Exact(multiplier)
                : KnownCount.Unknown;
            var totalPayload = payload.IsKnown && allocationCount.IsKnown
                ? PayloadEstimate.Known(checked(payload.Bytes * allocationCount.Value))
                : PayloadEstimate.Unknown;
            var limitation = totalPayload.IsKnown
                ? "Cabeçalhos, alinhamento, sobrevivência e pausas de GC não são calculados."
                : "O tamanho ou a quantidade depende de entrada, runtime ou estado não resolvido.";

            evidence.Add(new CostEvidence(
                CostDimension.Memory,
                ComplexityClassification.Constant,
                allocationCount,
                allocationCount,
                totalPayload,
                totalPayload.IsKnown
                    ? $"Payload de array estimado em {totalPayload.Bytes} bytes."
                    : "Alocação de array identificada com payload parcial ou desconhecido.",
                "O cálculo considera somente o payload dos elementos e multiplicadores estruturais conhecidos.",
                limitation));
        }

        return CostEvidenceSet.Create(evidence.ToImmutable());
    }

    private static bool TryGetPayload(
        ArrayCreationExpressionSyntax allocation,
        SemanticModel model,
        out long bytes)
    {
        bytes = 0;
        if (model.GetTypeInfo(allocation.Type.ElementType).Type is not { } elementType
            || !TryGetElementSize(elementType.SpecialType, out var elementSize))
            return false;

        long elementCount = 1;
        foreach (var rank in allocation.Type.RankSpecifiers)
        {
            if (rank.Sizes.Count != 1
                || rank.Sizes[0] is not LiteralExpressionSyntax literal
                || !literal.IsKind(SyntaxKind.NumericLiteralExpression)
                || !long.TryParse(literal.Token.ValueText, out var dimension)
                || dimension < 0
                || elementCount > long.MaxValue / Math.Max(1, dimension))
                return false;

            elementCount *= dimension;
        }

        if (elementCount > long.MaxValue / elementSize)
            return false;

        bytes = elementCount * elementSize;
        return true;
    }

    private static bool TryGetLoopMultiplier(SyntaxNode node, SemanticModel model, out long multiplier)
    {
        multiplier = 1;
        foreach (var ancestor in node.Ancestors())
        {
            if (!IsLoop(ancestor))
                continue;

            if (!LoopCostInference.TryGetKnownIterationCount(ancestor, model, out var count)
                || count == 0
                || multiplier > long.MaxValue / count)
            {
                multiplier = 0;
                return false;
            }

            multiplier *= count;
        }

        return true;
    }

    private static bool IsLoop(SyntaxNode node)
    {
        return node is ForStatementSyntax
            or ForEachStatementSyntax
            or ForEachVariableStatementSyntax
            or WhileStatementSyntax
            or DoStatementSyntax;
    }

    private static bool TryGetElementSize(SpecialType type, out long size)
    {
        size = type switch
        {
            SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_SByte => 1,
            SpecialType.System_Char or SpecialType.System_Int16 or SpecialType.System_UInt16 => 2,
            SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Single => 4,
            SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Double => 8,
            SpecialType.System_Decimal => 16,
            _ => 0
        };
        return size != 0;
    }
}
