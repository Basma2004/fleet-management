namespace Fleet.Shared.Domain;

/// <summary>
/// Input parameters for paginated queries with boundary clamping.
/// </summary>
public record PaginationParams
{
    private const int MaxPageSize = 200;
    private const int DefaultPageSize = 50;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}
