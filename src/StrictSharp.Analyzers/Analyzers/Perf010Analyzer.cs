using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf010Analyzer : RuleAnalyzerBase
{
    public Perf010Analyzer() : base("PERF010") { }
    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(static context =>
        {
            if (context.Node is not LambdaExpressionSyntax lambda
                || lambda.Modifiers.Any(SyntaxKind.StaticKeyword)
                || !RuleAnalysis.IsInsideLoop(lambda))
                return;

            var captures = lambda.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Any(identifier => context.SemanticModel.GetSymbolInfo(identifier).Symbol is ILocalSymbol or IParameterSymbol);
            if (captures)
                RuleAnalysis.Report(RuleCatalog.Get("PERF010"), context, lambda.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleLambdaExpression,
           Microsoft.CodeAnalysis.CSharp.SyntaxKind.ParenthesizedLambdaExpression);
    }
}
