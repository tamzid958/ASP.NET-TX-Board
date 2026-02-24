namespace AspNet.Tx.Board.Domain;

public sealed class PageResponse<T>
{
    public IReadOnlyList<T> Content { get; init; } = [];
    public long TotalElements { get; init; }
    public int Page { get; init; }
    public int Size { get; init; }
    public int TotalPages => Size == 0 ? 0 : (int)Math.Ceiling((double)TotalElements / Size);
    public bool First => Page == 0;
    public bool Last => Page >= TotalPages - 1;
}
