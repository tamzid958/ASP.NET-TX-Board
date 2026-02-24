namespace AspNet.Tx.Board.Core;

internal sealed class SqlExecutionInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset ConAcquiredTime { get; set; }
    public DateTimeOffset? ConReleaseTime { get; set; }
    public string Thread { get; set; } = string.Empty;
    public List<string> Queries { get; } = [];
}
