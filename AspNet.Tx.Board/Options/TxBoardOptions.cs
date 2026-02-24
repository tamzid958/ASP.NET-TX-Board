namespace AspNet.Tx.Board.Options;

public enum TxBoardLogType
{
    Simple,
    Details
}

public enum TxBoardStorageType
{
    InMemory,
    Redis
}

public sealed class TxBoardOptions
{
    public bool Enabled { get; set; } = true;

    public TxBoardLogType LogType { get; set; } = TxBoardLogType.Simple;

    public TxBoardStorageType Storage { get; set; } = TxBoardStorageType.InMemory;

    public ThresholdOptions AlarmingThreshold { get; set; } = new();

    public List<int> DurationBuckets { get; set; } = new() { 100, 500, 1000, 2000, 5000 };

    public RedisOptions Redis { get; set; } = new();
}

public sealed class ThresholdOptions
{
    public int Transaction { get; set; } = 1000;

    public int Connection { get; set; } = 1000;
}

public sealed class RedisOptions
{
    public TimeSpan EntityTtl { get; set; } = TimeSpan.FromDays(7);
}
