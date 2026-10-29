using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
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
                || invocation.Expression is not MemberAccessExpressionSyntax member
                || member.Name.Identifier.ValueText is not ("Add" or "AddRange" or "Subscribe"))
                return;

            if (member.Expression is not IdentifierNameSyntax receiver
                || context.SemanticModel.GetSymbolInfo(receiver).Symbol is not IFieldSymbol field
                || !RuleAnalysis.IsInsideLoop(invocation)
                || HasReleaseOperation(invocation, field, context.SemanticModel))
                return;

            RuleAnalysis.Report(RuleCatalog.Get("PERF011"), context, receiver.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
    }

    private static bool HasReleaseOperation(
        InvocationExpressionSyntax invocation,
        IFieldSymbol field,
        SemanticModel model)
    {
        var containingMethod = invocation.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod is null)
            return false;

        foreach (var release in containingMethod.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (release.Expression is not MemberAccessExpressionSyntax member
                || member.Expression is not IdentifierNameSyntax receiver
                || !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, field))
                continue;

            if (member.Name.Identifier.ValueText is "Clear" or "Remove" or "RemoveAll" or "Dequeue" or "Pop")
                return true;
        }

        return false;
    }
}
