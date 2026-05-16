using System;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public class HeavyObjectWithoutCachingAnalyzerTests
{
    [Fact]
    public async Task HeavyObjectCreating_WithCache_Regex()
    {
        var code = 
        """
            class C {
                private static readonly System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex("[A-z]");               
                void M() {
                }
            }
        """;

        // Precisamos indicar que esperamos um Warning no VerifyCS
        await VerifyCS.VerifyAnalyzerAsync<HeavyObjectWithoutCachingAnalyzer>(code);
    }

    [Fact]
    public async Task HeavyObjectCreating_WithoutCache_Regex()
    {
        var code = 
        """
            class C {
                private System.Text.RegularExpressions.Regex _regex = [|new System.Text.RegularExpressions.Regex("[A-z]")|];               
                void M() {
                }
            }
        """;

        // Precisamos indicar que esperamos um Warning no VerifyCS
        await VerifyCS.VerifyAnalyzerAsync<HeavyObjectWithoutCachingAnalyzer>(code);
    }

    [Fact]
    public async Task HeavyObjectCreating_WithCache_Random()
    {
        
        var code = 
        """
            class C {
                void M() {
                    var number = System.Random.Shared.Next();
                }
            }
        """;

        // Precisamos indicar que esperamos um Warning no VerifyCS
        await VerifyCS.VerifyAnalyzerAsync<HeavyObjectWithoutCachingAnalyzer>(code);
    }
    
    [Fact]
    public async Task HeavyObjectCreating_WithoutCache_Random()
    {
        
        var code = 
        """
            class C {
                void M() {
                    var number = [|new System.Random()|];
                }
            }
        """;

        // Precisamos indicar que esperamos um Warning no VerifyCS
        await VerifyCS.VerifyAnalyzerAsync<HeavyObjectWithoutCachingAnalyzer>(code);
    }
}
