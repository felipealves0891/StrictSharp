using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf027Analyzer : RuleAnalyzerBase
{
    public Perf027Analyzer()
        : base("PERF027")
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
            || context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method
            || !method.Name.StartsWith("Log", StringComparison.Ordinal)
            || !RuleAnalysis.IsLogger(context.SemanticModel.GetTypeInfo(member.Expression).Type)
            || !invocation.ArgumentList.Arguments.Any(argument => IsExpensive(argument.Expression)))
            return;

        RuleAnalysis.Report(Rule, context);
    }

    private static bool IsExpensive(ExpressionSyntax expression)
    {
        return expression is InterpolatedStringExpressionSyntax
            || expression.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>().Any(binary => binary.IsKind(SyntaxKind.AddExpression))
            || expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(invocation =>
                invocation.Expression is MemberAccessExpressionSyntax member
                    && member.Name.Identifier.Text == "Serialize");
    }
}
