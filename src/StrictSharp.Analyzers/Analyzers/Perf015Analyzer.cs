using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf015Analyzer : RuleAnalyzerBase
{
    public Perf015Analyzer()
        : base("PERF015")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation
            || invocation.Expression is not MemberAccessExpressionSyntax member
            || !IsCountOrAny(invocation, member, context.SemanticModel)
            || invocation.ArgumentList.Arguments.Count != 0
            || !RuleAnalysis.HasEfficientCount(context.SemanticModel.GetTypeInfo(member.Expression).Type))
            return;

        RuleAnalysis.Report(Rule, context);
    }

    private static bool IsCountOrAny(InvocationExpressionSyntax invocation, MemberAccessExpressionSyntax member, SemanticModel model)
    {
        var method = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (method is not null)
            return method.ContainingType?.ToDisplayString() == "System.Linq.Enumerable"
                && method.Name is "Count" or "Any";

        return member.Name.Identifier.Text is "Count" or "Any";
    }
}
