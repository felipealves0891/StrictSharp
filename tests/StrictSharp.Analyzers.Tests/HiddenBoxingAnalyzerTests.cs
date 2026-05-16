using System;
using System.Numerics;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public class HiddenBoxingAnalyzerTests
{
    [Fact]
    public async Task IntegerToObjectAssignment_ShouldReportWarning()
    {
        var code = @"
            class C {
                void M() {
                    int i = 42;
                    object o = [|i|]; // Boxing aqui
                }
            }";

        // Precisamos indicar que esperamos um Warning no VerifyCS
        await VerifyCS.VerifyAnalyzerAsync<HiddenBoxingAnalyzer>(code);
    }

    [Fact]
    public async Task ClassToInterfaceAssignment_ShouldReportWarning()
    {
        var code = @"
            using System.Collections.Generic;
            class C {
                void M() {
                    int i = 42;
                    System.Numerics.INumber<int> o = [|i|]; // Boxing aqui
                }
            }";

        // Precisamos indicar que esperamos um Warning no VerifyCS
        await VerifyCS.VerifyAnalyzerAsync<HiddenBoxingAnalyzer>(code);
    }

    [Fact]
    public async Task Generics_ShouldNotReportWarning()
    {
        var code = @"
            class C {
                void M<T>(T item) {
                    // Sem boxing se usarmos Generics corretamente
                }
                void Call() { M(42); }
            }";

        await VerifyCS.VerifyAnalyzerAsync<HiddenBoxingAnalyzer>(code);
    }
}
