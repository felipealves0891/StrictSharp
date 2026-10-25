using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Rules;
using System.Collections.Immutable;

namespace StrictSharp.Analyzers.Infrastructure;

public abstract class RuleAnalyzerBase : DiagnosticAnalyzer
{
    protected RuleAnalyzerBase(string ruleId)
    {
        Rule = RuleCatalog.Get(ruleId);
    }

    protected RuleDefinition Rule { get; }

    public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Rule.SupportedDiagnostics;

    public sealed override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        RegisterAnalysis(context);
    }

    protected abstract void RegisterAnalysis(AnalysisContext context);
}
