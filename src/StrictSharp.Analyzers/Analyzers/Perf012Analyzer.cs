using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf012Analyzer : RuleAnalyzerBase
{
    public Perf012Analyzer()
        : base("PERF012")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation
            || !IsCopyOperation(invocation, context.SemanticModel))
            return;

        RuleAnalysis.Report(Rule, context);
    }

    private static bool IsCopyOperation(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method)
            return method.Name == "Clone"
                || (method.Name is "ToArray" or "ToList"
                    && invocation.Expression is MemberAccessExpressionSyntax member
                    && RuleAnalysis.IsEnumerable(model.GetTypeInfo(member.Expression).Type));

        return invocation.Expression is MemberAccessExpressionSyntax unresolvedMember
            && unresolvedMember.Name.Identifier.Text is "ToArray" or "ToList"
            && RuleAnalysis.IsEnumerable(model.GetTypeInfo(unresolvedMember.Expression).Type);
    }
}
