using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf016Analyzer : RuleAnalyzerBase
{
    public Perf016Analyzer() : base("PERF016") { }
    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(static context =>
        {
            if (context.Node is not InvocationExpressionSyntax invocation
                || !RuleAnalysis.IsInsideLoop(invocation)
                || invocation.Expression is not MemberAccessExpressionSyntax member
                || member.Name.Identifier.Text is not ("OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending"))
                return;
            if (RuleAnalysis.IsSourceMutatedInContainingLoop(member.Expression, invocation, context.SemanticModel))
                return;

            RuleAnalysis.Report(RuleCatalog.Get("PERF016"), context, invocation.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
    }
}
