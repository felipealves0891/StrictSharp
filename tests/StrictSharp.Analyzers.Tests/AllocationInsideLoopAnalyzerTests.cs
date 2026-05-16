using System;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public class AllocationInsideLoopAnalyzerTests
{
    [Fact]
    public async Task NewInForLoop_ShouldReportError()
    {
        // O código que queremos testar (como uma string)
        var testCode = @"
            using System;
            public class Test {
                public void DoWork() {
                    for (int i = 0; i < 10; i++) {
                        var data = [|new object()|]; 
                    }
                }
            }";

        await VerifyCS.VerifyAnalyzerAsync<AllocationInsideLoopAnalyzer>(testCode);
    }

    [Fact]
    public async Task NewOutsideLoop_ShouldNotReportError()
    {
        var testCode = @"
            public class Test {
                public void DoWork() {
                    var data = new object(); // Fora de loop, deve passar
                }
            }";

        // Não passamos nenhum 'expected', então o teste falha se encontrar qualquer erro
        await VerifyCS.VerifyAnalyzerAsync<AllocationInsideLoopAnalyzer>(testCode);
    }
}
