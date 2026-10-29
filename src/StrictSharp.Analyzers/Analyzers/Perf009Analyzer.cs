using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf009Analyzer : RuleAnalyzerBase
{
    public Perf009Analyzer() : base("PERF009") { }
    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterOperationAction(static context =>
        {
            if (context.Operation is not IConversionOperation conversion
                || conversion.Conversion.IsIdentity
                || conversion.Conversion.IsNumeric
                || conversion.Conversion.IsReference
                || conversion.Conversion.IsUserDefined
                || conversion.Operand.Type is not { IsValueType: true }
                || conversion.Type is not { SpecialType: SpecialType.System_Object } and not INamedTypeSymbol { TypeKind: TypeKind.Interface }
                || conversion.Parent is IArgumentOperation)
                return;
            RuleAnalysis.Report(RuleCatalog.Get("PERF009"), context, conversion.Syntax.GetLocation());
        }, OperationKind.Conversion);
    }
}
