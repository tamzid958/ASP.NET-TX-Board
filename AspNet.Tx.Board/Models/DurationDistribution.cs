namespace AspNet.Tx.Board.Models;

public sealed class DurationDistribution
{
    public required DurationRange Range { get; init; }
    public long Count { get; set; }
}
