using System.Text.Json.Serialization;

namespace AspNet.Tx.Board.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TxIsolationLevel
{
    Default,
    ReadUncommitted,
    ReadCommitted,
    RepeatableRead,
    Serializable
}
