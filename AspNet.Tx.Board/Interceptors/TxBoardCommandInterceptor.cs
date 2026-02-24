using System.Data.Common;
using AspNet.Tx.Board.Core;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AspNet.Tx.Board.Interceptors;

/// <summary>
/// EF Core interceptor that records SQL commands for AspNet.Tx.Board monitoring.
/// Register via <c>optionsBuilder.AddInterceptors(interceptor)</c>.
/// </summary>
public sealed class TxBoardCommandInterceptor : DbCommandInterceptor
{
    private readonly TxBoardListener _listener;

    public TxBoardCommandInterceptor(TxBoardListener listener)
    {
        _listener = listener;
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        _listener.OnSqlExecuted(command.CommandText);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        _listener.OnSqlExecuted(command.CommandText);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(
        DbCommand command, CommandExecutedEventData eventData, int result)
    {
        _listener.OnSqlExecuted(command.CommandText);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        _listener.OnSqlExecuted(command.CommandText);
        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(
        DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        _listener.OnSqlExecuted(command.CommandText);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result,
        CancellationToken cancellationToken = default)
    {
        _listener.OnSqlExecuted(command.CommandText);
        return ValueTask.FromResult(result);
    }
}
