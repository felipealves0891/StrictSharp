using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf021Analyzer : RuleAnalyzerBase
{
    public Perf021Analyzer() : base("PERF021") { }
    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(static context =>
        {
            if (context.Node is not ObjectCreationExpressionSyntax creation
                || !RuleAnalysis.IsInsideLoop(creation)
                || context.SemanticModel.GetTypeInfo(creation).Type is not INamedTypeSymbol type
                || type.ToDisplayString() != "System.Text.RegularExpressions.Regex")
                return;

            RuleAnalysis.Report(RuleCatalog.Get("PERF021"), context, creation.Type.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ObjectCreationExpression);
    }
}
