using System.Data.Common;
using AspNet.Tx.Board.Core;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AspNet.Tx.Board.Interceptors;

/// <summary>
/// EF Core interceptor that tracks connection acquisition/release for AspNet.Tx.Board.
/// Register via <c>optionsBuilder.AddInterceptors(interceptor)</c>.
/// </summary>
public sealed class TxBoardConnectionInterceptor : DbConnectionInterceptor
{
    private readonly TxBoardListener _listener;

    public TxBoardConnectionInterceptor(TxBoardListener listener)
    {
        _listener = listener;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        _listener.OnConnectionAcquired();
    }

    public override Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _listener.OnConnectionAcquired();
        return Task.CompletedTask;
    }

    public override void ConnectionClosed(DbConnection connection, ConnectionEndEventData eventData)
    {
        _listener.OnConnectionClosed();
    }

    public override Task ConnectionClosedAsync(
        DbConnection connection, ConnectionEndEventData eventData)
    {
        _listener.OnConnectionClosed();
        return Task.CompletedTask;
    }
}
