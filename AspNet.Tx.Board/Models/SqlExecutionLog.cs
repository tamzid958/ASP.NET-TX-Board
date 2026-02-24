using System.Text.Json.Serialization;

namespace AspNet.Tx.Board.Models;

public sealed class SqlExecutionLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset ConAcquiredTime { get; init; }
    public DateTimeOffset ConReleaseTime { get; init; }
    public long ConOccupiedTime { get; init; }
    public bool AlarmingConnection { get; init; }
    public string Thread { get; init; } = string.Empty;

    // Keep typo from Spring to be compatible with the existing script.js
    [JsonPropertyName("executedQuires")]
    public List<string> ExecutedQueries { get; init; } = [];
}
