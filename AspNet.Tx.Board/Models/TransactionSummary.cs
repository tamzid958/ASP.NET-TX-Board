namespace AspNet.Tx.Board.Models;

public sealed class TransactionSummary
{
    public long CommittedCount { get; set; }
    public long RolledBackCount { get; set; }
    public long ErroredCount { get; set; }
    public long TotalDuration { get; set; }
    public long AlarmingCount { get; set; }
    public long ConnectionAcquisitionCount { get; set; }
    public long TotalConnectionOccupiedTime { get; set; }
    public long AlarmingConnectionCount { get; set; }

    public long TotalTransaction => CommittedCount + RolledBackCount + ErroredCount;

    public double AverageDuration =>
        TotalTransaction == 0 ? 0 : (double)TotalDuration / TotalTransaction;

    public double AverageConnectionOccupiedTime =>
        ConnectionAcquisitionCount == 0 ? 0 : (double)TotalConnectionOccupiedTime / ConnectionAcquisitionCount;
}
