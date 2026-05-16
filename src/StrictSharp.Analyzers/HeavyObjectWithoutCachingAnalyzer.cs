using System;
using System.Collections.Generic;
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
    public class HeavyObjectWithoutCachingAnalyzer : DiagnosticAnalyzer
    {
        public const string ManipulationDiagnosticId = "PERF005";
        private static readonly Dictionary<string, Action<SyntaxNodeAnalysisContext, ObjectCreationExpressionSyntax>> HeavyObjects = new Dictionary<string, Action<SyntaxNodeAnalysisContext, ObjectCreationExpressionSyntax>>
        {
            { "Regex", AnalyzeRegex },
            { "Random", AnalyzeRandom }
        };

        private static readonly DiagnosticDescriptor ManipulationRule = new DiagnosticDescriptor(
            ManipulationDiagnosticId,
            "Large objects have slow initialization",
            "Large objects have slow initialization, instead use {0}",
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            "Large objects have slow initialization.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ManipulationRule);

        public override void Initialize(AnalysisContext context)
        {
            // Configurações de segurança para o analisador
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Registra a ação para quando encontrar um "new" (ObjectCreation)
            context.RegisterSyntaxNodeActionIfAssembly(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

            var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation).Type;
            if (typeInfo == null) return;

            if(!HeavyObjects.ContainsKey(typeInfo.Name))
                return;

            HeavyObjects[typeInfo.Name](context, objectCreation);
        }

        private static void AnalyzeRegex(SyntaxNodeAnalysisContext context, ObjectCreationExpressionSyntax objectCreation)
        {
            var field = objectCreation.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault();
            var property = objectCreation.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            
            if(property != null || field != null)
            {
                var modifiers = property != null ? property.Modifiers : field.Modifiers;
                var isStatic = modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
                var isReadonly = modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword));
                if(isReadonly && isReadonly)
                    return;    
            }
            
            var diagnostic = Diagnostic.Create(ManipulationRule, objectCreation.GetLocation(), "static and read-only properties");
            context.ReportDiagnostic(diagnostic);
        }

        private static void AnalyzeRandom(SyntaxNodeAnalysisContext context, ObjectCreationExpressionSyntax objectCreation)
        {
            var diagnostic = Diagnostic.Create(ManipulationRule, objectCreation.GetLocation(), "Random.Shared");
            context.ReportDiagnostic(diagnostic);
        }
    }
}
