using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using AspNet.Tx.Board.Core;

namespace AspNet.Tx.Board.Proxy;

/// <summary>
/// Wraps a <see cref="DbConnection"/> to intercept transaction and SQL execution events
/// for AspNet.Tx.Board monitoring. Use this to instrument raw ADO.NET or Dapper code.
/// </summary>
public sealed class TxBoardDbConnection : DbConnection
{
    private readonly DbConnection _inner;
    private readonly TxBoardListener _listener;

    public TxBoardDbConnection(DbConnection inner, TxBoardListener listener)
    {
        _inner = inner;
        _listener = listener;
        _inner.StateChange += OnInnerStateChange;
    }

    // ── State forwarding ──────────────────────────────────────────────────────

    [AllowNull]
    public override string ConnectionString
    {
        get => _inner.ConnectionString;
        set => _inner.ConnectionString = value;
    }

    public override string Database => _inner.Database;
    public override string DataSource => _inner.DataSource;
    public override string ServerVersion => _inner.ServerVersion;
    public override ConnectionState State => _inner.State;

    // ── Open / Close ──────────────────────────────────────────────────────────

    public override void Open()
    {
        _inner.Open();
        _listener.OnConnectionAcquired();
    }

    public override async Task OpenAsync(CancellationToken cancellationToken)
    {
        await _inner.OpenAsync(cancellationToken);
        _listener.OnConnectionAcquired();
    }

    public override void Close()
    {
        _listener.OnConnectionClosed();
        _inner.Close();
    }

    public override async Task CloseAsync()
    {
        _listener.OnConnectionClosed();
        await _inner.CloseAsync();
    }

    // ── Transaction ───────────────────────────────────────────────────────────

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        _listener.OnTransactionBegin(isolationLevel, method: null);
        var inner = _inner.BeginTransaction(isolationLevel);
        return new TxBoardDbTransaction(inner, _listener);
    }

    protected override async ValueTask<DbTransaction> BeginDbTransactionAsync(
        IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        _listener.OnTransactionBegin(isolationLevel, method: null);
        var inner = await _inner.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new TxBoardDbTransaction(inner, _listener);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    protected override DbCommand CreateDbCommand()
    {
        var cmd = _inner.CreateCommand();
        return new TxBoardDbCommand(cmd, _listener);
    }

    // ── Schema / misc ─────────────────────────────────────────────────────────

    public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

    public override DataTable GetSchema() => _inner.GetSchema();

    public override DataTable GetSchema(string collectionName) => _inner.GetSchema(collectionName);

    public override DataTable GetSchema(string collectionName, string?[] restrictionValues) =>
        _inner.GetSchema(collectionName, restrictionValues);

    private void OnInnerStateChange(object sender, StateChangeEventArgs e) =>
        OnStateChange(e);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.StateChange -= OnInnerStateChange;
            _inner.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        _inner.StateChange -= OnInnerStateChange;
        await _inner.DisposeAsync();
        base.Dispose(false);
    }
}
