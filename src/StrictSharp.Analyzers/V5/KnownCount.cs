namespace StrictSharp.Analyzers.V5;

public readonly struct KnownCount
{
    private KnownCount(long value, bool isMaximum, bool isKnown)
    {
        Value = value;
        IsKnown = isKnown;
        IsMaximum = isMaximum;
    }

    public long Value { get; }
    public bool IsKnown { get; }
    public bool IsMaximum { get; }

    public static KnownCount Exact(long value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));

        return new KnownCount(value, false, true);
    }

    public static KnownCount Maximum(long value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));

        return new KnownCount(value, true, true);
    }

    public static KnownCount Unknown => new(0, false, false);

}
