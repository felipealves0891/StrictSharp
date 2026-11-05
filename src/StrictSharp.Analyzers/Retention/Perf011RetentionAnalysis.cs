using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Retention;

internal static class Perf011RetentionAnalysis
{
    public static bool ShouldReport(
        InvocationExpressionSyntax invocation,
        SemanticModel model)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member
            || member.Name.Identifier.ValueText is not ("Add" or "AddRange" or "Subscribe")
            || GetField(member, model) is not { } field
            || !IsRetainingOperation(invocation, field, model)
            || !RuleAnalysis.IsInsideLoop(invocation))
            return false;

        if (IsTransientOwnerWithoutEscape(member, invocation, model))
            return false;

        if (HasReleaseOperation(invocation, field, model)
            || HasExplicitBound(invocation, field, model))
            return false;

        return true;
    }

    private static IFieldSymbol? GetField(
        MemberAccessExpressionSyntax member,
        SemanticModel model)
    {
        return model.GetSymbolInfo(member.Expression).Symbol as IFieldSymbol;
    }

    private static bool IsTransientOwnerWithoutEscape(
        MemberAccessExpressionSyntax member,
        InvocationExpressionSyntax invocation,
        SemanticModel model)
    {
        if (member.Expression is not ExpressionSyntax ownerExpression
            || GetRootIdentifier(ownerExpression) is not { } root)
            return false;

        var ownerSymbol = model.GetSymbolInfo(root).Symbol;
        if (ownerSymbol is not ILocalSymbol owner)
            return ownerSymbol is IParameterSymbol;

        if (!IsCreatedLocally(owner, model))
            return true;

        var containingMethod = invocation.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod is null)
            return false;

        var evidence = ClassifyOwnerUses(containingMethod, owner, invocation, model);
        return evidence != OwnerEscapeEvidence.LongLivedEscape;
    }

    private static IdentifierNameSyntax? GetRootIdentifier(ExpressionSyntax expression)
    {
        while (true)
        {
            switch (expression)
            {
                case IdentifierNameSyntax identifier:
                    return identifier;
                case MemberAccessExpressionSyntax member:
                    expression = member.Expression;
                    continue;
                case ParenthesizedExpressionSyntax parenthesized:
                    expression = parenthesized.Expression;
                    continue;
                default:
                    return null;
            }
        }
    }

    private static bool IsCreatedLocally(ILocalSymbol owner, SemanticModel model)
    {
        var declaration = owner.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;
        return declaration?.Initializer?.Value is ObjectCreationExpressionSyntax;
    }

    private static OwnerEscapeEvidence ClassifyOwnerUses(
        BaseMethodDeclarationSyntax containingMethod,
        ILocalSymbol owner,
        InvocationExpressionSyntax currentInvocation,
        SemanticModel model)
    {
        var evidence = OwnerEscapeEvidence.NoEscape;
        foreach (var identifier in containingMethod.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (!SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(identifier).Symbol, owner)
                || identifier == GetRootIdentifier(((MemberAccessExpressionSyntax)currentInvocation.Expression).Expression))
                continue;

            if (IsLongLivedEscape(identifier, model))
                return OwnerEscapeEvidence.LongLivedEscape;

            evidence = OwnerEscapeEvidence.UnknownEscape;
        }

        return evidence;
    }

    private static bool IsLongLivedEscape(IdentifierNameSyntax identifier, SemanticModel model)
    {
        if (identifier.Parent is ReturnStatementSyntax)
            return true;

        if (identifier.Parent is ArgumentSyntax
            && identifier.Parent.Parent?.Parent is InvocationExpressionSyntax invocation)
            return model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
                && method.ContainingType?.IsStatic == true;

        if (identifier.Parent is AssignmentExpressionSyntax assignment
            && model.GetSymbolInfo(assignment.Left).Symbol is IFieldSymbol field)
            return field.IsStatic;

        return identifier.Ancestors().Any(node => node is AnonymousFunctionExpressionSyntax);
    }

    private enum OwnerEscapeEvidence
    {
        NoEscape,
        UnknownEscape,
        LongLivedEscape
    }

    private static bool IsRetainingOperation(
        InvocationExpressionSyntax invocation,
        IFieldSymbol field,
        SemanticModel model)
    {
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method
            || method.Name is not ("Add" or "AddRange" or "Subscribe"))
            return false;

        var receiverType = field.Type;
        if (method.Name is "Add" or "AddRange")
            return RuleAnalysis.IsRetainingCollection(receiverType);

        return RuleAnalysis.IsObservableSource(receiverType);
    }

    private static bool HasReleaseOperation(
        InvocationExpressionSyntax invocation,
        IFieldSymbol field,
        SemanticModel model)
    {
        if (HasReleaseOperationInMethod(invocation, field, model))
            return true;

        if (!IsDisposableOwner(field.ContainingType))
            return false;

        var declaration = field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()
            .AncestorsAndSelf()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();
        if (declaration is null)
            return false;

        foreach (var method in declaration.Members.OfType<MethodDeclarationSyntax>())
        {
            if (method.Identifier.ValueText != "Dispose")
                continue;

            foreach (var release in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (IsReleaseForField(release, field, model))
                    return true;
            }
        }

        return false;
    }

    private static bool HasReleaseOperationInMethod(
        InvocationExpressionSyntax invocation,
        IFieldSymbol field,
        SemanticModel model)
    {
        var containingMethod = invocation.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod is null)
            return false;

        foreach (var release in containingMethod.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (IsReleaseForField(release, field, model))
                return true;
        }

        return false;
    }

    private static bool IsReleaseForField(
        InvocationExpressionSyntax invocation,
        IFieldSymbol field,
        SemanticModel model)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member
            || member.Expression is not IdentifierNameSyntax receiver
            || !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, field))
            return false;

        return member.Name.Identifier.ValueText is "Clear" or "Remove" or "RemoveAll" or "Dequeue" or "Pop";
    }

    private static bool IsDisposableOwner(INamedTypeSymbol? type)
    {
        return type?.AllInterfaces.Any(interfaceType =>
            interfaceType.SpecialType == SpecialType.System_IDisposable) == true;
    }

    private static bool HasExplicitBound(
        InvocationExpressionSyntax invocation,
        IFieldSymbol field,
        SemanticModel model)
    {
        foreach (var condition in invocation.Ancestors().OfType<IfStatementSyntax>())
        {
            if (!condition.Statement.Span.Contains(invocation.Span)
                || !IsBoundComparison(condition.Condition, field, model))
                continue;

            return true;
        }

        return false;
    }

    private static bool IsBoundComparison(
        ExpressionSyntax condition,
        IFieldSymbol field,
        SemanticModel model)
    {
        if (condition is not BinaryExpressionSyntax binary
            || binary.Kind() is not (Microsoft.CodeAnalysis.CSharp.SyntaxKind.LessThanExpression
                or Microsoft.CodeAnalysis.CSharp.SyntaxKind.LessThanOrEqualExpression))
            return false;

        return IsCountAccess(binary.Left, field, model)
            && model.GetConstantValue(binary.Right).HasValue
            && model.GetConstantValue(binary.Right).Value is int;
    }

    private static bool IsCountAccess(
        ExpressionSyntax expression,
        IFieldSymbol field,
        SemanticModel model)
    {
        return expression is MemberAccessExpressionSyntax member
            && member.Name.Identifier.ValueText == "Count"
            && member.Expression is IdentifierNameSyntax receiver
            && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, field);
    }
}
