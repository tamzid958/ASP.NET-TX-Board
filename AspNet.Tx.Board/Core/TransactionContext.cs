namespace AspNet.Tx.Board.Core;

/// <summary>
/// Mutable per-request/per-call-chain state shared across async continuations via AsyncLocal.
/// Because this is a reference type, AsyncLocal flows the reference to all child contexts —
/// mutations (Push/Pop, SQL tracking) are visible to all continuations in the same logical chain.
/// </summary>
internal sealed class TransactionContext
{
    public Stack<TransactionInfo> TxStack { get; } = new();
    public SqlExecutionInfo? SqlInfo { get; set; }
}
