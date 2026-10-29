using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf006Analyzer : RuleAnalyzerBase
{
    public Perf006Analyzer() : base("PERF006") { }
    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(static context =>
        {
            var model = context.SemanticModel;
            if (context.Node is ObjectCreationExpressionSyntax creation
                && RuleAnalysis.IsInsideLoop(creation)
                && model.GetTypeInfo(creation).Type is INamedTypeSymbol type
                && type.ToDisplayString() == "System.Threading.Thread")
            {
                RuleAnalysis.Report(RuleCatalog.Get("PERF006"), context, creation.GetLocation());
                return;
            }

            if (context.Node is InvocationExpressionSyntax invocation
                && RuleAnalysis.IsInsideLoop(invocation)
                && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
                && RuleAnalysis.IsTaskRun(method))
                RuleAnalysis.Report(RuleCatalog.Get("PERF006"), context, invocation.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ObjectCreationExpression,
           Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
    }
}
