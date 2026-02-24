using AspNet.Tx.Board.Models;
using AspNet.Tx.Board.Options;
using AspNet.Tx.Board.Services;
using AspNet.Tx.Board.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AspNet.Tx.Board.Tests;

public sealed class TxBoardRecorderTests
{
    [Fact]
    public async Task RecordAsync_DoesNothing_WhenDisabled()
    {
        var store = new SpyStore();
        var options = CreateOptions(new TxBoardOptions { Enabled = false });
        var recorder = new TxBoardRecorder(store, options, NullLogger<TxBoardRecorder>.Instance);

        await recorder.RecordAsync(CreateRecord(durationMs: 1200, connectionCount: 10));

        Assert.Empty(store.Writes);
    }

    [Fact]
    public async Task RecordAsync_SetsBucketAndUnhealthy_ByTransactionThreshold()
    {
        var store = new SpyStore();
        var options = CreateOptions(new TxBoardOptions
        {
            Enabled = true,
            DurationBuckets = new List<int> { 100, 500, 1000 },
            AlarmingThreshold = new ThresholdOptions
            {
                Transaction = 800,
                Connection = 10
            }
        });
        var recorder = new TxBoardRecorder(store, options, NullLogger<TxBoardRecorder>.Instance);

        await recorder.RecordAsync(CreateRecord(durationMs: 900, connectionCount: 1));

        var write = Assert.Single(store.Writes);
        Assert.True(write.IsUnhealthy);
        Assert.Equal("500-1000ms", write.DurationBucket);
    }

    [Fact]
    public async Task RecordAsync_SetsUnhealthy_ByConnectionThreshold()
    {
        var store = new SpyStore();
        var options = CreateOptions(new TxBoardOptions
        {
            Enabled = true,
            DurationBuckets = new List<int> { 100, 500 },
            AlarmingThreshold = new ThresholdOptions
            {
                Transaction = 5000,
                Connection = 2
            }
        });
        var recorder = new TxBoardRecorder(store, options, NullLogger<TxBoardRecorder>.Instance);

        await recorder.RecordAsync(CreateRecord(durationMs: 120, connectionCount: 3));

        var write = Assert.Single(store.Writes);
        Assert.True(write.IsUnhealthy);
        Assert.Equal("100-500ms", write.DurationBucket);
    }

    private static TxRecord CreateRecord(long durationMs, int connectionCount)
    {
        var now = DateTimeOffset.UtcNow;

        return new TxRecord
        {
            Method = "Test.Method",
            Status = "Committed",
            StartedAt = now.AddMilliseconds(-durationMs),
            EndedAt = now,
            DurationMs = durationMs,
            ConnectionCount = connectionCount,
            ExecutedQueryCount = 1
        };
    }

    private static IOptionsMonitor<TxBoardOptions> CreateOptions(TxBoardOptions options) =>
        new StaticOptionsMonitor<TxBoardOptions>(options);

    private sealed class SpyStore : ITxBoardStore
    {
        public List<TxRecord> Writes { get; } = new();

        public Task WriteAsync(TxRecord record, CancellationToken cancellationToken = default)
        {
            Writes.Add(record);
            return Task.CompletedTask;
        }

        public IReadOnlyList<TxRecord> Query(DateTimeOffset? from = null, DateTimeOffset? to = null, bool? unhealthyOnly = null, int skip = 0, int take = 100) =>
            Writes;

        public IReadOnlyDictionary<string, int> GetDurationDistribution(IReadOnlyList<int> buckets, DateTimeOffset? from = null, DateTimeOffset? to = null) =>
            new Dictionary<string, int>();
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        public StaticOptionsMonitor(T currentValue)
        {
            CurrentValue = currentValue;
        }

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
