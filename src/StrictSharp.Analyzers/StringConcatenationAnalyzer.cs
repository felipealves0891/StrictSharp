using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Extensions;

namespace StrictSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StringConcatenationAnalyzer  : DiagnosticAnalyzer
    {
        public const string ManipulationDiagnosticId = "PERF007";
        private static readonly string[] BannedMethods = { "Substring", "Replace", "ToLower", "ToUpper", "Trim", "Split" };
        private static readonly DiagnosticDescriptor ManipulationRule = new DiagnosticDescriptor(
            ManipulationDiagnosticId,
            "String manipulation prohibited",
            "The operation '{0}' allocates memory on the Heap. Use Spans or StringBuilder.",
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            "Traditional string handling in .NET is one of the \"silent villains\" of performance, especially in high-throughput systems (HFT, low-latency APIs, or Engines).");
        
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ManipulationRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Captura: "a" + "b"
            context.RegisterSyntaxNodeActionIfAssembly(AnalyzeBinaryExpression, SyntaxKind.AddExpression);
            // Captura: $"{var}"
            context.RegisterSyntaxNodeActionIfAssembly(AnalyzeInterpolation, SyntaxKind.InterpolatedStringExpression);
            // Captura: str.Substring() ou string.Format()
            context.RegisterSyntaxNodeActionIfAssembly(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context)
        {
            var binary = (BinaryExpressionSyntax)context.Node;
            var typeInfo = context.SemanticModel.GetTypeInfo(binary);
            
            // Se o resultado da soma for string, houve concatenação (alocação)
            if (typeInfo.Type?.SpecialType == SpecialType.System_String)
            {
                context.ReportDiagnostic(Diagnostic.Create(ManipulationRule, binary.OperatorToken.GetLocation(), "Concatenação (+)"));
            }
        }

        private void AnalyzeInterpolation(SyntaxNodeAnalysisContext context)
        {
            var interpolation = (InterpolatedStringExpressionSyntax)context.Node;
            context.ReportDiagnostic(Diagnostic.Create(ManipulationRule, interpolation.GetLocation(), "Interpolação ($)"));
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

            if (symbol?.ContainingType?.SpecialType == SpecialType.System_String)
            {
                if (BannedMethods.Contains(symbol.Name) || symbol.IsStatic)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ManipulationRule, invocation.GetLocation(), $"string.{symbol.Name}"));
                }
            }
        }
    }    
}


