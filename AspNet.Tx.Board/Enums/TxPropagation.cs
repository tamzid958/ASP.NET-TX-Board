using System.Text.Json.Serialization;

namespace AspNet.Tx.Board.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TxPropagation
{
    Required,
    Supports,
    Mandatory,
    RequiresNew,
    NotSupported,
    Never,
    Nested
}
