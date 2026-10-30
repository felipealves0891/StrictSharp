using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf030Analyzer : RuleAnalyzerBase
{
    public Perf030Analyzer() : base("PERF030") { }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(static context =>
        {
            if (context.Node is not ObjectCreationExpressionSyntax creation
                || !RuleAnalysis.IsInsideLoop(creation)
                || !HasInvariantArguments(creation, context.SemanticModel)
                || !HasComplexInitialization(creation, context.SemanticModel))
                return;

            RuleAnalysis.Report(RuleCatalog.Get("PERF030"), context, creation.Type.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ObjectCreationExpression);
    }

    private static bool HasInvariantArguments(ObjectCreationExpressionSyntax creation, SemanticModel model)
    {
        var loop = creation.Ancestors().FirstOrDefault(node => node is ForStatementSyntax
            or ForEachStatementSyntax
            or ForEachVariableStatementSyntax
            or WhileStatementSyntax
            or DoStatementSyntax);
        if (loop is WhileStatementSyntax or DoStatementSyntax)
            return false;

        var loopSymbols = Array.Empty<ISymbol>();
        if (loop is ForEachStatementSyntax forEach)
        {
            loopSymbols = model.GetDeclaredSymbol(forEach) is { } symbol
                ? new[] { symbol }
                : Array.Empty<ISymbol>();
        }

        if (loop is ForStatementSyntax forStatement)
        {
            loopSymbols = forStatement.Declaration?.Variables
                .Select(variable => model.GetDeclaredSymbol(variable))
                .Where(symbol => symbol is not null)
                .Cast<ISymbol>()
                .ToArray() ?? Array.Empty<ISymbol>();
        }

        if (loop is not (ForEachStatementSyntax or ForStatementSyntax))
            return false;

        if (creation.ArgumentList?.Arguments.Any(argument =>
            UsesLoopSymbol(argument.Expression, loopSymbols, model)) == true)
            return false;

        return creation.Initializer?.Expressions.Any(expression =>
            UsesLoopSymbol(expression, loopSymbols, model)) != true;
    }

    private static bool UsesLoopSymbol(
        SyntaxNode expression,
        IReadOnlyCollection<ISymbol> loopSymbols,
        SemanticModel model)
    {
        return expression.DescendantNodesAndSelf()
            .OfType<IdentifierNameSyntax>()
            .Any(identifier => loopSymbols.Any(symbol =>
                SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(identifier).Symbol, symbol)));
    }

    private static bool HasComplexInitialization(
        ObjectCreationExpressionSyntax creation,
        SemanticModel model)
    {
        if (creation.Initializer is not null)
            return creation.Initializer.Expressions.Any(expression =>
                expression.DescendantNodesAndSelf().Any(node => node is InvocationExpressionSyntax
                    or ObjectCreationExpressionSyntax
                    or ConditionalExpressionSyntax));

        if (model.GetSymbolInfo(creation).Symbol is not IMethodSymbol constructor)
            return false;

        foreach (var declaration in constructor.DeclaringSyntaxReferences)
        {
            if (declaration.GetSyntax() is not ConstructorDeclarationSyntax syntax)
                continue;

            if (syntax.DescendantNodes().Any(node => node is InvocationExpressionSyntax
                or ObjectCreationExpressionSyntax
                or ConditionalExpressionSyntax))
                return true;
        }

        return false;
    }
}
