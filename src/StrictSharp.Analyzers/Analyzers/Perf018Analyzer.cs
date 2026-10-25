using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf018Analyzer : RuleAnalyzerBase
{
    public Perf018Analyzer()
        : base("PERF018")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.AddAssignmentExpression);
    }

    private void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not AssignmentExpressionSyntax assignment
            || !RuleAnalysis.IsInsideLoop(assignment)
            || !RuleAnalysis.IsString(context.SemanticModel.GetTypeInfo(assignment.Left).Type))
            return;

        RuleAnalysis.Report(Rule, context, assignment.OperatorToken.GetLocation());
    }
}
