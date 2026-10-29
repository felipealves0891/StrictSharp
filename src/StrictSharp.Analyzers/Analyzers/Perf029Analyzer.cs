using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf029Analyzer : RuleAnalyzerBase
{
    public Perf029Analyzer() : base("PERF029") { }
    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(static context =>
        {
            if (context.Node is not CatchClauseSyntax catchClause
                || catchClause.Declaration?.Type is not IdentifierNameSyntax { Identifier.ValueText: "FormatException" }
                || !RuleAnalysis.IsInsideLoop(catchClause)
                || catchClause.Ancestors().OfType<TryStatementSyntax>().FirstOrDefault() is not { } tryStatement)
                return;

            if (!tryStatement.Block.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(invocation =>
                    invocation.Expression is MemberAccessExpressionSyntax member
                    && member.Name.Identifier.ValueText is "Parse" or "ParseExact"))
                return;

            RuleAnalysis.Report(RuleCatalog.Get("PERF029"), context, catchClause.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.CatchClause);
    }
}
