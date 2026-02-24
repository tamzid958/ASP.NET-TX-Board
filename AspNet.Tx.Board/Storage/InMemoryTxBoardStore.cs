using System.Collections.Concurrent;
using AspNet.Tx.Board.Models;

namespace AspNet.Tx.Board.Storage;

public sealed class InMemoryTxBoardStore : ITxBoardStore
{
    private const int MaxRecords = 5000;
    private readonly ConcurrentQueue<TxRecord> _records = new();

    public Task WriteAsync(TxRecord record, CancellationToken cancellationToken = default)
    {
        _records.Enqueue(record);

        while (_records.Count > MaxRecords)
        {
            _records.TryDequeue(out _);
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<TxRecord> Query(DateTimeOffset? from = null, DateTimeOffset? to = null, bool? unhealthyOnly = null, int skip = 0, int take = 100)
    {
        skip = Math.Max(skip, 0);
        take = Math.Clamp(take, 1, 1000);

        IEnumerable<TxRecord> query = _records.ToArray();

        if (from.HasValue)
        {
            query = query.Where(x => x.EndedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.EndedAt <= to.Value);
        }

        if (unhealthyOnly.HasValue && unhealthyOnly.Value)
        {
            query = query.Where(x => x.IsUnhealthy);
        }

        return query
            .OrderByDescending(x => x.EndedAt)
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public IReadOnlyDictionary<string, int> GetDurationDistribution(IReadOnlyList<int> buckets, DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        var orderedBuckets = buckets
            .Where(x => x > 0)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        var result = new Dictionary<string, int>();
        var records = Query(from, to, unhealthyOnly: null, skip: 0, take: 5000);

        foreach (var threshold in orderedBuckets)
        {
            var label = $"<= {threshold}ms";
            result[label] = 0;
        }

        result[$"> {orderedBuckets.LastOrDefault(0)}ms"] = 0;

        foreach (var record in records)
        {
            var placed = false;

            foreach (var threshold in orderedBuckets)
            {
                if (record.DurationMs <= threshold)
                {
                    result[$"<= {threshold}ms"]++;
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                result[$"> {orderedBuckets.LastOrDefault(0)}ms"]++;
            }
        }

        return result;
    }
}
