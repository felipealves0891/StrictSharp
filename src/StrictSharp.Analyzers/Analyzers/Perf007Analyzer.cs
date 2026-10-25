using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf007Analyzer : RuleAnalyzerBase
{
    public Perf007Analyzer()
        : base("PERF007")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeObject, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeArray, SyntaxKind.ArrayCreationExpression, SyntaxKind.ImplicitArrayCreationExpression);
    }

    private void AnalyzeObject(SyntaxNodeAnalysisContext context)
    {
        if (!RuleAnalysis.IsInsideLoop(context.Node)
            || context.Node is not ObjectCreationExpressionSyntax creation
            || context.SemanticModel.GetTypeInfo(creation).Type is not INamedTypeSymbol type
            || !IsCandidate(type))
            return;

        RuleAnalysis.Report(Rule, context, creation.Type.GetLocation());
    }

    private void AnalyzeArray(SyntaxNodeAnalysisContext context)
    {
        if (RuleAnalysis.IsInsideLoop(context.Node))
            RuleAnalysis.Report(Rule, context);
    }

    private static bool IsCandidate(INamedTypeSymbol type)
    {
        return type.Name is "List" or "Dictionary" or "HashSet" or "StringBuilder" or "MemoryStream"
            && type.ContainingNamespace.ToDisplayString() is "System.Collections.Generic" or "System.Text" or "System.IO";
    }
}
