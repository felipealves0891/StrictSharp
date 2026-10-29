using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf028Analyzer : RuleAnalyzerBase
{
    public Perf028Analyzer() : base("PERF028") { }
    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(static context =>
        {
            if (context.Node is not ObjectCreationExpressionSyntax creation
                || context.SemanticModel.GetTypeInfo(creation).Type is not INamedTypeSymbol type
                || !type.AllInterfaces.Any(interfaceType =>
                    interfaceType.ToDisplayString() is "System.IDisposable" or "System.IAsyncDisposable")
                || creation.Ancestors().OfType<UsingStatementSyntax>().Any())
                return;

            if (creation.Parent?.Parent is VariableDeclaratorSyntax variable
                && variable.Parent?.Parent is LocalDeclarationStatementSyntax declaration
                && declaration.UsingKeyword != default)
                return;
            if (creation.Parent is ReturnStatementSyntax or ArrowExpressionClauseSyntax
                || creation.Parent is ArgumentSyntax
                || creation.Parent is AssignmentExpressionSyntax assignment
                    && context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol is IPropertySymbol or IFieldSymbol)
                return;
            if (creation.Parent is EqualsValueClauseSyntax initializer
                && initializer.Parent is VariableDeclaratorSyntax declarator
                && context.SemanticModel.GetDeclaredSymbol(declarator) is ILocalSymbol localSymbol)
            {
                var method = creation.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
                if (method?.DescendantNodes().OfType<IdentifierNameSyntax>().Any(identifier =>
                    SymbolEqualityComparer.Default.Equals(context.SemanticModel.GetSymbolInfo(identifier).Symbol, localSymbol)
                    && identifier.Parent is MemberAccessExpressionSyntax member
                    && member.Parent is InvocationExpressionSyntax invocation
                    && member.Name.Identifier.ValueText is "Dispose" or "DisposeAsync") == true)
                    return;
                if (method?.DescendantNodes().OfType<IdentifierNameSyntax>().Any(identifier =>
                    SymbolEqualityComparer.Default.Equals(context.SemanticModel.GetSymbolInfo(identifier).Symbol, localSymbol)
                    && identifier.Parent is ArgumentSyntax or AssignmentExpressionSyntax) == true)
                    return;
            }

            RuleAnalysis.Report(RuleCatalog.Get("PERF028"), context, creation.Type.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ObjectCreationExpression);
    }
}
