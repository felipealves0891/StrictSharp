using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace StrictSharp.Analyzers.V5;

public static class LoopCostInference
{
    public static CostEvidenceSet Analyze(SyntaxNode node, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var loops = node.DescendantNodes()
            .Where(IsLoop)
            .ToArray();
        var hasSort = node.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(IsKnownSort);

        if (loops.Length == 0)
        {
            if (!hasSort)
                return CostEvidenceSet.Empty;

            return CostEvidenceSet.Create(ImmutableArray.Create(new CostEvidence(
                CostDimension.Complexity,
                ComplexityClassification.Linearithmic,
                KnownCount.Unknown,
                KnownCount.Unknown,
                PayloadEstimate.Unknown,
                "Ordenação conhecida identificada no caminho analisado.",
                "A forma típica da ordenação foi usada como evidência estrutural.",
                "O tamanho efetivo da entrada e o custo do comparador não são medidos.")));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var depth = loops.Max(GetLoopDepth);
        var complexity = depth switch
        {
            1 => ComplexityClassification.Linear,
            2 => ComplexityClassification.Quadratic,
            _ => ComplexityClassification.Unknown
        };
        var knownCounts = loops.Select(GetKnownCount).ToArray();
        var count = loops.Length == 1 ? knownCounts[0] : KnownCount.Unknown;
        var multiplier = TryMultiply(knownCounts, out var product)
            ? KnownCount.Exact(product)
            : KnownCount.Unknown;
        var limitation = complexity.IsKnown
            ? "Limites dinâmicos, entrada efetiva e comportamento do runtime não são medidos."
            : "A profundidade ou a forma do loop excede as classificações suportadas.";

        return CostEvidenceSet.Create(ImmutableArray.Create(new CostEvidence(
            CostDimension.Complexity,
            complexity,
            count,
            multiplier,
            PayloadEstimate.Unknown,
            $"{loops.Length} loop(s) observável(is) no caminho analisado.",
            "A classificação usa somente a estrutura sintática observável.",
            limitation)));
    }

    public static ComplexityClassification Classify(SyntaxNode node)
    {
        var evidence = Analyze(node, CancellationToken.None);
        return evidence.IsEmpty
            ? ComplexityClassification.Constant
            : evidence.Items[0].Complexity;
    }

    public static bool TryGetKnownIterationCount(SyntaxNode loop, out long count)
    {
        var known = GetKnownCount(loop);
        count = known.Value;
        return known.IsKnown;
    }

    private static bool IsLoop(SyntaxNode node)
    {
        return node is ForStatementSyntax
            or ForEachStatementSyntax
            or ForEachVariableStatementSyntax
            or WhileStatementSyntax
            or DoStatementSyntax;
    }

    private static int GetLoopDepth(SyntaxNode loop)
    {
        return loop.Ancestors().Count(IsLoop) + 1;
    }

    private static KnownCount GetKnownCount(SyntaxNode loop)
    {
        if (loop is not ForStatementSyntax forStatement
            || forStatement.Condition is not BinaryExpressionSyntax condition
            || condition.Right is not LiteralExpressionSyntax literal
            || !literal.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression)
            || !long.TryParse(literal.Token.ValueText, out var end)
            || forStatement.Initializers.Count > 1)
            return KnownCount.Unknown;

        var startText = forStatement.Declaration?.Variables.Count == 1
            ? forStatement.Declaration.Variables[0].Initializer?.Value.ToString()
            : forStatement.Initializers.Count == 1
                ? forStatement.Initializers[0] switch
                {
                    AssignmentExpressionSyntax assignment => assignment.Right.ToString(),
                    _ => null
                }
                : null;
        if (!long.TryParse(startText, out var start) || end < start)
            return KnownCount.Unknown;

        return KnownCount.Exact(end - start);
    }

    private static bool TryMultiply(IReadOnlyList<KnownCount> counts, out long product)
    {
        product = 1;
        foreach (var count in counts)
        {
            if (!count.IsKnown || count.Value == 0 || product > long.MaxValue / count.Value)
            {
                product = 0;
                return false;
            }

            product *= count.Value;
        }

        return true;
    }

    private static bool IsKnownSort(InvocationExpressionSyntax invocation)
    {
        var name = invocation.Expression is MemberAccessExpressionSyntax member
            ? member.Name.Identifier.ValueText
            : string.Empty;
        return name is "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending" or "Sort";
    }
}
