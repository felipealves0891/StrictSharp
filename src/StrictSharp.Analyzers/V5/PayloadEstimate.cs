namespace StrictSharp.Analyzers.V5;

public readonly struct PayloadEstimate
{
    private PayloadEstimate(long bytes)
    {
        Bytes = bytes;
        IsKnown = true;
    }

    public long Bytes { get; }
    public bool IsKnown { get; }
    public bool IncludesObjectOverhead => false;

    public static PayloadEstimate Known(long bytes)
    {
        if (bytes < 0)
            throw new ArgumentOutOfRangeException(nameof(bytes));

        return new PayloadEstimate(bytes);
    }

    public static PayloadEstimate Unknown => default;
}
