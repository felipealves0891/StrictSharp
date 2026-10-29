using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf024Analyzer : RuleAnalyzerBase
{
    public Perf024Analyzer() : base("PERF024") { }
    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(static context =>
        {
            if (context.Node is not ObjectCreationExpressionSyntax creation
                || creation.Type is not GenericNameSyntax { Identifier.ValueText: "List" }
                || creation.ArgumentList?.Arguments.Count > 0
                || creation.Parent?.Parent is not VariableDeclaratorSyntax variable)
                return;

            var model = context.SemanticModel;
            if (variable.Parent?.Parent is not LocalDeclarationStatementSyntax declaration)
                return;

            if (declaration.Parent?.AncestorsAndSelf().OfType<ForEachStatementSyntax>().Any() == true
                || declaration.Parent?.AncestorsAndSelf().OfType<ForStatementSyntax>().Any() == true
                || declaration.Parent?.AncestorsAndSelf().OfType<WhileStatementSyntax>().Any() == true)
                return;

            var symbol = model.GetDeclaredSymbol(variable);
            if (symbol is null)
                return;

            var method = declaration.Parent?.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (method is not null
                && method.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(invocation =>
                    invocation.Expression is MemberAccessExpressionSyntax member
                    && member.Expression is IdentifierNameSyntax identifier
                    && identifier.Identifier.ValueText == variable.Identifier.ValueText
                    && member.Name.Identifier.ValueText == "Add"
                    && RuleAnalysis.IsInsideLoop(invocation)))
                RuleAnalysis.Report(RuleCatalog.Get("PERF024"), context, creation.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ObjectCreationExpression);
    }
}
