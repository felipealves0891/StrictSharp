using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf002Analyzer : RuleAnalyzerBase
{
    public Perf002Analyzer() : base("PERF002") { }
    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(static context =>
        {
            if (context.Node is not InvocationExpressionSyntax invocation
                || invocation.Expression is not MemberAccessExpressionSyntax member
                || member.Expression is not IdentifierNameSyntax { Identifier.Text: "Task" }
                || member.Name.Identifier.Text != "WhenAll"
                || invocation.ArgumentList.Arguments.Count != 1)
                return;

            var expression = invocation.ArgumentList.Arguments[0].Expression;
            if (expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax selectMember }
                && selectMember.Name.Identifier.Text == "Select")
                RuleAnalysis.Report(RuleCatalog.Get("PERF002"), context, expression.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
    }
}
