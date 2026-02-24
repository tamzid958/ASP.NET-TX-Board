using AspNet.Tx.Board.Enums;
using AspNet.Tx.Board.Models;

namespace AspNet.Tx.Board.Core;

internal sealed class TransactionInfo
{
    public Guid? TxId { get; set; }
    public string Method { get; set; } = "Unknown";
    public TxPropagation Propagation { get; set; } = TxPropagation.Required;
    public TxIsolationLevel Isolation { get; set; } = TxIsolationLevel.Default;
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndTime { get; set; }
    public TransactionStatus Status { get; set; }
    public string Thread { get; set; } = string.Empty;
    public bool IsRoot { get; set; }
    public bool IsCompleted { get; set; }

    // SQL tracking
    public List<string> Queries { get; } = [];
    public List<string> PostTransactionQueries { get; } = [];

    // Nested transactions
    public List<TransactionInfo> Children { get; } = [];

    // Events (only populated for root)
    public List<TransactionEvent>? Events { get; set; }

    // Connection tracking
    public int AcquiredConnectionCount { get; set; }
    public int AlarmingConnectionCount { get; set; }
    public long TotalConnectionOccupiedMs { get; set; }
    public DateTimeOffset? LastConnectionAcquiredAt { get; set; }
}
