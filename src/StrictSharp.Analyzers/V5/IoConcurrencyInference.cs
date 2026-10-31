using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace StrictSharp.Analyzers.V5;

public static class IoConcurrencyInference
{
    public static CostEvidenceSet Analyze(
        SyntaxNode node,
        SemanticModel model,
        CancellationToken cancellationToken)
    {
        var evidence = ImmutableArray.CreateBuilder<CostEvidence>();
        foreach (var invocation in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var method = model.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
            var member = invocation.Expression as MemberAccessExpressionSyntax;
            var receiverType = member is not null
                ? model.GetTypeInfo(member.Expression, cancellationToken).Type?.ToDisplayString()
                : null;
            if ((method is null || method.ContainingType?.TypeKind == TypeKind.Error)
                && member is not null
                && (receiverType is null || method?.ContainingType?.TypeKind == TypeKind.Error))
            {
                evidence.Add(CostEvidence.Unknown(
                    CostDimension.Performance,
                    $"Alvo externo ou dispatch não resolvido em {member.Name.Identifier.ValueText}.",
                    "O alvo, a duração e a quantidade de trabalho permanecem desconhecidos."));
                continue;
            }

            var methodName = method?.Name ?? member?.Name.Identifier.ValueText;
            var containingType = method?.ContainingType?.ToDisplayString()
                ?? receiverType;
            var dimension = IsIo(containingType, methodName)
                ? CostDimension.Io
                : IsTaskCreation(containingType, methodName)
                    ? CostDimension.Concurrency
                    : (CostDimension?)null;
            if (dimension is not { } resolvedDimension)
                continue;

            var knownMultiplier = TryGetLoopMultiplier(invocation, out var multiplier);
            var count = knownMultiplier ? KnownCount.Exact(multiplier) : KnownCount.Unknown;
            evidence.Add(new CostEvidence(
                resolvedDimension,
                ComplexityClassification.Constant,
                count,
                count,
                PayloadEstimate.Unknown,
                knownMultiplier
                    ? $"Operação {methodName} potencialmente executada {multiplier} vez(es) no caminho estrutural."
                    : $"Operação {methodName} identificada com repetição dependente de entrada ou runtime.",
                "A quantidade considera somente loops constantes conhecidos.",
                resolvedDimension == CostDimension.Io
                    ? "Duração, latência, disponibilidade e volume transferido não são medidos."
                    : "Fila, execução simultânea e capacidade efetiva do runtime não são inferidas."));
        }

        return CostEvidenceSet.Create(evidence.ToImmutable());
    }

    private static bool IsIo(string? type, string? name)
    {
        if (type == "System.Net.Http.HttpClient")
            return name is "Send" or "SendAsync" or "GetAsync" or "PostAsync" or "PutAsync" or "DeleteAsync";

        if (type == "System.IO.File")
            return name?.StartsWith("Read", StringComparison.Ordinal) == true
                || name?.StartsWith("Write", StringComparison.Ordinal) == true
                || name?.StartsWith("Open", StringComparison.Ordinal) == true
                || name?.StartsWith("Append", StringComparison.Ordinal) == true;

        return type is not null
            && name is "Read" or "ReadAsync" or "Write" or "WriteAsync" or "Flush" or "FlushAsync" or "CopyTo" or "CopyToAsync"
            && (type.Contains("System.IO.Stream", StringComparison.Ordinal)
                || type.Contains("System.IO.TextReader", StringComparison.Ordinal)
                || type.Contains("System.IO.TextWriter", StringComparison.Ordinal));
    }

    private static bool IsTaskCreation(string? type, string? name)
    {
        return type == "System.Threading.Tasks.Task"
            && name is "Run" or "WhenAll" or "WhenAny";
    }

    private static bool TryGetLoopMultiplier(SyntaxNode node, out long multiplier)
    {
        multiplier = 1;
        foreach (var ancestor in node.Ancestors())
        {
            if (ancestor is not ForStatementSyntax
                and not ForEachStatementSyntax
                and not ForEachVariableStatementSyntax
                and not WhileStatementSyntax
                and not DoStatementSyntax)
                continue;

            if (!LoopCostInference.TryGetKnownIterationCount(ancestor, out var count)
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

}
