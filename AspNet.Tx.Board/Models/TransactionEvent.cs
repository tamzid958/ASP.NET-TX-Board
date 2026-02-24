using System.Text.Json.Serialization;

namespace AspNet.Tx.Board.Models;

public enum TransactionEventType
{
    TransactionStart,
    TransactionEnd,
    ConnectionAcquired,
    ConnectionReleased
}

public sealed class TransactionEvent
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TransactionEventType Type { get; init; }

    public DateTimeOffset Timestamp { get; init; }

    public string? Details { get; init; }
}
