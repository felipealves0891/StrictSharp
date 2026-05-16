using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictSharp.Analyzers.Extensions;

namespace StrictSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class BigOAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PERF009";
        private static readonly LocalizableString Title = "Big O complexity";
        private static readonly LocalizableString MessageFormat = "This code has inferred complexity '{0}'";
        private static readonly LocalizableString Description = "Algorithms with high asymptotic cost may not scale as input grows.";
        private const string Category = "Performance";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeActionIfAssembly(AnalyzeMethod, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeActionIfAssembly(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            if (method.Body == null) return;

            ReportIfNeeded(context, method.Body, AnalyzeBlock(method.Body));
        }

        private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            var constructor = (ConstructorDeclarationSyntax)context.Node;
            if (constructor.Body == null) return;

            ReportIfNeeded(context, constructor.Body, AnalyzeBlock(constructor.Body));
        }

        private static BigOComplexity AnalyzeBlock(BlockSyntax block)
        {
            var result = BigOComplexity.Constant;
            foreach (var statement in block.Statements)
                result = BigOComplexity.AddSequential(result, AnalyzeStatement(statement));

            return result;
        }

        private static BigOComplexity AnalyzeStatement(StatementSyntax statement)
        {
            if (statement is BlockSyntax block) return AnalyzeBlock(block);
            if (statement is IfStatementSyntax ifStatement) return AnalyzeIf(ifStatement);

            return BigOComplexity.Constant;
        }

        private static BigOComplexity AnalyzeIf(IfStatementSyntax ifStatement)
        {
            var whenTrue = AnalyzeStatement(ifStatement.Statement);
            var whenFalse = BigOComplexity.Constant;

            if (ifStatement.Else != null)
                whenFalse = AnalyzeStatement(ifStatement.Else.Statement);

            return BigOComplexity.AddSequential(whenTrue, whenFalse);
        }

        private static void ReportIfNeeded(
            SyntaxNodeAnalysisContext context,
            SyntaxNode node,
            BigOComplexity complexity)
        {
            if (complexity.IsUnknown) return;
            if (complexity.CompareTo(BigOComplexity.Quadratic) < 0) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), complexity.ToString()));
        }
    }
}
