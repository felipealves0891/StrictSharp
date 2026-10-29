using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf005Analyzer : RuleAnalyzerBase
{
    public Perf005Analyzer() : base("PERF005") { }
    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(static context =>
        {
            var model = context.SemanticModel;
            var loop = context.Node;
            var hasQuery = loop switch
            {
                WhileStatementSyntax whileStatement => IsQuery(whileStatement.Condition, model),
                ForStatementSyntax forStatement => IsQuery(forStatement.Condition, model)
                    || IsQuery(forStatement.Incrementors.FirstOrDefault(), model),
                DoStatementSyntax doStatement => IsQuery(doStatement.Condition, model),
                _ => false
            };
            if (!hasQuery)
                return;

            var statement = loop switch
            {
                WhileStatementSyntax whileStatement => whileStatement.Statement,
                ForStatementSyntax forStatement => forStatement.Statement,
                DoStatementSyntax doStatement => doStatement.Statement,
                _ => null
            };
            if (statement?.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(invocation =>
                    model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
                    && method.Name is "Sleep" or "Delay" or "Wait" or "SpinOnce" or "SpinUntil" or "SpinWait") == true)
                return;

            RuleAnalysis.Report(RuleCatalog.Get("PERF005"), context, loop.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.WhileStatement,
           Microsoft.CodeAnalysis.CSharp.SyntaxKind.ForStatement,
           Microsoft.CodeAnalysis.CSharp.SyntaxKind.DoStatement);
    }

    private static bool IsQuery(SyntaxNode? node, SemanticModel model)
    {
        return node is not null
            && node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(invocation =>
                model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
                && method.Name is not ("Sleep" or "Delay" or "Wait"));
    }
}
