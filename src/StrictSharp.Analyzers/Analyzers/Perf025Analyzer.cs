using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf025Analyzer : RuleAnalyzerBase
{
    public Perf025Analyzer()
        : base("PERF025")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ObjectCreationExpression);
    }

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ObjectCreationExpressionSyntax creation
            || context.SemanticModel.GetTypeInfo(creation).Type is not INamedTypeSymbol type
            || type.ToDisplayString() != "System.Net.Http.HttpClient"
            || creation.Ancestors().All(ancestor => ancestor is not BaseMethodDeclarationSyntax))
            return;

        RuleAnalysis.Report(Rule, context, creation.Type.GetLocation());
    }
}
