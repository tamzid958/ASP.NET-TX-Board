using AspNet.Tx.Board.Models;
using Microsoft.Extensions.Logging;

namespace AspNet.Tx.Board.Storage;

public sealed class RedisTxBoardStoreFallback : ITxBoardStore
{
    private readonly InMemoryTxBoardStore _inner = new();

    public RedisTxBoardStoreFallback(ILogger<RedisTxBoardStoreFallback> logger)
    {
        logger.LogWarning("TxBoard storage is set to Redis, but Redis backend is not configured yet. Falling back to in-memory storage.");
    }

    public Task WriteAsync(TxRecord record, CancellationToken cancellationToken = default) =>
        _inner.WriteAsync(record, cancellationToken);

    public IReadOnlyList<TxRecord> Query(DateTimeOffset? from = null, DateTimeOffset? to = null, bool? unhealthyOnly = null, int skip = 0, int take = 100) =>
        _inner.Query(from, to, unhealthyOnly, skip, take);

    public IReadOnlyDictionary<string, int> GetDurationDistribution(IReadOnlyList<int> buckets, DateTimeOffset? from = null, DateTimeOffset? to = null) =>
        _inner.GetDurationDistribution(buckets, from, to);
}
