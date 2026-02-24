using AspNet.Tx.Board.Models;
using AspNet.Tx.Board.Storage;

namespace AspNet.Tx.Board.Tests;

public sealed class InMemoryTxBoardStoreTests
{
    [Fact]
    public async Task Query_AppliesFiltersOrderingAndPagination()
    {
        var store = new InMemoryTxBoardStore();
        var now = DateTimeOffset.UtcNow;

        await store.WriteAsync(new TxRecord
        {
            Method = "A",
            EndedAt = now.AddMinutes(-3),
            StartedAt = now.AddMinutes(-4),
            DurationMs = 90,
            IsUnhealthy = false
        });
        await store.WriteAsync(new TxRecord
        {
            Method = "B",
            EndedAt = now.AddMinutes(-2),
            StartedAt = now.AddMinutes(-3),
            DurationMs = 300,
            IsUnhealthy = true
        });
        await store.WriteAsync(new TxRecord
        {
            Method = "C",
            EndedAt = now.AddMinutes(-1),
            StartedAt = now.AddMinutes(-2),
            DurationMs = 1200,
            IsUnhealthy = true
        });

        var result = store.Query(
            from: now.AddMinutes(-2.5),
            to: now,
            unhealthyOnly: true,
            skip: 0,
            take: 1);

        Assert.Single(result);
        Assert.Equal("C", result[0].Method);
    }

    [Fact]
    public async Task GetDurationDistribution_GroupsRecordsIntoExpectedBuckets()
    {
        var store = new InMemoryTxBoardStore();
        var now = DateTimeOffset.UtcNow;

        await store.WriteAsync(new TxRecord { Method = "A", StartedAt = now, EndedAt = now, DurationMs = 50 });
        await store.WriteAsync(new TxRecord { Method = "B", StartedAt = now, EndedAt = now, DurationMs = 350 });
        await store.WriteAsync(new TxRecord { Method = "C", StartedAt = now, EndedAt = now, DurationMs = 950 });

        var distribution = store.GetDurationDistribution(new[] { 100, 500 });

        Assert.Equal(1, distribution["<= 100ms"]);
        Assert.Equal(1, distribution["<= 500ms"]);
        Assert.Equal(1, distribution["> 500ms"]);
    }
}
