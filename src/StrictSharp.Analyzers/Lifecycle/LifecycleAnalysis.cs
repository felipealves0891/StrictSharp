using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrictSharp.Analyzers.Infrastructure;

namespace StrictSharp.Analyzers.Lifecycle;

public static class LifecycleAnalysis
{
    public static bool TryCreateContext(
        SyntaxNode node,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out LifecycleContext context)
    {
        context = default;
        if (node is not MethodDeclarationSyntax method)
            return false;

        var isDeclared = TryGetDeclaredContext(method, semanticModel, cancellationToken, out var declaredKind, out var requestedFrequency);
        if (!isDeclared
            && !IsLifecycleEntryPoint(method, semanticModel, cancellationToken)
            && !method.DescendantNodes().OfType<WhileStatementSyntax>().Any(HasExpandedLifecycleEvidence))
            return false;

        var loop = method.DescendantNodes()
            .OfType<WhileStatementSyntax>()
            .FirstOrDefault(candidate => IsLifecycleCondition(candidate.Condition)
                || HasExpandedLifecycleEvidence(candidate));
        if (loop is null)
            return false;

        var executionKind = isDeclared
            ? ToExecutionKind(declaredKind)
            : ClassifyExecutionContext(method, loop, semanticModel, cancellationToken);
        if (executionKind == LifecycleExecutionContextKind.Unknown)
            return false;

        var waitKind = ClassifyWait(loop, cancellationToken);
        var unit = InferCostUnit(loop, out var requestedIterationsPerSecond, cancellationToken);
        var frequencySource = requestedIterationsPerSecond.HasValue
            ? LifecycleFrequencySource.RequestedDelay
            : LifecycleFrequencySource.None;
        if (requestedFrequency.HasValue)
        {
            requestedIterationsPerSecond = requestedFrequency;
            unit = LifecycleCostUnit.CostPerTime;
            frequencySource = LifecycleFrequencySource.DeclaredHypothesis;
        }

        context = new LifecycleContext(
            method,
            loop,
            waitKind,
            ObservesCancellation(loop.Condition, cancellationToken),
            unit,
            requestedIterationsPerSecond,
            executionKind,
            isDeclared,
            frequencySource);
        return true;
    }

    public static bool IsServiceLifecycleLoop(
        SyntaxNode loop,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (loop is not WhileStatementSyntax whileLoop)
            return false;

        var method = whileLoop.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (method is null)
            return false;

        return TryCreateContext(method, semanticModel, cancellationToken, out var context)
            && context.Loop.Span == whileLoop.Span;
    }

    public static LifecycleWaitKind ClassifyWait(
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var invocations = node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>();
        foreach (var invocation in invocations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = GetInvocationName(invocation);
            if (name is "Delay")
                return LifecycleWaitKind.Temporal;
            if (name is "ReadAsync" or "WaitToReadAsync" or "ReceiveAsync")
                return LifecycleWaitKind.Event;
            if (name is "WaitAsync")
                return LifecycleWaitKind.Synchronization;
            if (name is "Yield")
                return LifecycleWaitKind.YieldOnly;
            if (name is "Wait" or "Sleep" or "GetResult")
                return LifecycleWaitKind.Blocking;
        }

        if (node.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>()
            .Any(member => member.Name.Identifier.ValueText is "Result" or "IsCompleted"))
            return LifecycleWaitKind.Blocking;

        if (node.DescendantNodesAndSelf().OfType<AwaitExpressionSyntax>().Any())
            return LifecycleWaitKind.Unknown;

        return LifecycleWaitKind.NoSuspension;
    }

    public static bool HasPollingOperation(SyntaxNode node)
    {
        return node.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation => GetInvocationName(invocation) is
                "TryDequeue" or "TryReceive" or "TryRead" or "Poll" or "Peek")
            || node.DescendantNodesAndSelf()
                .OfType<MemberAccessExpressionSyntax>()
                .Any(member => member.Name.Identifier.ValueText is "Count" or "IsEmpty");
    }

    public static bool HasRepeatedWork(SyntaxNode node)
    {
        return node.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation => GetInvocationName(invocation) is
                "ToList" or "ToArray" or "ToDictionary" or "ToLookup" or
                "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending")
            ;
    }

    public static LifecycleCostUnit InferCostUnit(
        SyntaxNode node,
        out double? requestedIterationsPerSecond,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        requestedIterationsPerSecond = null;
        if (HasWorkItemBoundary(node))
            return LifecycleCostUnit.CostPerWorkItem;

        if (TryGetRequestedFrequency(node, out var frequency, cancellationToken))
        {
            requestedIterationsPerSecond = frequency;
            return LifecycleCostUnit.CostPerTime;
        }

        return LifecycleCostUnit.CostPerIteration;
    }

    public static bool HasBlockingOperation(SyntaxNode node)
    {
        return node.DescendantNodesAndSelf()
                .OfType<MemberAccessExpressionSyntax>()
                .Any(member => member.Name.Identifier.ValueText is "Result" or "GetResult")
            || node.DescendantNodesAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .Any(invocation => GetInvocationName(invocation) is "Wait" or "Sleep" or "Read" or "Write" or "ReadToEnd");
    }

    public static bool HasBlockingOperationNotCoveredByPerf001(
        SyntaxNode node,
        SemanticModel semanticModel)
    {
        foreach (var member in node.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>())
        {
            if (member.Name.Identifier.ValueText is not ("Result" or "GetResult"))
                continue;

            if (member.Name.Identifier.ValueText == "Result"
                && member.Parent is not InvocationExpressionSyntax
                && IsPerf001Result(member, semanticModel))
                continue;

            if (member.Name.Identifier.ValueText == "GetResult"
                && member.Parent is InvocationExpressionSyntax invocation
                && IsPerf001GetResult(invocation, semanticModel))
                continue;

            return true;
        }

        foreach (var invocation in node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            var name = GetInvocationName(invocation);
            if (name is not ("Wait" or "Sleep" or "Read" or "Write" or "ReadToEnd"))
                continue;

            if (name == "Wait" && IsPerf001Wait(invocation, semanticModel))
                continue;

            return true;
        }

        return false;
    }

    public static bool HasUncancelableWait(SyntaxNode node)
    {
        foreach (var invocation in node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            var name = GetInvocationName(invocation);
            if (name is not ("Delay" or "ReadAsync" or "WaitAsync" or "WaitToReadAsync" or "ReceiveAsync"))
                continue;

            if (!invocation.ArgumentList.Arguments.Any(argument =>
                    argument.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                        .Any(identifier => identifier.Identifier.ValueText.Contains("token", StringComparison.OrdinalIgnoreCase)
                            || identifier.Identifier.ValueText.Contains("cancellation", StringComparison.OrdinalIgnoreCase))))
                return true;
        }

        return false;
    }

    public static bool HasGrowingParallelism(SyntaxNode node)
    {
        return node.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation => GetInvocationName(invocation) is "Run" or "StartNew" or "WhenAll")
            || node.DescendantNodesAndSelf()
                .OfType<ObjectCreationExpressionSyntax>()
                .Any(creation => creation.Type.ToString() is "Thread" or "Task");
    }

    private static bool HasWorkItemBoundary(SyntaxNode node)
    {
        return node.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation => GetInvocationName(invocation) is
                "TryDequeue" or "TryReceive" or "TryRead" or "ReadAsync" or
                "WaitToReadAsync" or "ReceiveAsync");
    }

    private static bool TryGetRequestedFrequency(
        SyntaxNode node,
        out double frequency,
        CancellationToken cancellationToken)
    {
        frequency = 0;
        foreach (var invocation in node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (GetInvocationName(invocation) != "Delay"
                || invocation.ArgumentList.Arguments.Count == 0
                || invocation.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax literal
                || !double.TryParse(literal.Token.ValueText, out var milliseconds)
                || milliseconds <= 0)
                continue;

            frequency = 1000d / milliseconds;
            return true;
        }

        return false;
    }

    private static bool IsBackgroundServiceMethod(
        MethodDeclarationSyntax method,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetDeclaredSymbol(method, cancellationToken);
        var type = symbol?.ContainingType;
        if (type is null)
            return false;

        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(current.ToDisplayString(), "Microsoft.Extensions.Hosting.BackgroundService", StringComparison.Ordinal)
                || string.Equals(current.Name, "BackgroundService", StringComparison.Ordinal)
                    && string.Equals(current.ContainingNamespace?.ToDisplayString(), "Microsoft.Extensions.Hosting", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool IsLifecycleEntryPoint(
        MethodDeclarationSyntax method,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetDeclaredSymbol(method, cancellationToken);
        if (symbol is null)
            return false;

        if (IsBackgroundServiceMethod(symbol, cancellationToken))
            return true;

        return symbol.ContainingType.AllInterfaces.Any(interfaceSymbol =>
            interfaceSymbol.Name == "IHostedService"
            && interfaceSymbol.ContainingNamespace.ToDisplayString() == "Microsoft.Extensions.Hosting");
    }

    private static bool IsBackgroundServiceMethod(
        IMethodSymbol method,
        CancellationToken cancellationToken)
    {
        for (var current = method.ContainingType.BaseType; current is not null; current = current.BaseType)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (current.Name == "BackgroundService"
                && current.ContainingNamespace?.ToDisplayString() == "Microsoft.Extensions.Hosting")
                return true;
        }

        return false;
    }

    private static LifecycleExecutionContextKind ClassifyExecutionContext(
        MethodDeclarationSyntax method,
        WhileStatementSyntax loop,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsBackgroundServiceMethod(method, semanticModel, cancellationToken)
            || IsLifecycleEntryPoint(method, semanticModel, cancellationToken)
            && IsLifecycleCondition(loop.Condition))
            return LifecycleExecutionContextKind.ServiceLifecycle;

        if (HasInvocation(loop, "ReadAsync", "WaitToReadAsync", "ReceiveAsync", "AcceptAsync", "MoveNextAsync"))
            return LifecycleExecutionContextKind.EventDriven;

        if (HasInvocation(loop, "Delay", "WaitForNextTickAsync", "Timer"))
            return LifecycleExecutionContextKind.Polling;

        if (HasInvocation(loop, "Retry", "Backoff", "BackoffAsync", "RetryAsync")
            || loop.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                .Any(identifier => identifier.Identifier.ValueText is "attempt" or "retryCount"))
            return LifecycleExecutionContextKind.Retry;

        if (loop.DescendantNodes().OfType<ForEachStatementSyntax>().Any())
            return LifecycleExecutionContextKind.Batch;

        if (HasInvocation(loop, "Update", "Render", "Tick"))
            return LifecycleExecutionContextKind.ServiceLifecycle;

        return LifecycleExecutionContextKind.Unknown;
    }

    private static bool HasExpandedLifecycleEvidence(SyntaxNode loop)
    {
        return HasInvocation(loop,
                "ReadAsync", "WaitToReadAsync", "ReceiveAsync", "AcceptAsync", "MoveNextAsync",
                "Delay", "WaitForNextTickAsync", "Timer", "Retry", "Backoff", "BackoffAsync", "RetryAsync",
                "Update", "Render", "Tick")
            || loop.DescendantNodes().OfType<ForEachStatementSyntax>().Any();
    }

    private static bool HasInvocation(SyntaxNode node, params string[] names)
    {
        return node.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation => names.Contains(GetInvocationName(invocation), StringComparer.Ordinal));
    }

    private static LifecycleExecutionContextKind ToExecutionKind(PerformanceContextKind kind)
    {
        return kind switch
        {
            PerformanceContextKind.BackgroundWorker => LifecycleExecutionContextKind.ServiceLifecycle,
            PerformanceContextKind.EventDriven => LifecycleExecutionContextKind.EventDriven,
            PerformanceContextKind.Polling => LifecycleExecutionContextKind.Polling,
            PerformanceContextKind.Retry => LifecycleExecutionContextKind.Retry,
            PerformanceContextKind.Batch => LifecycleExecutionContextKind.Batch,
            PerformanceContextKind.GameLoop => LifecycleExecutionContextKind.ServiceLifecycle,
            _ => LifecycleExecutionContextKind.Unknown
        };
    }

    private static bool TryGetDeclaredContext(
        MethodDeclarationSyntax method,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out PerformanceContextKind kind,
        out double? requestedFrequency)
    {
        kind = default;
        requestedFrequency = null;
        var symbol = semanticModel.GetDeclaredSymbol(method, cancellationToken);
        if (symbol is null)
            return false;

        var attributes = symbol.GetAttributes()
            .Where(IsPerformanceContextAttribute)
            .Concat(symbol.ContainingType.GetAttributes().Where(IsPerformanceContextAttribute))
            .ToArray();
        if (attributes.Length == 0)
            return false;

        var hasKind = false;
        foreach (var attribute in attributes)
        {
            if (!TryReadDeclaredContext(attribute, out var attributeKind, out var attributeFrequency))
                return false;

            if (hasKind && (kind != attributeKind || requestedFrequency != attributeFrequency))
                return false;

            kind = attributeKind;
            requestedFrequency = attributeFrequency;
            hasKind = true;
        }

        return hasKind;
    }

    private static bool IsPerformanceContextAttribute(AttributeData attribute)
    {
        return string.Equals(
            attribute.AttributeClass?.ToDisplayString(),
            "StrictSharp.Analyzers.Lifecycle.PerformanceContextAttribute",
            StringComparison.Ordinal);
    }

    private static bool TryReadDeclaredContext(
        AttributeData attribute,
        out PerformanceContextKind kind,
        out double? requestedFrequency)
    {
        kind = default;
        requestedFrequency = null;
        var hasKind = false;

        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == nameof(PerformanceContextAttribute.Kind))
            {
                if (argument.Value.Value is not int kindValue
                    || !Enum.IsDefined(typeof(PerformanceContextKind), kindValue))
                    return false;

                kind = (PerformanceContextKind)kindValue;
                hasKind = true;
                continue;
            }

            if (argument.Key != nameof(PerformanceContextAttribute.ExpectedIterationsPerSecond))
                continue;

            if (argument.Value.Value is double frequency && frequency > 0)
                requestedFrequency = frequency;
        }

        return hasKind;
    }

    private static bool IsLifecycleCondition(ExpressionSyntax condition)
    {
        return condition.DescendantNodesAndSelf()
            .OfType<MemberAccessExpressionSyntax>()
            .Any(member => member.Name.Identifier.ValueText == "IsCancellationRequested")
            || condition.IsKind(SyntaxKind.TrueLiteralExpression);
    }

    private static bool ObservesCancellation(SyntaxNode node, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return node.DescendantNodesAndSelf()
            .OfType<IdentifierNameSyntax>()
            .Any(identifier => identifier.Identifier.ValueText.Contains("token", StringComparison.OrdinalIgnoreCase)
                || identifier.Identifier.ValueText.Contains("cancellation", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetInvocationName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => string.Empty
        };
    }

    private static bool IsPerf001Result(
        MemberAccessExpressionSyntax member,
        SemanticModel semanticModel)
    {
        return semanticModel.GetSymbolInfo(member).Symbol is IPropertySymbol property
            && property.Name == "Result"
            && RuleAnalysis.IsTaskLike(property.ContainingType)
            && !RuleAnalysis.IsKnownCompletedTask(member.Expression, semanticModel);
    }

    private static bool IsPerf001Wait(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        return semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
            && method.Name == "Wait"
            && RuleAnalysis.IsTaskLike(method.ContainingType)
            && invocation.Expression is MemberAccessExpressionSyntax wait
            && !RuleAnalysis.IsKnownCompletedTask(wait.Expression, semanticModel);
    }

    private static bool IsPerf001GetResult(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        return semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
            && method.Name == "GetResult"
            && RuleAnalysis.IsAwaiter(method.ContainingType)
            && invocation.Expression is MemberAccessExpressionSyntax getResult
            && getResult.Expression is InvocationExpressionSyntax getAwaiter
            && getAwaiter.Expression is MemberAccessExpressionSyntax getAwaiterMember
            && getAwaiterMember.Name.Identifier.ValueText == "GetAwaiter"
            && !RuleAnalysis.IsKnownCompletedTask(getAwaiterMember.Expression, semanticModel);
    }
}
