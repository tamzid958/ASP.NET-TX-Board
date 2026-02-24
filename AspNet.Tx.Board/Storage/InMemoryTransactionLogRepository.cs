using System.Collections.Concurrent;
using AspNet.Tx.Board.Domain;
using AspNet.Tx.Board.Enums;
using AspNet.Tx.Board.Models;
using AspNet.Tx.Board.Options;
using Microsoft.Extensions.Options;

namespace AspNet.Tx.Board.Storage;

public sealed class InMemoryTransactionLogRepository : ITransactionLogRepository
{
    private const int MaxLogs = 5000;
    private readonly ConcurrentQueue<TransactionLog> _logs = new();
    private readonly object _summaryLock = new();
    private TransactionSummary _summary = new();
    private readonly List<DurationRange> _buckets;

    // Per-bucket counters
    private readonly ConcurrentDictionary<int, long> _bucketCounts = new();

    public InMemoryTransactionLogRepository(IOptions<TxBoardOptions> options)
    {
        var sortedBuckets = options.Value.DurationBuckets
            .Where(b => b > 0)
            .Distinct()
            .OrderBy(b => b)
            .ToList();

        _buckets = BuildRanges(sortedBuckets);
        foreach (var (b, i) in _buckets.Select((b, i) => (b, i)))
            _bucketCounts[i] = 0;
    }

    public void Save(TransactionLog log)
    {
        _logs.Enqueue(log);

        while (_logs.Count > MaxLogs)
            _logs.TryDequeue(out _);

        UpdateSummary(log);
        UpdateDistribution(log.Duration);
    }

    public PageResponse<TransactionLog> FindAll(PageRequest request)
    {
        var query = _logs.ToArray().AsEnumerable();

        // Search
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.ToLowerInvariant();
            query = query.Where(t =>
                t.Method.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.Thread.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<TransactionStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(t => t.Status == status);
        }

        // Propagation filter
        if (!string.IsNullOrWhiteSpace(request.Propagation) &&
            Enum.TryParse<TxPropagation>(request.Propagation, ignoreCase: true, out var prop))
        {
            query = query.Where(t => t.Propagation == prop);
        }

        // Isolation filter
        if (!string.IsNullOrWhiteSpace(request.Isolation) &&
            Enum.TryParse<TxIsolationLevel>(request.Isolation, ignoreCase: true, out var iso))
        {
            query = query.Where(t => t.Isolation == iso);
        }

        // Connection oriented filter
        if (request.ConnectionOriented.HasValue)
        {
            query = query.Where(t => t.ConnectionOriented == request.ConnectionOriented.Value);
        }

        // Sort
        query = ApplySort(query, request.SortField, request.IsSortDescending);

        var list = query.ToList();
        var total = list.Count;

        var page = Math.Max(0, request.Page);
        var size = Math.Clamp(request.Size, 1, 1000);
        var content = list.Skip(page * size).Take(size).ToList();

        return new PageResponse<TransactionLog>
        {
            Content = content,
            TotalElements = total,
            Page = page,
            Size = size
        };
    }

    public TransactionSummary GetSummary()
    {
        lock (_summaryLock)
        {
            // Return a snapshot (clone the current summary)
            return new TransactionSummary
            {
                CommittedCount = _summary.CommittedCount,
                RolledBackCount = _summary.RolledBackCount,
                ErroredCount = _summary.ErroredCount,
                TotalDuration = _summary.TotalDuration,
                AlarmingCount = _summary.AlarmingCount,
                ConnectionAcquisitionCount = _summary.ConnectionAcquisitionCount,
                TotalConnectionOccupiedTime = _summary.TotalConnectionOccupiedTime,
                AlarmingConnectionCount = _summary.AlarmingConnectionCount
            };
        }
    }

    public List<DurationDistribution> GetDurationDistributions()
    {
        return _buckets.Select((b, i) => new DurationDistribution
        {
            Range = b,
            Count = _bucketCounts.TryGetValue(i, out var c) ? c : 0
        }).ToList();
    }

    private void UpdateSummary(TransactionLog log)
    {
        lock (_summaryLock)
        {
            switch (log.Status)
            {
                case TransactionStatus.Committed: _summary.CommittedCount++; break;
                case TransactionStatus.RolledBack: _summary.RolledBackCount++; break;
                case TransactionStatus.Errored: _summary.ErroredCount++; break;
            }

            _summary.TotalDuration += log.Duration;

            if (log.AlarmingTransaction)
                _summary.AlarmingCount++;

            if (log.ConnectionSummary is { } cs)
            {
                _summary.ConnectionAcquisitionCount += cs.AcquisitionCount;
                _summary.TotalConnectionOccupiedTime += cs.OccupiedTime;
                _summary.AlarmingConnectionCount += cs.AlarmingConnectionCount;
            }
        }
    }

    private void UpdateDistribution(long durationMs)
    {
        for (int i = 0; i < _buckets.Count; i++)
        {
            if (_buckets[i].Matches(durationMs))
            {
                _bucketCounts.AddOrUpdate(i, 1, (_, v) => v + 1);
                return;
            }
        }
    }

    private static IEnumerable<TransactionLog> ApplySort(
        IEnumerable<TransactionLog> query, string? field, bool descending)
    {
        return field?.ToLowerInvariant() switch
        {
            "method" => descending ? query.OrderByDescending(t => t.Method) : query.OrderBy(t => t.Method),
            "duration" => descending ? query.OrderByDescending(t => t.Duration) : query.OrderBy(t => t.Duration),
            "status" => descending ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
            "propagation" => descending ? query.OrderByDescending(t => t.Propagation) : query.OrderBy(t => t.Propagation),
            "isolation" => descending ? query.OrderByDescending(t => t.Isolation) : query.OrderBy(t => t.Isolation),
            "thread" => descending ? query.OrderByDescending(t => t.Thread) : query.OrderBy(t => t.Thread),
            _ => descending ? query.OrderByDescending(t => t.StartTime) : query.OrderBy(t => t.StartTime)
        };
    }

    private static List<DurationRange> BuildRanges(List<int> sortedBuckets)
    {
        var ranges = new List<DurationRange>();
        long prev = 0;

        foreach (var threshold in sortedBuckets)
        {
            ranges.Add(new DurationRange { MinMillis = prev, MaxMillis = threshold });
            prev = threshold + 1;
        }

        ranges.Add(new DurationRange { MinMillis = prev, MaxMillis = long.MaxValue });
        return ranges;
    }
}
