using System.Collections.Concurrent;
using AspNet.Tx.Board.Domain;
using AspNet.Tx.Board.Models;

namespace AspNet.Tx.Board.Storage;

public sealed class InMemorySqlExecutionLogRepository : ISqlExecutionLogRepository
{
    private const int MaxLogs = 2000;
    private readonly ConcurrentQueue<SqlExecutionLog> _logs = new();

    public void Save(SqlExecutionLog log)
    {
        _logs.Enqueue(log);
        while (_logs.Count > MaxLogs)
            _logs.TryDequeue(out _);
    }

    public PageResponse<SqlExecutionLog> FindAll(PageRequest request)
    {
        var query = _logs.ToArray().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.ToLowerInvariant();
            query = query.Where(s => s.Thread.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // Sort
        query = (request.SortField?.ToLowerInvariant()) switch
        {
            "conoccupiedtime" => request.IsSortDescending
                ? query.OrderByDescending(s => s.ConOccupiedTime)
                : query.OrderBy(s => s.ConOccupiedTime),
            "conreleasetime" => request.IsSortDescending
                ? query.OrderByDescending(s => s.ConReleaseTime)
                : query.OrderBy(s => s.ConReleaseTime),
            "thread" => request.IsSortDescending
                ? query.OrderByDescending(s => s.Thread)
                : query.OrderBy(s => s.Thread),
            _ => request.IsSortDescending
                ? query.OrderByDescending(s => s.ConAcquiredTime)
                : query.OrderBy(s => s.ConAcquiredTime)
        };

        var list = query.ToList();
        var total = list.Count;

        var page = Math.Max(0, request.Page);
        var size = Math.Clamp(request.Size, 1, 1000);

        return new PageResponse<SqlExecutionLog>
        {
            Content = list.Skip(page * size).Take(size).ToList(),
            TotalElements = total,
            Page = page,
            Size = size
        };
    }
}
