using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Infrastructure;

public static class RuleAnalysis
{
    public static Diagnostic CreateDiagnostic(RuleDefinition rule, Location location)
    {
        return Diagnostic.Create(rule.Descriptor, location);
    }

    public static bool IsSourceLocation(Location location)
    {
        return location is { IsInSource: true, SourceTree: not null };
    }

    public static void Report(RuleDefinition rule, SyntaxNodeAnalysisContext context)
    {
        context.ReportDiagnostic(CreateDiagnostic(rule, context.Node.GetLocation()));
    }

    public static void Report(RuleDefinition rule, SyntaxNodeAnalysisContext context, Location location)
    {
        context.ReportDiagnostic(CreateDiagnostic(rule, location));
    }

    public static void Report(RuleDefinition rule, OperationAnalysisContext context, Location location)
    {
        context.ReportDiagnostic(CreateDiagnostic(rule, location));
    }

    public static bool IsTaskLike(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol named
            && named.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks"
            && (named.Name == "Task" || named.Name == "ValueTask");
    }

    public static bool IsTaskType(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol named
            && named.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks"
            && named.Name == "Task";
    }

    public static bool IsTaskRun(IMethodSymbol method)
    {
        return method.Name == "Run"
            && method.ContainingType is not null
            && method.ContainingType.ToDisplayString() == "System.Threading.Tasks.Task";
    }

    public static bool IsTaskWhenAll(IMethodSymbol method)
    {
        return method.Name == "WhenAll"
            && method.ContainingType?.ToDisplayString() == "System.Threading.Tasks.Task";
    }

    public static bool IsBlockingOperation(IMethodSymbol method)
    {
        if (method.Name is "Sleep" or "Wait" or "WaitOne" or "Join")
            return true;

        return IsSynchronousIo(method);
    }

    public static bool IsAwaiter(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol named
            && named.ContainingNamespace.ToDisplayString() == "System.Runtime.CompilerServices"
            && named.Name.EndsWith("Awaiter", StringComparison.Ordinal);
    }

    public static bool IsKnownCompletedTask(ExpressionSyntax expression, SemanticModel model)
    {
        expression = Unwrap(expression);

        if (expression is MemberAccessExpressionSyntax member)
        {
            var symbol = model.GetSymbolInfo(member).Symbol;
            if (symbol is IPropertySymbol property
                && property.Name == "CompletedTask"
                && IsTaskType(property.ContainingType))
                return true;

            if (member.Expression is InvocationExpressionSyntax awaiter
                && awaiter.Expression is MemberAccessExpressionSyntax awaiterMember
                && awaiterMember.Name.Identifier.Text == "GetAwaiter")
                return IsKnownCompletedTask(awaiterMember.Expression, model);
        }

        if (expression is InvocationExpressionSyntax invocation
            && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method)
        {
            if (method.Name == "FromResult"
                && method.ContainingType.ToDisplayString() == "System.Threading.Tasks.Task")
                return true;

            if (method.Name == "GetAwaiter"
                && IsTaskLike(method.ContainingType)
                && invocation.Expression is MemberAccessExpressionSyntax getAwaiter)
                return IsKnownCompletedTask(getAwaiter.Expression, model);
        }

        return false;
    }

    public static bool IsAsyncFlow(SemanticModel model, SyntaxNode node)
    {
        var symbol = model.GetEnclosingSymbol(node.SpanStart);
        return symbol is IMethodSymbol method
            && (method.IsAsync || IsTaskLike(method.ReturnType));
    }

    public static bool IsSynchronousIo(IMethodSymbol method)
    {
        if (method.Name.EndsWith("Async", StringComparison.Ordinal))
            return false;

        var containingType = method.ContainingType;
        if (containingType is null)
            return false;

        var fullName = containingType.ToDisplayString();
        if (fullName == "System.IO.File")
            return method.Name is "ReadAllText" or "ReadAllBytes" or "WriteAllText" or "WriteAllBytes"
                or "AppendAllText" or "OpenRead" or "OpenWrite";

        if (fullName == "System.Net.Http.HttpClient")
            return method.Name == "Send";

        if (fullName == "System.Net.WebClient")
            return method.Name.StartsWith("Download", StringComparison.Ordinal)
                || method.Name.StartsWith("Upload", StringComparison.Ordinal)
                || method.Name.StartsWith("OpenRead", StringComparison.Ordinal);

        return IsOrDerivedFrom(containingType, "System.IO.Stream")
            && method.Name is "Read" or "ReadByte" or "Write" or "WriteByte" or "Flush" or "CopyTo"
            || IsOrDerivedFrom(containingType, "System.IO.TextReader")
                && method.Name is "Read" or "ReadLine" or "ReadToEnd" or "ReadBlock"
            || IsOrDerivedFrom(containingType, "System.IO.TextWriter")
                && method.Name is "Write" or "WriteLine" or "Flush";
    }

    public static bool IsInsideLoop(SyntaxNode node)
    {
        return node.Ancestors().Any(ancestor => ancestor is ForStatementSyntax
            or ForEachStatementSyntax
            or ForEachVariableStatementSyntax
            or WhileStatementSyntax
            or DoStatementSyntax);
    }

    public static bool IsString(ITypeSymbol? type)
    {
        return type?.SpecialType == SpecialType.System_String;
    }

    public static bool IsEnumerable(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
            return false;

        if (named.Name == "IEnumerable"
            && named.ContainingNamespace.ToDisplayString() == "System.Collections.Generic")
            return true;

        return named.AllInterfaces.Any(interfaceType =>
            interfaceType.Name == "IEnumerable"
            && interfaceType.ContainingNamespace.ToDisplayString() == "System.Collections.Generic");
    }

    public static bool IsListLike(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol named
            && named.ContainingNamespace.ToDisplayString() == "System.Collections.Generic"
            && named.Name is "List" or "IList" or "IReadOnlyList";
    }

    public static bool IsDictionaryLike(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol named
            && named.ContainingNamespace.ToDisplayString() == "System.Collections.Generic"
            && named.Name is "Dictionary" or "IDictionary" or "IReadOnlyDictionary";
    }

    public static bool HasEfficientCount(ITypeSymbol? type)
    {
        if (type is IArrayTypeSymbol || IsString(type))
            return true;

        return type is INamedTypeSymbol named
            && named.Name is "ICollection" or "IReadOnlyCollection"
            && named.ContainingNamespace.ToDisplayString() == "System.Collections.Generic"
            || type is INamedTypeSymbol collection
                && collection.Name == "ICollection"
                && collection.ContainingNamespace.ToDisplayString() == "System.Collections";
    }

    public static bool IsLogger(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol named
            && (named.Name == "ILogger"
                || named.ToDisplayString().StartsWith("Microsoft.Extensions.Logging.ILogger", StringComparison.Ordinal));
    }

    public static bool IsStringCopy(IMethodSymbol method)
    {
        return method.ContainingType?.SpecialType == SpecialType.System_String
            && method.Name is "Substring" or "ToCharArray";
    }

    public static bool IsStringNormalizationComparison(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method
            || method.ContainingType?.SpecialType != SpecialType.System_String
            || method.Name is not ("ToLower" or "ToLowerInvariant" or "ToUpper" or "ToUpperInvariant"))
            return false;

        if (invocation.Parent is BinaryExpressionSyntax binary
            && (binary.IsKind(SyntaxKind.EqualsExpression)
                || binary.IsKind(SyntaxKind.NotEqualsExpression)))
            return true;

        if (invocation.Parent?.Parent is InvocationExpressionSyntax parentInvocation
            && model.GetSymbolInfo(parentInvocation).Symbol is IMethodSymbol parentMethod)
            return parentMethod.Name is "Equals" or "Compare";

        return false;
    }

    private static bool IsOrDerivedFrom(INamedTypeSymbol type, string fullName)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == fullName)
                return true;
        }

        return false;
    }

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        return expression;
    }
}
