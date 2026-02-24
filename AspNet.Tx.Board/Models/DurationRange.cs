namespace AspNet.Tx.Board.Models;

public sealed class DurationRange : IComparable<DurationRange>
{
    public long MinMillis { get; init; }
    public long MaxMillis { get; init; }

    public bool Matches(long millis) => millis >= MinMillis && (MaxMillis == long.MaxValue || millis <= MaxMillis);

    public int CompareTo(DurationRange? other) => MinMillis.CompareTo(other?.MinMillis ?? 0);

    public override string ToString() =>
        MaxMillis == long.MaxValue ? $">{MinMillis}ms" : $"{MinMillis}-{MaxMillis}ms";
}
