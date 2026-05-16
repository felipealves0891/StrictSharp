using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using StrictSharp.Analyzers.Extensions;

namespace StrictSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HiddenBoxingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PERF003";
        private const string Category = "Performance";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            title: "Allocation by Boxing detected",
            messageFormat: "The value type '{0}' was converted to 'object' (Boxing). This causes an allocation on the Heap.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Boxing allocates memory on the Heap. Try using Generics <T> or Spans to avoid converting to object.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Usamos OperationAction porque o Roslyn já identifica explicitamente o que é Boxing na árvore semântica
            context.RegisterOperationActionIfAssembly(AnalyzeConversion, OperationKind.Conversion);
        }

        private static void AnalyzeConversion(OperationAnalysisContext context)
        {
            var conversion = (IConversionOperation)context.Operation;

            // A forma correta de verificar se é boxing através da IConversionOperation
            if (IsConversionBoxing(conversion))
            {
                var sourceType = conversion.Operand.Type;

                // Mantendo o padrão de mensagem: frase única, sem ponto final
                var diagnostic = Diagnostic.Create(
                    Rule,
                    conversion.Syntax.GetLocation(),
                    sourceType?.ToDisplayString() ?? "desconhecido");

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsConversionBoxing(IConversionOperation conversion)
        {
            var isReferenceType = conversion.Type?.IsReferenceType;
            var isInterface = conversion.Type?.TypeKind == TypeKind.Interface;
            var boxing = (isReferenceType.HasValue && isReferenceType.Value) || isInterface; 
            return conversion.Operand.Type?.IsValueType == true && boxing;
        }
    }

}
