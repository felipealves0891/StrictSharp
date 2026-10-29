using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf004Analyzer : RuleAnalyzerBase
{
    public Perf004Analyzer() : base("PERF004") { }
    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(static context =>
        {
            var model = context.SemanticModel;
            if (context.Node is not LockStatementSyntax lockStatement)
                return;

            foreach (var invocation in lockStatement.Statement.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
                    && RuleAnalysis.IsBlockingOperation(method))
                {
                    RuleAnalysis.Report(RuleCatalog.Get("PERF004"), context, invocation.GetLocation());
                    return;
                }
            }
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.LockStatement);
    }
}
