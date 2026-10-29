using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf017Analyzer : RuleAnalyzerBase
{
    public Perf017Analyzer() : base("PERF017") { }
    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(static context =>
        {
            if (context.Node is not InvocationExpressionSyntax invocation
                || !RuleAnalysis.IsInsideLoop(invocation)
                || invocation.Expression is not MemberAccessExpressionSyntax member
                || member.Name.Identifier.Text is not ("Where" or "Select" or "SelectMany" or "Sum" or "Average" or "Count" or "ToList" or "ToArray"))
                return;

            if (invocation.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax })
                return;

            var iterationNames = invocation.Ancestors()
                .OfType<ForEachStatementSyntax>()
                .Select(loop => loop.Identifier.ValueText)
                .ToHashSet(StringComparer.Ordinal);
            var dependsOnIteration = invocation.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Any(identifier => iterationNames.Contains(identifier.Identifier.ValueText));
            if (!dependsOnIteration)
                RuleAnalysis.Report(RuleCatalog.Get("PERF017"), context, invocation.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
    }
}
