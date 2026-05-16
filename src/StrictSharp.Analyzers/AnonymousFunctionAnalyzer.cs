using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using StrictSharp.Analyzers.Extensions;

namespace StrictSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AnonymousFunctionAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString Description = "This lambda expression captures variables from an outer scope, which results in a closure allocation on the heap. Captured variables extend their lifetime and may introduce unintended memory pressure, especially in high-frequency or performance-critical code paths. Consider avoiding captures by using static lambdas, passing variables as parameters, or restructuring the code to eliminate the closure.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            "PERF020",
            "Closure captures outer variables causing allocation",
            "Lambda captures {0} outer variable(s) ({1})",
            "Performance",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description);


        private static readonly DiagnosticDescriptor RuleInLoop = new DiagnosticDescriptor(
            "PERF021",
            "Closure captures outer variables causing allocation",
            "Lambda captures {0} outer variable(s) ({1}) inside a loop",
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        private static readonly DiagnosticDescriptor RuleWithThis = new DiagnosticDescriptor(
            "PERF022",
            "Closure captures outer variables causing allocation",
            "Capturing 'this' instance",
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        private static readonly DiagnosticDescriptor RuleLangeStruct = new DiagnosticDescriptor(
            "PERF023",
            "Closure captures outer variables causing allocation",
            "Lambda captures large struct(s): {0}",
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, RuleWithThis, RuleLangeStruct, RuleInLoop);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterOperationActionIfAssembly(AnalyzeLambda, OperationKind.AnonymousFunction);
        }

        private static void AnalyzeLambda(OperationAnalysisContext context)
        {
            var lambda = (IAnonymousFunctionOperation)context.Operation;
            var captured = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            VisitOperation(lambda.Body, lambda, captured, out var capturedThis);
            if (captured.Count == 0 && !capturedThis)
                return;

            if(capturedThis)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        RuleWithThis,
                        lambda.Syntax.GetLocation()));
            }

            var largeStructsFormatted = LargeStructsFormatted(captured);
            if (!string.IsNullOrEmpty(largeStructsFormatted))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        RuleLangeStruct,
                        lambda.Syntax.GetLocation(),
                        largeStructsFormatted));
            }

            var namesFormatted = NamesFormatted(captured);
            if(namesFormatted.variablesCount <= 0)
                return;
                
            if (IsInsideLoop(lambda))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        RuleInLoop,
                        lambda.Syntax.GetLocation(),
                        namesFormatted.variablesCount,
                        namesFormatted.Message));
            }
            else
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        lambda.Syntax.GetLocation(),
                        namesFormatted.variablesCount,
                        namesFormatted.Message));
            }
            
        }

        private static bool IsInsideLoop(IOperation operation)
        {
            var current = operation.Parent;

            while (current != null)
            {
                if (current is ILoopOperation)
                    return true;

                current = current.Parent;
            }

            return false;
        }

        private static (string Message, int variablesCount) NamesFormatted(HashSet<ISymbol> captured)
        {
            var variableNames = captured
                .Where(s => !(s is ILocalSymbol local && IsLargeStruct(local.Type)))
                .Select(s => s.Name)
                .Distinct()
                .ToArray();

            return (string.Join(", ", variableNames), variableNames.Count());
        }

        private static string LargeStructsFormatted(HashSet<ISymbol> captured)
        {
            var largeStructs = captured
                .Where(s => s is ILocalSymbol local && IsLargeStruct(local.Type))
                .Select(s => s.Name)
                .ToList();

            if (largeStructs.Count > 0)
                return $", including large struct(s): {string.Join(", ", largeStructs)}";

            return string.Empty;
        }

        private static void VisitOperation(
            IOperation op,
            IAnonymousFunctionOperation lambda,
            HashSet<ISymbol> captured,
            out bool capturedThis)
        {
            var localCapturedThis = false;

            if (op is IInstanceReferenceOperation instance &&
                instance.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance)
            {
                localCapturedThis = true;
            }

            if (op is ILocalReferenceOperation localRef)
            {
                var symbol = localRef.Local;

                if (!SymbolEqualityComparer.Default.Equals(
                        symbol.ContainingSymbol, lambda.Symbol))
                {
                    captured.Add(symbol);
                }
            }

            foreach (var child in op.ChildOperations)
            {
                VisitOperation(child, lambda, captured, out var childCapturedThis);
                localCapturedThis = localCapturedThis || childCapturedThis;
            }

            capturedThis = localCapturedThis;
        }

        private static bool IsLargeStruct(ITypeSymbol type)
        {
            if (!(type is INamedTypeSymbol named) || !named.IsValueType)
                return false;

            var fields = named.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => !f.IsStatic)
                .ToList();

            if (fields.Count >= 3) // heurística simples
                return true;

            // heurística extra: contém struct dentro
            if (fields.Any(f => f.Type.IsValueType && f.Type.SpecialType == SpecialType.None))
                return true;

            return false;
        }

    }
}

