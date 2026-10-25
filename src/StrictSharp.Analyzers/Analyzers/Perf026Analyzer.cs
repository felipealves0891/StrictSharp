using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf026Analyzer : RuleAnalyzerBase
{
    public Perf026Analyzer()
        : base("PERF026")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation
            || context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method
            || !RuleAnalysis.IsAsyncFlow(context.SemanticModel, invocation)
            || !RuleAnalysis.IsSynchronousIo(method))
            return;

        RuleAnalysis.Report(Rule, context, invocation.GetLocation());
    }
}
