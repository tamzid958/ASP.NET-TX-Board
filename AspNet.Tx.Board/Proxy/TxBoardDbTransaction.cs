using System.Data;
using System.Data.Common;
using AspNet.Tx.Board.Core;

namespace AspNet.Tx.Board.Proxy;

public sealed class TxBoardDbTransaction : DbTransaction
{
    private readonly DbTransaction _inner;
    private readonly TxBoardListener _listener;
    private bool _completed;

    public TxBoardDbTransaction(DbTransaction inner, TxBoardListener listener)
    {
        _inner = inner;
        _listener = listener;
    }

    protected override DbConnection? DbConnection => _inner.Connection;

    public override IsolationLevel IsolationLevel => _inner.IsolationLevel;

    public override void Commit()
    {
        _inner.Commit();
        if (!_completed)
        {
            _completed = true;
            _listener.OnAfterCommit();
        }
    }

    public override async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _inner.CommitAsync(cancellationToken);
        if (!_completed)
        {
            _completed = true;
            _listener.OnAfterCommit();
        }
    }

    public override void Rollback()
    {
        _inner.Rollback();
        if (!_completed)
        {
            _completed = true;
            _listener.OnAfterRollback();
        }
    }

    public override async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await _inner.RollbackAsync(cancellationToken);
        if (!_completed)
        {
            _completed = true;
            _listener.OnAfterRollback();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!_completed)
            {
                _completed = true;
                _listener.OnAfterRollback();
            }
            _inner.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            _completed = true;
            _listener.OnAfterRollback();
        }
        await _inner.DisposeAsync();
        base.Dispose(false);
    }
}
