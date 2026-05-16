using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace StrictSharp.Analyzers.Tests;

public class AnonymousFunctionAnalyzerTests
{
    [Fact]
    public async Task AnonimousFunction_InLoop_ShouldReportError()
    {
        // O código que queremos testar (como uma string)
        var testCode = @"
            using System;
            using System.Collections.Generic;

            public class Test {
                public void DoWork() {
                    var list = new List<Action>();
                    for (int i = 0; i < 10; i++) {
                        list.Add(() => Console.WriteLine(i)); 
                    }
                }
            }";

        var expected = VerifyCS.Diagnostic<AnonymousFunctionAnalyzer>("PERF021", "Lambda captures 1 outer variable(s) (i) inside a loop", DiagnosticSeverity.Warning);
        expected = expected.WithLocation(9, 34);
        await VerifyCS.VerifyAnalyzerAsync<AnonymousFunctionAnalyzer>(testCode, expected);
    }
    
    [Fact]
    public async Task AnonimousFunction_OutLoop_ShouldReportError()
    {
        // O código que queremos testar (como uma string)
        var testCode = @"
            using System;
            using System.Collections.Generic;

            public class Test {
                public void DoWork() {
                    var list = new List<Action>();
                    var i = 1;
                    list.Add(() => Console.WriteLine(i)); 
                }
            }";

        var expected = VerifyCS.Diagnostic<AnonymousFunctionAnalyzer>("PERF020", "Lambda captures 1 outer variable(s) (i)", DiagnosticSeverity.Info);
        expected = expected.WithLocation(9, 30);
        await VerifyCS.VerifyAnalyzerAsync<AnonymousFunctionAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task AnonimousFunction_ThisCapture_ShouldReportError()
    {
        // O código que queremos testar (como uma string)
        var testCode = @"
            using System;
            using System.Collections.Generic;

            public class Test {
                private readonly int _i = 1;
                public void DoWork() {
                    var list = new List<Action>();
                    list.Add(() => Console.WriteLine(this._i)); 
                }
            }";

        var expected = VerifyCS.Diagnostic<AnonymousFunctionAnalyzer>("PERF022", "Capturing 'this' instance", DiagnosticSeverity.Warning);
        expected = expected.WithLocation(9, 30);
        await VerifyCS.VerifyAnalyzerAsync<AnonymousFunctionAnalyzer>(testCode, expected);
    }
}
