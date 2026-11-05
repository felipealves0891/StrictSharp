using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Retention;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf011Analyzer : RuleAnalyzerBase
{
    public Perf011Analyzer() : base("PERF011") { }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(static context =>
        {
            if (context.Node is not InvocationExpressionSyntax invocation
                || !Perf011RetentionAnalysis.ShouldReport(invocation, context.SemanticModel))
                return;

            var receiver = (MemberAccessExpressionSyntax)invocation.Expression;
            RuleAnalysis.Report(RuleCatalog.Get("PERF011"), context, receiver.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
    }
}
