using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf014Analyzer : RuleAnalyzerBase
{
    public Perf014Analyzer()
        : base("PERF014")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation
            || !RuleAnalysis.IsInsideLoop(invocation)
            || invocation.Expression is not MemberAccessExpressionSyntax member
            || context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method
            || method.Name is not ("Contains" or "IndexOf" or "Find" or "First" or "FirstOrDefault" or "SingleOrDefault")
            || !RuleAnalysis.IsListLike(context.SemanticModel.GetTypeInfo(member.Expression).Type))
            return;

        RuleAnalysis.Report(Rule, context);
    }
}
