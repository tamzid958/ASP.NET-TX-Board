namespace AspNet.Tx.Board.Models;

public sealed class TxRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Method { get; init; } = string.Empty;

    public string Status { get; init; } = "Committed";

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset EndedAt { get; init; }

    public long DurationMs { get; init; }

    public int ConnectionCount { get; init; }

    public int ExecutedQueryCount { get; init; }

    public IReadOnlyList<string> ExecutedQueries { get; init; } = Array.Empty<string>();

    public string? Path { get; init; }

    public string? HttpMethod { get; init; }

    public bool IsUnhealthy { get; set; }

    public string DurationBucket { get; set; } = string.Empty;
}
