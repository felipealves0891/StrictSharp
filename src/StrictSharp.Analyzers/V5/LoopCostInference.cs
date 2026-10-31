using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;
using System.Collections.Immutable;

namespace StrictSharp.Analyzers.V5;

public static class LoopCostInference
{
    public static CostEvidenceSet Analyze(
        SyntaxNode node,
        CancellationToken cancellationToken,
        SemanticModel? semanticModel = null)
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
        var knownCounts = loops.Select(loop => GetKnownCount(loop, semanticModel, cancellationToken)).ToArray();
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
        return TryGetKnownIterationCount(loop, semanticModel: null, out count);
    }

    public static bool TryGetKnownIterationCount(
        SyntaxNode loop,
        SemanticModel? semanticModel,
        out long count,
        CancellationToken cancellationToken = default)
    {
        var known = GetKnownCount(loop, semanticModel, cancellationToken);
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

    private static KnownCount GetKnownCount(
        SyntaxNode loop,
        SemanticModel? semanticModel,
        CancellationToken cancellationToken)
    {
        if (loop is not ForStatementSyntax forStatement
            || semanticModel is null
            || forStatement.Condition is not BinaryExpressionSyntax condition
            || condition.Left is not IdentifierNameSyntax conditionVariable
            || condition.Kind() is not (SyntaxKind.LessThanExpression or SyntaxKind.GreaterThanExpression)
            || !TryGetInitializer(forStatement, out var variable, out var start)
            || conditionVariable.Identifier.ValueText != variable
            || !TryParseInteger(condition.Right, out var end)
            || !HasUnitIncrement(forStatement, variable, condition.IsKind(SyntaxKind.LessThanExpression)))
            return KnownCount.Unknown;

        var variableType = semanticModel.GetTypeInfo(conditionVariable, cancellationToken).Type?.SpecialType;
        if (variableType is not (SpecialType.System_Int32 or SpecialType.System_Int64))
            return KnownCount.Unknown;

        if (variableType == SpecialType.System_Int32
            && (start is < int.MinValue or > int.MaxValue || end is < int.MinValue or > int.MaxValue))
            return KnownCount.Unknown;

        try
        {
            if (condition.IsKind(SyntaxKind.LessThanExpression))
                return start >= end ? KnownCount.Exact(0) : KnownCount.Exact(checked(end - start));

            return start <= end ? KnownCount.Exact(0) : KnownCount.Exact(checked(start - end));
        }
        catch (OverflowException)
        {
            return KnownCount.Unknown;
        }
    }

    private static bool TryGetInitializer(ForStatementSyntax statement, out string variable, out long start)
    {
        variable = string.Empty;
        start = 0;
        ExpressionSyntax? value;
        if (statement.Declaration is { Variables.Count: 1 } declaration)
        {
            variable = declaration.Variables[0].Identifier.ValueText;
            value = declaration.Variables[0].Initializer?.Value;
        }
        else if (statement.Declaration is null
            && statement.Initializers.Count == 1
            && statement.Initializers[0] is AssignmentExpressionSyntax assignment
            && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            && assignment.Left is IdentifierNameSyntax identifier)
        {
            variable = identifier.Identifier.ValueText;
            value = assignment.Right;
        }
        else
        {
            return false;
        }

        return value is not null && TryParseInteger(value, out start);
    }

    private static bool TryParseInteger(ExpressionSyntax expression, out long value)
    {
        value = 0;
        if (expression is LiteralExpressionSyntax literal
            && literal.IsKind(SyntaxKind.NumericLiteralExpression))
        {
            try
            {
                value = Convert.ToInt64(literal.Token.Value, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException)
            {
                return false;
            }
        }

        if (expression is PrefixUnaryExpressionSyntax negative
            && negative.IsKind(SyntaxKind.UnaryMinusExpression)
            && TryParseInteger(negative.Operand, out var positive))
        {
            try
            {
                value = checked(-positive);
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        return false;
    }

    private static bool HasUnitIncrement(ForStatementSyntax statement, string variable, bool incrementing)
    {
        if (statement.Incrementors.Count != 1)
            return false;

        var expression = statement.Incrementors[0];
        if (expression is PrefixUnaryExpressionSyntax prefix)
            return IsUnitUnaryUpdate(prefix.Operand, prefix.Kind(), variable, incrementing);
        if (expression is PostfixUnaryExpressionSyntax postfix)
            return IsUnitUnaryUpdate(postfix.Operand, postfix.Kind(), variable, incrementing);
        return false;
    }

    private static bool IsUnitUnaryUpdate(
        ExpressionSyntax operand,
        SyntaxKind updateKind,
        string variable,
        bool incrementing)
    {
        if (operand is not IdentifierNameSyntax identifier
            || identifier.Identifier.ValueText != variable)
            return false;

        return incrementing
            ? updateKind is SyntaxKind.PreIncrementExpression or SyntaxKind.PostIncrementExpression
            : updateKind is SyntaxKind.PreDecrementExpression or SyntaxKind.PostDecrementExpression;
    }

    private static bool TryMultiply(IReadOnlyList<KnownCount> counts, out long product)
    {
        product = 1;
        foreach (var count in counts)
        {
            if (!count.IsKnown)
            {
                product = 0;
                return false;
            }

            if (count.Value == 0)
            {
                product = 0;
                continue;
            }
            if (product > long.MaxValue / count.Value)
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
