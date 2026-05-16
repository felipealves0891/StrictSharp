using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace StrictSharp.Analyzers.Tests;

public static class VerifyCS
{
    private static readonly ReferenceAssemblies Net10 = new ReferenceAssemblies(
        "net10.0",
        new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0"),
        Path.Combine("ref", "net10.0"));

    // Resolve o erro 'Diagnostic' criando a definição esperada
    public static DiagnosticResult Diagnostic<TAnalyzer>(string diagnosticId, string messageFormat, DiagnosticSeverity severity = DiagnosticSeverity.Error)
        where TAnalyzer : DiagnosticAnalyzer, new()
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(new DiagnosticDescriptor(diagnosticId, "", messageFormat, "", severity, true));

    public static async Task VerifyAnalyzerAsync<TAnalyzer>(string source, params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>();
        test.ReferenceAssemblies = Net10;
        test.TestCode = source;
        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }
}
