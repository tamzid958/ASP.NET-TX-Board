namespace AspNet.Tx.Board.Domain;

public sealed class PageRequest
{
    public int Page { get; init; } = 0;
    public int Size { get; init; } = 10;
    public string? SortField { get; init; }
    public string? SortDirection { get; init; }
    public string? Search { get; init; }

    // Transaction-specific filters
    public string? Status { get; init; }
    public string? Propagation { get; init; }
    public string? Isolation { get; init; }
    public bool? ConnectionOriented { get; init; }

    public bool IsSortDescending =>
        string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
}
