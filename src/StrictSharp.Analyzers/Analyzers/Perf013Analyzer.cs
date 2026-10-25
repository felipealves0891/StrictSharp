using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf013Analyzer : RuleAnalyzerBase
{
    public Perf013Analyzer()
        : base("PERF013")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var invocation in context.Node.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (!IsEnumeration(invocation, context.SemanticModel, out var receiver))
                continue;

            if (!seen.Add(receiver.ToString()))
                RuleAnalysis.Report(Rule, context, invocation.GetLocation());
        }
    }

    private static bool IsEnumeration(InvocationExpressionSyntax invocation, SemanticModel model, out ExpressionSyntax receiver)
    {
        receiver = null!;
        if (invocation.Expression is not MemberAccessExpressionSyntax member
            || !RuleAnalysis.IsEnumerable(model.GetTypeInfo(member.Expression).Type))
            return false;

        var method = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        var name = method?.Name ?? member.Name.Identifier.Text;
        if (name is not ("Count" or "LongCount" or "Any" or "First" or "FirstOrDefault" or "Single"
            or "SingleOrDefault" or "Last" or "LastOrDefault" or "ToList" or "ToArray" or "ToDictionary"
            or "ToLookup" or "Sum" or "Average" or "Min" or "Max" or "Contains" or "ElementAt" or "ElementAtOrDefault"))
            return false;

        receiver = member.Expression;
        return true;
    }
}
