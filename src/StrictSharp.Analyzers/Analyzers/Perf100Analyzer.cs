using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Interprocedural;

namespace StrictSharp.Analyzers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Perf100Analyzer : RuleAnalyzerBase
{
    public Perf100Analyzer()
        : base("PERF100")
    {
    }

    protected override void RegisterAnalysis(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not IMethodSymbol method)
            return;

        var graph = InterproceduralGraphBuilder.Build(context.SemanticModel.Compilation, method, context.CancellationToken);
        var evidence = InterproceduralEvidenceAnalyzer.Analyze(
            context.SemanticModel.Compilation,
            graph,
            context.CancellationToken);
        var grouped = new Dictionary<string, List<InterproceduralEvidence>>(StringComparer.Ordinal);

        foreach (var item in evidence)
        {
            if (!RuleAnalysis.IsSourceLocation(item.Location))
                continue;

            var key = $"{item.Location.SourceSpan.Start}:{item.Target?.ToDisplayString()}";
            if (!grouped.TryGetValue(key, out var items))
            {
                items = [];
                grouped.Add(key, items);
            }

            items.Add(item);
        }

        foreach (var items in grouped.Values)
        {
            var first = items[0];
            var kinds = string.Join(", ", items.Select(item => item.Kind).Distinct());
            var dimensions = string.Join(", ", items.Select(item => item.Dimension).Distinct(StringComparer.Ordinal));

            var targetName = first.Target?.Name ?? "desconhecido";
            RuleAnalysis.Report(
                Rule,
                context,
                first.Location,
                kinds,
                dimensions,
                first.Caller.Name,
                targetName);
        }
    }
}
