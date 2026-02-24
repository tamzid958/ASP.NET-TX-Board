using System.Text.Json.Serialization;
using AspNet.Tx.Board.Enums;

namespace AspNet.Tx.Board.Models;

public sealed class TransactionLog
{
    public Guid? TxId { get; init; }
    public string Method { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TxPropagation Propagation { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TxIsolationLevel Isolation { get; init; }

    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public long Duration { get; init; }
    public ConnectionSummary? ConnectionSummary { get; init; }
    public bool? ConnectionOriented { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TransactionStatus Status { get; init; }

    public string Thread { get; init; } = string.Empty;

    // Keep typo from Spring to be compatible with the existing script.js
    [JsonPropertyName("executedQuires")]
    public List<string> ExecutedQueries { get; init; } = [];

    public List<TransactionLog> Child { get; init; } = [];
    public List<TransactionEvent> Events { get; init; } = [];
    public bool AlarmingTransaction { get; init; }
    public bool? HavingAlarmingConnection { get; init; }

    [JsonPropertyName("postTransactionQuires")]
    public List<string> PostTransactionQueries { get; init; } = [];

    [JsonIgnore]
    public int TotalTransactionCount => 1 + Child.Sum(c => c.TotalTransactionCount);

    [JsonIgnore]
    public int TotalQueryCount => ExecutedQueries.Count + Child.Sum(c => c.TotalQueryCount);
}
