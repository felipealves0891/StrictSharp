using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StrictSharp.Analyzers.Extensions
{    
    public static class AnalysisContextExtensions
    {
        public static void RegisterSyntaxNodeActionIfAssembly(this AnalysisContext context, Action<SyntaxNodeAnalysisContext> action, params SyntaxKind[] kind)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var userAssembly = compilationContext.Compilation.Assembly;

                // 3. Registramos a análise de binários (ex: string + string)
                compilationContext.RegisterSyntaxNodeAction(syntaxContext =>
                {
                    // Verificamos se o nó atual pertence ao código-fonte do Assembly do usuário
                    // Isso garante que não estamos analisando nada "injetado" ou externo
                    var symbol = syntaxContext.SemanticModel.GetDeclaredSymbol(
                        syntaxContext.Node.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault());
                    
                    if (symbol != null && !SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, userAssembly))
                        return; // Se o membro que contém o código não é do usuário, ignora
                    
                    action(syntaxContext);

                }, kind);    

            });
        }

        public static void RegisterOperationActionIfAssembly(this AnalysisContext context, Action<OperationAnalysisContext> action, OperationKind kind)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var userAssembly = compilationContext.Compilation.Assembly;

                compilationContext.RegisterOperationAction(operationContext =>
                {
                    if (!SymbolEqualityComparer.Default.Equals(operationContext.Compilation.Assembly, userAssembly))
                        return; // Se o membro que contém o código não é do usuário, ignora
                    
                    action(operationContext);

                }, kind);    

            });
        }
    }   
}
