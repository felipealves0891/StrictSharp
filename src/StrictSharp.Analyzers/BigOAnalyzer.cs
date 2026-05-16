using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
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

            var symbol = context.SemanticModel.GetDeclaredSymbol(method);
            var state = new AnalysisState(context.SemanticModel, symbol);

            var recursion = AnalyzeDirectRecursion(method, symbol, context.SemanticModel);
            if (!recursion.IsUnknown)
            {
                ReportIfNeeded(context, method.Body, recursion);
                return;
            }

            ReportIfNeeded(context, method.Body, AnalyzeBlock(method.Body, state));
        }

        private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            var constructor = (ConstructorDeclarationSyntax)context.Node;
            if (constructor.Body == null) return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(constructor);
            var state = new AnalysisState(context.SemanticModel, symbol);
            ReportIfNeeded(context, constructor.Body, AnalyzeBlock(constructor.Body, state));
        }

        private static BigOComplexity AnalyzeBlock(BlockSyntax block, AnalysisState state)
        {
            var result = BigOComplexity.Constant;
            foreach (var statement in block.Statements)
                result = BigOComplexity.AddSequential(result, AnalyzeStatement(statement, state));

            return result;
        }

        private static BigOComplexity AnalyzeStatement(StatementSyntax statement, AnalysisState state)
        {
            if (statement is BlockSyntax block) return AnalyzeBlock(block, state);
            if (statement is IfStatementSyntax ifStatement) return AnalyzeIf(ifStatement, state);
            if (statement is ForStatementSyntax forStatement) return AnalyzeFor(forStatement, state);
            if (statement is WhileStatementSyntax whileStatement) return AnalyzeWhile(whileStatement, state);
            if (statement is DoStatementSyntax doStatement) return AnalyzeDo(doStatement, state);
            if (statement is ForEachStatementSyntax forEachStatement) return AnalyzeForEach(forEachStatement, state);

            return AnalyzeInvocations(statement, state);
        }

        private static BigOComplexity AnalyzeIf(IfStatementSyntax ifStatement, AnalysisState state)
        {
            var whenTrue = AnalyzeStatement(ifStatement.Statement, state);
            var whenFalse = BigOComplexity.Constant;

            if (ifStatement.Else != null)
                whenFalse = AnalyzeStatement(ifStatement.Else.Statement, state);

            return BigOComplexity.AddSequential(whenTrue, whenFalse);
        }

        private static BigOComplexity AnalyzeFor(ForStatementSyntax forStatement, AnalysisState state)
        {
            var iteration = AnalyzeForIteration(forStatement);
            return BigOComplexity.Multiply(iteration, AnalyzeStatement(forStatement.Statement, state));
        }

        private static BigOComplexity AnalyzeWhile(WhileStatementSyntax whileStatement, AnalysisState state)
        {
            var iteration = AnalyzeWhileIteration(whileStatement.Condition, whileStatement.Statement);
            return BigOComplexity.Multiply(iteration, AnalyzeStatement(whileStatement.Statement, state));
        }

        private static BigOComplexity AnalyzeDo(DoStatementSyntax doStatement, AnalysisState state)
        {
            var iteration = AnalyzeWhileIteration(doStatement.Condition, doStatement.Statement);
            return BigOComplexity.Multiply(iteration, AnalyzeStatement(doStatement.Statement, state));
        }

        private static BigOComplexity AnalyzeForEach(ForEachStatementSyntax forEachStatement, AnalysisState state)
        {
            var iteration = AnalyzeCollection(forEachStatement.Expression);
            return BigOComplexity.Multiply(iteration, AnalyzeStatement(forEachStatement.Statement, state));
        }

        private static BigOComplexity AnalyzeForIteration(ForStatementSyntax forStatement)
        {
            var variable = GetLimitVariable(forStatement.Condition);
            if (HasLogarithmicIncrementor(forStatement, variable))
                return BigOComplexity.LogarithmicOf(variable);

            return AnalyzeLoopCondition(forStatement.Condition);
        }

        private static BigOComplexity AnalyzeWhileIteration(
            ExpressionSyntax condition,
            StatementSyntax statement)
        {
            var variable = GetLimitVariable(condition);
            if (HasLogarithmicMutation(statement, variable))
                return BigOComplexity.LogarithmicOf(variable);

            return AnalyzeLoopCondition(condition);
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

        private static string GetLimitVariable(ExpressionSyntax condition)
        {
            var binary = condition as BinaryExpressionSyntax;
            if (binary == null) return null;

            return GetLimitVariable(binary);
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

        private static bool HasLogarithmicIncrementor(ForStatementSyntax forStatement, string variable)
        {
            foreach (var incrementor in forStatement.Incrementors)
            {
                if (IsLogarithmicAssignment(incrementor, null)) return true;
            }

            return false;
        }

        private static bool HasLogarithmicMutation(StatementSyntax statement, string variable)
        {
            if (string.IsNullOrEmpty(variable)) return false;

            foreach (var node in statement.DescendantNodesAndSelf())
            {
                if (node is ExpressionSyntax expression && IsLogarithmicAssignment(expression, variable))
                    return true;
            }

            return false;
        }

        private static bool IsLogarithmicAssignment(ExpressionSyntax expression, string variable)
        {
            var assignment = expression as AssignmentExpressionSyntax;
            if (assignment == null) return false;
            if (!IsMultiplicativeAssignment(assignment)) return false;
            if (!IsConstantGreaterThanOne(assignment.Right)) return false;

            return IsVariable(assignment.Left, variable);
        }

        private static bool IsMultiplicativeAssignment(AssignmentExpressionSyntax assignment)
        {
            return assignment.IsKind(SyntaxKind.MultiplyAssignmentExpression)
                || assignment.IsKind(SyntaxKind.DivideAssignmentExpression);
        }

        private static bool IsConstantGreaterThanOne(ExpressionSyntax expression)
        {
            var literal = expression as LiteralExpressionSyntax;
            if (literal == null) return false;
            if (!(literal.Token.Value is int value)) return false;

            return value > 1;
        }

        private static bool IsVariable(ExpressionSyntax expression, string variable)
        {
            if (string.IsNullOrEmpty(variable)) return true;

            var identifier = expression as IdentifierNameSyntax;
            if (identifier == null) return false;

            return identifier.Identifier.ValueText == variable;
        }

        private static bool IsConstantExpression(ExpressionSyntax expression)
        {
            if (expression is LiteralExpressionSyntax) return true;
            if (expression is ArrayCreationExpressionSyntax) return true;
            if (expression is ImplicitArrayCreationExpressionSyntax) return true;

            return false;
        }

        private static BigOComplexity AnalyzeDirectRecursion(
            MethodDeclarationSyntax method,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel)
        {
            if (methodSymbol == null) return BigOComplexity.Unknown;

            InvocationExpressionSyntax recursiveInvocation = null;
            var recursiveCount = 0;
            foreach (var invocation in method.Body.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (!SymbolEqualityComparer.Default.Equals(symbol, methodSymbol)) continue;

                recursiveCount++;
                recursiveInvocation = invocation;
            }

            if (recursiveCount == 0) return BigOComplexity.Unknown;
            if (recursiveCount > 1) return BigOComplexity.Exponential;

            return AnalyzeSingleRecursiveInvocation(methodSymbol, recursiveInvocation);
        }

        private static BigOComplexity AnalyzeSingleRecursiveInvocation(
            IMethodSymbol methodSymbol,
            InvocationExpressionSyntax invocation)
        {
            if (invocation.ArgumentList.Arguments.Count == 0) return BigOComplexity.Unknown;
            if (methodSymbol.Parameters.Length == 0) return BigOComplexity.Unknown;

            var parameterName = methodSymbol.Parameters[0].Name;
            var expression = invocation.ArgumentList.Arguments[0].Expression as BinaryExpressionSyntax;
            if (expression == null) return BigOComplexity.Unknown;
            if (!IsVariable(expression.Left, parameterName)) return BigOComplexity.Unknown;
            if (expression.IsKind(SyntaxKind.DivideExpression) && IsConstantGreaterThanOne(expression.Right))
                return BigOComplexity.LogarithmicOf(parameterName);

            if (expression.IsKind(SyntaxKind.SubtractExpression) && IsPositiveConstant(expression.Right))
                return BigOComplexity.LinearOf(parameterName);

            return BigOComplexity.Unknown;
        }

        private static bool IsPositiveConstant(ExpressionSyntax expression)
        {
            var literal = expression as LiteralExpressionSyntax;
            if (literal == null) return false;
            if (!(literal.Token.Value is int value)) return false;

            return value > 0;
        }

        private static BigOComplexity AnalyzeInvocations(SyntaxNode node, AnalysisState state)
        {
            var result = BigOComplexity.Constant;
            foreach (var invocation in node.DescendantNodesAndSelf())
            {
                if (!(invocation is InvocationExpressionSyntax invocationExpression)) continue;

                var cost = AnalyzeInvocation(invocationExpression, state);
                result = BigOComplexity.AddSequential(result, cost);
            }

            return result;
        }

        private static BigOComplexity AnalyzeInvocation(
            InvocationExpressionSyntax invocation,
            AnalysisState state)
        {
            var symbol = state.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol == null) return BigOComplexity.Unknown;
            if (!IsLocalMethod(symbol, state.CurrentSymbol)) return BigOComplexity.Constant;
            if (IsSameMethod(symbol, state.CurrentSymbol)) return BigOComplexity.Unknown;
            if (!state.Enter(symbol)) return BigOComplexity.Unknown;

            try
            {
                return AnalyzeLocalMethod(symbol, state);
            }
            finally
            {
                state.Exit(symbol);
            }
        }

        private static BigOComplexity AnalyzeLocalMethod(IMethodSymbol symbol, AnalysisState state)
        {
            foreach (var reference in symbol.DeclaringSyntaxReferences)
            {
                var method = reference.GetSyntax() as MethodDeclarationSyntax;
                if (method != null && method.Body != null)
                    return AnalyzeBlock(method.Body, state);
            }

            return BigOComplexity.Unknown;
        }

        private static bool IsLocalMethod(IMethodSymbol symbol, ISymbol currentSymbol)
        {
            if (currentSymbol == null) return false;
            if (symbol.ContainingType == null) return false;

            return SymbolEqualityComparer.Default.Equals(symbol.ContainingType, currentSymbol.ContainingType);
        }

        private static bool IsSameMethod(IMethodSymbol symbol, ISymbol currentSymbol)
        {
            return SymbolEqualityComparer.Default.Equals(symbol, currentSymbol);
        }

        private static void ReportIfNeeded(
            SyntaxNodeAnalysisContext context,
            SyntaxNode node,
            BigOComplexity complexity)
        {
            if (complexity.IsUnknown) return;
            if (complexity.CompareTo(BigOComplexity.Logarithmic) < 0) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), complexity.ToString()));
        }

        private sealed class AnalysisState
        {
            private readonly HashSet<IMethodSymbol> _visiting;

            public AnalysisState(SemanticModel semanticModel, ISymbol currentSymbol)
            {
                SemanticModel = semanticModel;
                CurrentSymbol = currentSymbol;
                _visiting = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            }

            public SemanticModel SemanticModel { get; }

            public ISymbol CurrentSymbol { get; }

            public bool Enter(IMethodSymbol symbol)
            {
                return _visiting.Add(symbol);
            }

            public void Exit(IMethodSymbol symbol)
            {
                _visiting.Remove(symbol);
            }
        }
    }
}
