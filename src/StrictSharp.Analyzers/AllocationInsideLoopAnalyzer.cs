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
    public class AllocationInsideLoopAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PERF001";
        private static readonly LocalizableString Title = "Allocation inside loop";
        private static readonly LocalizableString MessageFormat = "The object '{0}' should not be created with 'new' inside loops. Use pooling.";
        private static readonly LocalizableString Description = "To ensure zero allocation, objects in loops should come from a pool.";
        private const string Category = "Performance";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category, 
            DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            // Configurações de segurança para o analisador
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Registra a ação para quando encontrar um "new" (ObjectCreation)
            context.RegisterSyntaxNodeActionIfAssembly(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

            // 1. Verificar se o 'new' está dentro de um loop (For, ForEach, While, Do)
            var isInLoop = objectCreation.Ancestors().Any(a => 
                a is ForStatementSyntax || 
                a is ForEachStatementSyntax || 
                a is WhileStatementSyntax || 
                a is DoStatementSyntax);

            if (!isInLoop) return;

            // 2. Obter informações sobre o tipo que está sendo criado
            var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation).Type;
            if (typeInfo == null) return;

            // Opcional: Você pode filtrar para não dar erro em tipos primitivos ou específicos
            // Se for uma classe (ReferenceType), reportamos o erro
            if (typeInfo.IsReferenceType)
            {
                var diagnostic = Diagnostic.Create(Rule, objectCreation.GetLocation(), typeInfo.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
