using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf023Analyzer : RuleAnalyzerBase
{
    public Perf023Analyzer() : base("PERF023") { }
    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(static context =>
        {
            if (context.Node is not InvocationExpressionSyntax invocation
                || !RuleAnalysis.IsInsideLoop(invocation)
                || invocation.Expression is not MemberAccessExpressionSyntax member
                || member.Name.Identifier.Text is not ("RemoveAt" or "Contains"))
                return;

            var model = context.SemanticModel;
            if (member.Expression is not ExpressionSyntax receiver
                || model.GetTypeInfo(receiver).Type is not INamedTypeSymbol type
                || type.Name is not ("List" or "IList"))
                return;

            if (member.Name.Identifier.Text == "RemoveAt"
                && invocation.ArgumentList.Arguments.Count == 1
                && invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax { Token.Value: 0 })
            {
                RuleAnalysis.Report(RuleCatalog.Get("PERF023"), context, invocation.GetLocation());
                return;
            }

            if (member.Name.Identifier.Text == "Contains")
                RuleAnalysis.Report(RuleCatalog.Get("PERF023"), context, invocation.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
    }
}
