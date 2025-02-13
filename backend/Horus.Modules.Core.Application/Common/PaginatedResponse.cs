namespace Horus.Modules.Core.Application.Common;

public sealed class PaginatedResponse<T>
    where T : class
{
    public List<T> Items { get; init; }
    public int Page { get; init; }
    public int NexPage { get; init; }
    public bool LastPage { get; init; } = false;
    public int PreviousPage { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
}