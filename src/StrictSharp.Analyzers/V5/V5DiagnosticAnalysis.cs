using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Rules;

namespace StrictSharp.Analyzers.V5;

public static class V5DiagnosticAnalysis
{
    public static void Report(
        RuleDefinition rule,
        SyntaxNodeAnalysisContext context,
        CostEvidenceSet evidence,
        Func<CostEvidence, bool> shouldReport)
    {
        var reported = false;
        foreach (var item in evidence.Items)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (reported || !shouldReport(item))
                continue;

            context.ReportDiagnostic(Diagnostic.Create(
                rule.Descriptor,
                context.Node.GetLocation(),
                item.Explanation,
                item.Dimension,
                item.Limitations));
            reported = true;
        }
    }
}
