using System;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace StrictSharp.Analyzers.Benchmark;

[MemoryDiagnoser]
[DisassemblyDiagnoser]
[ThreadingDiagnoser]
public class StringConcatenationBenchmark
{
    private static readonly char[] Chars = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent lacus purus, pretium vitae dictum dapibus, elementum sit amet eros. Etiam porttitor nisi eu quam ullamcorper, eget vulputate neque ullamcorper. Duis at mauris est. Quisque viverra sodales pellentesque. Fusce ex nulla, ultricies at porttitor nec, convallis nec urna. Donec posuere ipsum et elit sodales pellentesque. Suspendisse volutpat tincidunt massa, sed malesuada velit fringilla nec. Maecenas tristique cursus ipsum non eleifend. Donec a ante sit amet quam pellentesque tincidunt. Cras laoreet interdum metus in eleifend. Nunc varius, lectus non dictum accumsan, orci metus vulputate metus, quis tempor sem leo quis est. Phasellus ut nibh sapien. Phasellus eget urna lectus. Donec eu tincidunt mi. Fusce non tortor et erat pretium maximus. Praesent metus magna, tempus id dolor quis, sodales imperdiet sem. Maecenas a elit enim. Sed condimentum justo eu venenatis condimentum. Ut ac sem feugiat, consectetur magna vel, porta ligula. Proin condimentum ipsum sit amet sapien porta fermentum. Etiam ac semper enim. Nunc auctor eleifend ante, vel scelerisque lectus tristique nec. Quisque sed lorem posuere, eleifend urna et, rhoncus erat. Proin ut lobortis velit. Fusce pulvinar eu velit in fringilla. Donec dictum erat dictum justo volutpat, non aliquam justo tempus. Vestibulum facilisis vel ipsum nec vehicula. Pellentesque sit amet nibh vitae sapien porttitor tempus. Praesent blandit nulla eget facilisis rutrum. Quisque nec nec.".ToCharArray();

    [Benchmark]
    public void ConcatWithOperation()
    {
        var result = string.Empty;
        foreach (var c in Chars)
            result = result + c;
    }

    [Benchmark]
    public void ConcatWithInterpolation()
    {
        var result = string.Empty;
        foreach (var c in Chars)
            result = $"{result}{c}";
    }
    
    [Benchmark]
    public void ConcatWithJoin()
    {
        var result = string.Join("", Chars);
    }
    
    [Benchmark]
    public void ConcatWithStringBuilder()
    {
        var result = new StringBuilder();
        foreach (var c in Chars)
            result.Append(c);
    }


}
