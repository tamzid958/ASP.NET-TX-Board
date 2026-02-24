namespace AspNet.Tx.Board.Models;

public sealed class ConnectionSummary
{
    public int AcquisitionCount { get; init; }
    public int AlarmingConnectionCount { get; init; }
    public long OccupiedTime { get; init; }
}
