using AspNet.Tx.Board.Models;

namespace AspNet.Tx.Board.Storage;

public interface ITxBoardStore
{
    Task WriteAsync(TxRecord record, CancellationToken cancellationToken = default);

    IReadOnlyList<TxRecord> Query(DateTimeOffset? from = null, DateTimeOffset? to = null, bool? unhealthyOnly = null, int skip = 0, int take = 100);

    IReadOnlyDictionary<string, int> GetDurationDistribution(IReadOnlyList<int> buckets, DateTimeOffset? from = null, DateTimeOffset? to = null);
}
