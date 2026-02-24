using AspNet.Tx.Board.Models;
using AspNet.Tx.Board.Options;
using AspNet.Tx.Board.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNet.Tx.Board.Services;

public sealed class TxBoardRecorder : ITxBoardRecorder
{
    private readonly ITxBoardStore _store;
    private readonly IOptionsMonitor<TxBoardOptions> _options;
    private readonly ILogger<TxBoardRecorder> _logger;

    public TxBoardRecorder(ITxBoardStore store, IOptionsMonitor<TxBoardOptions> options, ILogger<TxBoardRecorder> logger)
    {
        _store = store;
        _options = options;
        _logger = logger;
    }

    public async Task RecordAsync(TxRecord record, CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;

        if (!options.Enabled)
        {
            return;
        }

        record.DurationBucket = ResolveDurationBucket(record.DurationMs, options.DurationBuckets);
        record.IsUnhealthy =
            record.DurationMs >= options.AlarmingThreshold.Transaction ||
            record.ConnectionCount >= options.AlarmingThreshold.Connection;

        await _store.WriteAsync(record, cancellationToken);
        Log(record, options.LogType);
    }

    private void Log(TxRecord record, TxBoardLogType logType)
    {
        if (logType == TxBoardLogType.Details)
        {
            var message =
                "[Tx-Board] Transaction Completed: ID={Id}, Method={Method}, Status={Status}, StartedAt={StartedAt}, EndedAt={EndedAt}, Duration={Duration}ms, Connections={Connections}, Queries={Queries}";

            if (record.IsUnhealthy)
            {
                _logger.LogWarning(message, record.Id, record.Method, record.Status, record.StartedAt, record.EndedAt, record.DurationMs, record.ConnectionCount, record.ExecutedQueryCount);
            }
            else
            {
                _logger.LogInformation(message, record.Id, record.Method, record.Status, record.StartedAt, record.EndedAt, record.DurationMs, record.ConnectionCount, record.ExecutedQueryCount);
            }

            return;
        }

        var simpleMessage = record.IsUnhealthy
            ? "Transaction [{Method}] took {Duration} ms, Status: {Status}, Connections: {Connections}, Queries: {Queries}"
            : "Transaction [{Method}] took {Duration} ms, Status: {Status}";

        if (record.IsUnhealthy)
        {
            _logger.LogWarning(simpleMessage, record.Method, record.DurationMs, record.Status, record.ConnectionCount, record.ExecutedQueryCount);
        }
        else
        {
            _logger.LogInformation(simpleMessage, record.Method, record.DurationMs, record.Status);
        }
    }

    private static string ResolveDurationBucket(long durationMs, IReadOnlyList<int> buckets)
    {
        var sortedBuckets = buckets
            .Where(x => x > 0)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        if (sortedBuckets.Length == 0)
        {
            return "all";
        }

        var previous = 0;

        foreach (var threshold in sortedBuckets)
        {
            if (durationMs <= threshold)
            {
                return $"{previous}-{threshold}ms";
            }

            previous = threshold;
        }

        return $"> {sortedBuckets[^1]}ms";
    }
}
