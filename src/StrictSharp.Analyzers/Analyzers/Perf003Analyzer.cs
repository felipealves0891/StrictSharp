using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf003Analyzer : RuleAnalyzerBase
{
    public Perf003Analyzer()
        : base("PERF003")
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
            || !RuleAnalysis.IsTaskRun(method)
            || invocation.ArgumentList.Arguments.Count == 0
            || invocation.ArgumentList.Arguments[0].Expression is not LambdaExpressionSyntax lambda
            || !ReturnsTaskLike(lambda, context.SemanticModel)
            || !IsTaskOnlyWrapper(invocation))
            return;

        RuleAnalysis.Report(Rule, context, invocation.GetLocation());
    }

    private static bool ReturnsTaskLike(LambdaExpressionSyntax lambda, SemanticModel model)
    {
        if (lambda.AsyncKeyword != default)
            return true;

        if (lambda.Body is ExpressionSyntax expression)
            return RuleAnalysis.IsTaskLike(model.GetTypeInfo(expression).Type);

        return lambda.Body.DescendantNodes()
            .OfType<ReturnStatementSyntax>()
            .Any(returnStatement => returnStatement.Expression is not null
                && RuleAnalysis.IsTaskLike(model.GetTypeInfo(returnStatement.Expression).Type));
    }

    private static bool IsTaskOnlyWrapper(InvocationExpressionSyntax invocation)
    {
        var method = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method is null)
            return false;

        if (method.ExpressionBody?.Expression == invocation)
            return true;

        return method.Body?.Statements.Count == 1
            && method.Body.Statements[0] is ReturnStatementSyntax returnStatement
            && returnStatement.Expression == invocation;
    }
}
