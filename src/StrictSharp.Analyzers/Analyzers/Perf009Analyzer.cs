using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf009Analyzer : RuleAnalyzerBase
{
    public Perf009Analyzer() : base("PERF009") { }
    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(static context =>
        {
            var model = context.SemanticModel;
            if (context.Node is not VariableDeclaratorSyntax variable
                || variable.Initializer?.Value is not ExpressionSyntax expression)
                return;

            var sourceType = model.GetTypeInfo(expression).Type;
            var targetType = model.GetTypeInfo(expression).ConvertedType;
            if (sourceType is not { IsValueType: true }
                || targetType is not { SpecialType: SpecialType.System_Object } and not INamedTypeSymbol { TypeKind: TypeKind.Interface })
                return;

            RuleAnalysis.Report(RuleCatalog.Get("PERF009"), context, expression.GetLocation());
        }, Microsoft.CodeAnalysis.CSharp.SyntaxKind.VariableDeclarator);
    }
}
