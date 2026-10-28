using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf022Analyzer : RuleAnalyzerBase
{
    public Perf022Analyzer()
        : base("PERF022")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.IfStatement);
    }

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not IfStatementSyntax statement
            || statement.Condition.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault() is not { } contains
            || contains.Expression is not MemberAccessExpressionSyntax containsMember
            || context.SemanticModel.GetSymbolInfo(contains).Symbol is not IMethodSymbol containsMethod
            || containsMethod.Name != "ContainsKey"
            || !RuleAnalysis.IsDictionaryLike(context.SemanticModel.GetTypeInfo(containsMember.Expression).Type))
            return;

        var dictionaryText = containsMember.Expression.ToString();
        var keyText = contains.ArgumentList.Arguments.FirstOrDefault()?.Expression.ToString();
        var duplicate = statement.Statement.DescendantNodes().Any(node =>
            node is ElementAccessExpressionSyntax access
                && access.Expression.ToString() == dictionaryText
                && access.ArgumentList.Arguments.FirstOrDefault()?.Expression.ToString() == keyText
            || node is InvocationExpressionSyntax invocation
                && invocation.Expression is MemberAccessExpressionSyntax member
                && member.Name.Identifier.Text == "Remove"
                && member.Expression.ToString() == dictionaryText
                && invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression.ToString() == keyText);

        if (duplicate)
            RuleAnalysis.Report(Rule, context, contains.GetLocation());
    }
}
