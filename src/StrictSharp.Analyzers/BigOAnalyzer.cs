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
            if (statement is ForStatementSyntax forStatement) return AnalyzeFor(forStatement);
            if (statement is WhileStatementSyntax whileStatement) return AnalyzeWhile(whileStatement);
            if (statement is DoStatementSyntax doStatement) return AnalyzeDo(doStatement);
            if (statement is ForEachStatementSyntax forEachStatement) return AnalyzeForEach(forEachStatement);

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

        private static BigOComplexity AnalyzeFor(ForStatementSyntax forStatement)
        {
            var iteration = AnalyzeLoopCondition(forStatement.Condition);
            return BigOComplexity.Multiply(iteration, AnalyzeStatement(forStatement.Statement));
        }

        private static BigOComplexity AnalyzeWhile(WhileStatementSyntax whileStatement)
        {
            var iteration = AnalyzeLoopCondition(whileStatement.Condition);
            return BigOComplexity.Multiply(iteration, AnalyzeStatement(whileStatement.Statement));
        }

        private static BigOComplexity AnalyzeDo(DoStatementSyntax doStatement)
        {
            var iteration = AnalyzeLoopCondition(doStatement.Condition);
            return BigOComplexity.Multiply(iteration, AnalyzeStatement(doStatement.Statement));
        }

        private static BigOComplexity AnalyzeForEach(ForEachStatementSyntax forEachStatement)
        {
            var iteration = AnalyzeCollection(forEachStatement.Expression);
            return BigOComplexity.Multiply(iteration, AnalyzeStatement(forEachStatement.Statement));
        }

        private static BigOComplexity AnalyzeLoopCondition(ExpressionSyntax condition)
        {
            if (condition == null) return BigOComplexity.Unknown;
            if (!(condition is BinaryExpressionSyntax binary)) return BigOComplexity.Unknown;
            if (IsConstantExpression(binary.Right)) return BigOComplexity.Constant;
            if (IsConstantExpression(binary.Left)) return BigOComplexity.Constant;

            var variable = GetLimitVariable(binary);
            if (variable == null) return BigOComplexity.Unknown;

            return BigOComplexity.LinearOf(variable);
        }

        private static BigOComplexity AnalyzeCollection(ExpressionSyntax expression)
        {
            if (IsConstantExpression(expression)) return BigOComplexity.Constant;
            if (expression is IdentifierNameSyntax) return BigOComplexity.Linear;
            if (expression is MemberAccessExpressionSyntax) return BigOComplexity.Linear;

            return BigOComplexity.Unknown;
        }

        private static string GetLimitVariable(BinaryExpressionSyntax binary)
        {
            var right = GetVariableName(binary.Right);
            if (right != null) return right;

            return GetVariableName(binary.Left);
        }

        private static string GetVariableName(ExpressionSyntax expression)
        {
            if (expression is IdentifierNameSyntax identifier) return identifier.Identifier.ValueText;
            if (expression is MemberAccessExpressionSyntax memberAccess) return memberAccess.Name.Identifier.ValueText;

            return null;
        }

        private static bool IsConstantExpression(ExpressionSyntax expression)
        {
            if (expression is LiteralExpressionSyntax) return true;
            if (expression is ArrayCreationExpressionSyntax) return true;
            if (expression is ImplicitArrayCreationExpressionSyntax) return true;

            return false;
        }

        private static void ReportIfNeeded(
            SyntaxNodeAnalysisContext context,
            SyntaxNode node,
            BigOComplexity complexity)
        {
            if (complexity.IsUnknown) return;
            if (complexity.CompareTo(BigOComplexity.Linear) < 0) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), complexity.ToString()));
        }
    }
}
