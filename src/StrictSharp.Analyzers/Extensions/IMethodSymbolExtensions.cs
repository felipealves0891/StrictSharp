using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace StrictSharp.Analyzers.Extensions
{
    public static class IMethodSymbolExtensions
    {
        private static bool IsHotPath(this IMethodSymbol method)
        {
            return method.GetAttributes().Any(attr =>
                attr.AttributeClass?.Name == "HotPathAttribute");
        }
    }    
}


