using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Net.Http;
using StrictSharp.Analyzers.Infrastructure;
using StrictSharp.Analyzers.Rules;
using Xunit;

#pragma warning disable PERF012, PERF013

namespace StrictSharp.Analyzers.Tests;

public sealed class InfrastructureTests
{
    [Fact]
    public void DiagnosticUtilityPreservesRuleAndSourceLocation()
    {
        var tree = CSharpSyntaxTree.ParseText("public sealed class Sample { }");
        var node = tree.GetRoot().DescendantNodes().Single();
        var rule = RuleCatalog.Get("PERF001");
        var diagnostic = RuleAnalysis.CreateDiagnostic(rule, node.GetLocation());

        Assert.Equal("PERF001", diagnostic.Id);
        Assert.True(RuleAnalysis.IsSourceLocation(diagnostic.Location));
        var message = diagnostic.GetMessage();
        Assert.Contains("[PERF001]", message, StringComparison.Ordinal);
        Assert.Contains("Evidência:", message, StringComparison.Ordinal);
        Assert.Contains("Impacto:", message, StringComparison.Ordinal);
        Assert.Contains("Escalabilidade:", message, StringComparison.Ordinal);
        Assert.Contains("Correção:", message, StringComparison.Ordinal);
        Assert.Contains("Exceções:", message, StringComparison.Ordinal);
        Assert.Contains("Confiança:", message, StringComparison.Ordinal);
        Assert.Contains("Referência:", message, StringComparison.Ordinal);
        Assert.DoesNotContain("%", message, StringComparison.Ordinal);
        Assert.False(RuleAnalysis.IsSourceLocation(Location.None));
    }

    [Fact]
    public void TypePredicatesRecognizeSupportedTaskAndCollectionShapes()
    {
        var (compilation, model, root) = CreateModel("""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class Sample
            {
                void Run(Task<int> task, ValueTask<int> valueTask, List<int> list, Dictionary<string, int> map, IEnumerable<int> sequence, ICollection<int> collection)
                {
                    _ = task; _ = valueTask; _ = list; _ = map; _ = sequence;
                }
            }
            """);

        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
        var types = method.ParameterList.Parameters
            .Select(parameter => model.GetTypeInfo(parameter.Type!).Type)
            .ToArray();

        Assert.True(RuleAnalysis.IsTaskLike(types[0]));
        Assert.True(RuleAnalysis.IsTaskLike(types[1]));
        Assert.True(RuleAnalysis.IsTaskType(types[0]));
        Assert.False(RuleAnalysis.IsTaskType(types[1]));
        Assert.True(RuleAnalysis.IsListLike(types[2]));
        Assert.True(RuleAnalysis.IsDictionaryLike(types[3]));
        Assert.True(RuleAnalysis.IsEnumerable(types[4]));
        Assert.False(RuleAnalysis.IsEnumerable(compilation.GetSpecialType(SpecialType.System_Int32)));
        Assert.False(RuleAnalysis.IsListLike(types[4]));
        Assert.False(RuleAnalysis.IsDictionaryLike(types[4]));
        Assert.False(RuleAnalysis.HasEfficientCount(types[2]));
        Assert.False(RuleAnalysis.HasEfficientCount(types[4]));
        Assert.True(RuleAnalysis.HasEfficientCount(types[5]));
    }

    [Fact]
    public void IoPredicatesRecognizeSynchronousAndAsynchronousApis()
    {
        var (_, model, root) = CreateModel("""
            using System.IO;
            using System.Net;
            using System.Net.Http;
            class Sample
            {
                void Run(Stream stream, TextReader reader, TextWriter writer, HttpClient client, WebClient web)
                {
                    File.ReadAllText("file");
                    File.ReadAllTextAsync("file");
                    File.Exists("file");
                    File.ReadAllBytes("file");
                    File.WriteAllText("file", text);
                    File.WriteAllBytes("file", new byte[1]);
                    File.AppendAllText("file", text);
                    File.OpenRead("file");
                    File.OpenWrite("file");
                    client.Send(new HttpRequestMessage());
                    web.DownloadData("url");
                    web.UploadString("url", text);
                    web.OpenRead("url");
                    stream.Read(new byte[1], 0, 1);
                    stream.ReadByte();
                    stream.Write(new byte[1], 0, 1);
                    stream.WriteByte(1);
                    stream.Flush();
                    stream.CopyTo(new MemoryStream());
                    reader.Read(new char[1], 0, 1);
                    reader.ReadLine();
                    reader.ReadToEnd();
                    reader.ReadBlock(new char[1], 0, 1);
                    writer.Write(text);
                    writer.WriteLine("value");
                    writer.Flush();
                }
            }
            """);

        var methods = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(invocation => model.GetSymbolInfo(invocation).Symbol)
            .OfType<IMethodSymbol>()
            .ToArray();

        Assert.Contains(methods, method => RuleAnalysis.IsSynchronousIo(method) && method.Name == "ReadAllText");
        Assert.Contains(methods, method => !RuleAnalysis.IsSynchronousIo(method) && method.Name == "ReadAllTextAsync");
        Assert.Contains(methods, method => !RuleAnalysis.IsSynchronousIo(method) && method.Name == "Exists");
        Assert.Contains(methods, method => RuleAnalysis.IsSynchronousIo(method) && method.Name == "Send");
        Assert.Contains(methods, method => RuleAnalysis.IsSynchronousIo(method) && method.Name == "DownloadData");
        Assert.Contains(methods, method => RuleAnalysis.IsSynchronousIo(method) && method.Name == "ReadByte");
        Assert.Contains(methods, method => RuleAnalysis.IsSynchronousIo(method) && method.Name == "ReadLine");
        Assert.Contains(methods, method => RuleAnalysis.IsSynchronousIo(method) && method.Name == "WriteLine");
    }

    [Fact]
    public void SyntaxAndSemanticPredicatesCoverContextualCases()
    {
        var (compilation, model, root) = CreateModel("""
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;
            class Sample
            {
                async Task<int> AsyncMethod() => await Task.FromResult(1);
                Task<int> TaskMethod() => Task.FromResult(1);
                int SyncMethod()
                {
                    for (var i = 0; i < 1; i++)
                        return Task.FromResult(1).Result;
                    return 0;
                }
                bool Compare(string value) => value.ToUpperInvariant() == "VALUE";
                bool CompareWithApi(string value) => value.ToLower().Equals("value");
                bool NotCompare(string value) => value.ToUpperInvariant().Trim() == "VALUE";
            }
            """);

        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(item => item.Identifier.Text == "AsyncMethod");
        var taskMethod = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(item => item.Identifier.Text == "TaskMethod");
        var syncMethod = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(item => item.Identifier.Text == "SyncMethod");
        Assert.True(RuleAnalysis.IsAsyncFlow(model, method.ExpressionBody!.Expression));
        Assert.True(RuleAnalysis.IsAsyncFlow(model, taskMethod.ExpressionBody!.Expression));
        Assert.False(RuleAnalysis.IsAsyncFlow(model, syncMethod));

        var returnExpression = syncMethod.DescendantNodes().OfType<ReturnStatementSyntax>().First().Expression!;
        Assert.True(RuleAnalysis.IsInsideLoop(returnExpression));
        Assert.False(RuleAnalysis.IsInsideLoop(method));
        var stringLiteral = root.DescendantNodes().OfType<LiteralExpressionSyntax>()
            .First(literal => literal.IsKind(SyntaxKind.StringLiteralExpression));
        Assert.True(RuleAnalysis.IsString(model.GetTypeInfo(stringLiteral).Type));

        var comparisonInvocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax member
                && member.Name.Identifier.Text is "ToUpperInvariant" or "ToLower"
                && invocation.Ancestors().OfType<MethodDeclarationSyntax>().Any(method => method.Identifier.Text is "Compare" or "CompareWithApi"))
            .ToArray();
        Assert.All(comparisonInvocations, invocation => Assert.True(RuleAnalysis.IsStringNormalizationComparison(invocation, model)));
        var nonComparison = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(invocation => invocation.ToString() == "value.ToUpperInvariant()"
                && invocation.Ancestors().OfType<MethodDeclarationSyntax>().Any(method => method.Identifier.Text == "NotCompare"));
        Assert.False(RuleAnalysis.IsStringNormalizationComparison(nonComparison, model));

        var awaiter = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.TaskAwaiter`1");
        Assert.NotNull(awaiter);
        Assert.True(RuleAnalysis.IsAwaiter(awaiter));
        Assert.False(RuleAnalysis.IsAwaiter(compilation.GetSpecialType(SpecialType.System_Int32)));
    }

    [Fact]
    public void AdditionalPredicatesCoverCompletedTasksCollectionsAndLogging()
    {
        var (compilation, model, root) = CreateModel("""
            using System.Collections;
            using System.Collections.Generic;
            using System.IO;
            using System.Threading.Tasks;
            interface ILogger { }
            class Sample
            {
                Task<int> GetAsync() => Task.FromResult(1);
                Task<int> Wrap() => Task.Run(() => GetAsync());
                void Run(Task<int> task, string text, int[] array, ICollection<int> collection, ILogger logger, ArrayList legacy)
                {
                    _ = Task.CompletedTask;
                    _ = Task.FromResult(1);
                    _ = Task.FromResult(1).Result;
                    _ = task.GetAwaiter();
                    _ = task.GetAwaiter().GetResult();
                    _ = Task.CompletedTask.GetAwaiter().GetResult();
                    _ = task.Result;
                    _ = (task.Result);
                    _ = text.Substring(0, 1);
                    _ = text.ToCharArray();
                    _ = array.Length;
                    _ = collection.Count;
                    _ = logger;
                    _ = legacy.Count;
                    File.ReadAllBytes("file");
                    File.WriteAllText("file", text);
                    File.AppendAllText("file", text);
                    File.OpenRead("file");
                    File.OpenWrite("file");
                }
            }
            """);

        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        var taskRun = invocations.First(invocation => invocation.ToString().StartsWith("Task.Run", StringComparison.Ordinal));
        var taskRunMethod = model.GetSymbolInfo(taskRun).Symbol as IMethodSymbol;
        Assert.NotNull(taskRunMethod);
        Assert.True(RuleAnalysis.IsTaskRun(taskRunMethod));
        var fromResultInvocation = invocations.First(invocation => invocation.ToString().StartsWith("Task.FromResult", StringComparison.Ordinal));
        Assert.False(RuleAnalysis.IsTaskRun(model.GetSymbolInfo(fromResultInvocation).Symbol as IMethodSymbol ?? throw new InvalidOperationException()));

        var completed = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
            .First(member => member.ToString() == "Task.CompletedTask");
        Assert.True(RuleAnalysis.IsKnownCompletedTask(completed, model));
        var parenthesized = root.DescendantNodes().OfType<ParenthesizedExpressionSyntax>()
            .First(expression => expression.ToString() == "(task.Result)");
        Assert.False(RuleAnalysis.IsKnownCompletedTask(parenthesized, model));
        var completedAwaiter = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(invocation => invocation.ToString() == "Task.CompletedTask.GetAwaiter()");
        Assert.True(RuleAnalysis.IsKnownCompletedTask(completedAwaiter, model));
        var fromResult = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(invocation => invocation.ToString() == "Task.FromResult(1)");
        Assert.True(RuleAnalysis.IsKnownCompletedTask(fromResult, model));

        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(item => item.Identifier.Text == "Run");
        var parameters = method.ParameterList.Parameters;
        var array = model.GetTypeInfo(parameters[2].Type!).Type;
        var collection = model.GetTypeInfo(parameters[3].Type!).Type;
        var logger = model.GetTypeInfo(parameters[4].Type!).Type;
        var legacy = model.GetTypeInfo(parameters[5].Type!).Type;
        var textType = model.GetTypeInfo(parameters[1].Type!).Type;
        Assert.True(RuleAnalysis.HasEfficientCount(array));
        Assert.True(RuleAnalysis.HasEfficientCount(collection));
        Assert.True(RuleAnalysis.HasEfficientCount(textType));
        Assert.True(RuleAnalysis.IsLogger(logger));
        Assert.False(RuleAnalysis.IsLogger(legacy));

        var stringMethods = invocations.Select(invocation => model.GetSymbolInfo(invocation).Symbol)
            .OfType<IMethodSymbol>().ToArray();
        Assert.Contains(stringMethods, RuleAnalysis.IsStringCopy);
        Assert.True(RuleAnalysis.IsEnumerable(compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1")));
        Assert.False(RuleAnalysis.IsEnumerable(legacy));
    }

    private static (CSharpCompilation Compilation, SemanticModel Model, SyntaxNode Root) CreateModel(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14));
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(File).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(HttpClient).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Net.WebClient).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        };
        var compilation = CSharpCompilation.Create(
            "InfrastructureTestAssembly",
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return (compilation, compilation.GetSemanticModel(tree), tree.GetRoot());
    }
}
