using System.Data.Common;
using AspNet.Tx.Board.Core;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AspNet.Tx.Board.Interceptors;

/// <summary>
/// EF Core interceptor that tracks transaction lifecycle for AspNet.Tx.Board.
/// Register via <c>optionsBuilder.AddInterceptors(interceptor)</c>.
/// </summary>
public sealed class TxBoardTransactionInterceptor : DbTransactionInterceptor
{
    private readonly TxBoardListener _listener;

    public TxBoardTransactionInterceptor(TxBoardListener listener)
    {
        _listener = listener;
    }

    public override DbTransaction TransactionStarted(
        DbConnection connection, TransactionEndEventData eventData, DbTransaction transaction)
    {
        var method = eventData.Context?.GetType().Name is { } ctx
            ? $"{ctx}.BeginTransaction"
            : "DbContext.BeginTransaction";

        _listener.OnTransactionBegin(transaction.IsolationLevel, method);
        return transaction;
    }

    public override async ValueTask<DbTransaction> TransactionStartedAsync(
        DbConnection connection, TransactionEndEventData eventData, DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var method = eventData.Context?.GetType().Name is { } ctx
            ? $"{ctx}.BeginTransaction"
            : "DbContext.BeginTransaction";

        _listener.OnTransactionBegin(transaction.IsolationLevel, method);
        return await ValueTask.FromResult(transaction);
    }

    public override void TransactionCommitted(
        DbTransaction transaction, TransactionEndEventData eventData)
    {
        _listener.OnAfterCommit();
    }

    public override Task TransactionCommittedAsync(
        DbTransaction transaction, TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _listener.OnAfterCommit();
        return Task.CompletedTask;
    }

    public override void TransactionRolledBack(
        DbTransaction transaction, TransactionEndEventData eventData)
    {
        _listener.OnAfterRollback();
    }

    public override Task TransactionRolledBackAsync(
        DbTransaction transaction, TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _listener.OnAfterRollback();
        return Task.CompletedTask;
    }

    public override void TransactionFailed(
        DbTransaction transaction, TransactionErrorEventData eventData)
    {
        _listener.OnTransactionError();
    }

    public override Task TransactionFailedAsync(
        DbTransaction transaction, TransactionErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _listener.OnTransactionError();
        return Task.CompletedTask;
    }
}
