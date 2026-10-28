using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf001Analyzer : RuleAnalyzerBase
{
    public Perf001Analyzer()
        : base("PERF001")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MemberAccessExpressionSyntax member
            || member.Name.Identifier.Text != "Result"
            || context.SemanticModel.GetSymbolInfo(member).Symbol is not IPropertySymbol property
            || !RuleAnalysis.IsTaskLike(property.ContainingType)
            || RuleAnalysis.IsKnownCompletedTask(member.Expression, context.SemanticModel))
            return;

        RuleAnalysis.Report(Rule, context, member.Name.GetLocation());
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation
            || context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
            return;

        if (method.Name == "Wait"
            && RuleAnalysis.IsTaskLike(method.ContainingType)
            && invocation.Expression is MemberAccessExpressionSyntax wait
            && !RuleAnalysis.IsKnownCompletedTask(wait.Expression, context.SemanticModel))
        {
            RuleAnalysis.Report(Rule, context, invocation.GetLocation());
            return;
        }

        if (method.Name != "GetResult"
            || !RuleAnalysis.IsAwaiter(method.ContainingType)
            || invocation.Expression is not MemberAccessExpressionSyntax getResult
            || getResult.Expression is not InvocationExpressionSyntax getAwaiter
            || getAwaiter.Expression is not MemberAccessExpressionSyntax getAwaiterMember
            || getAwaiterMember.Name.Identifier.Text != "GetAwaiter"
            || RuleAnalysis.IsKnownCompletedTask(getAwaiterMember.Expression, context.SemanticModel))
            return;

        RuleAnalysis.Report(Rule, context, invocation.GetLocation());
    }
}
