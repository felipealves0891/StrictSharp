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

            RuleAnalysis.Report(RuleCatalog.Get("PERF028"), context, creation.Type.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ObjectCreationExpression);
    }
}
