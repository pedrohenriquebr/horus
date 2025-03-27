namespace Horus.Modules.Core.Application.Usecases.GetChatsByUserId;

public class PaginatedReponse<T>
{
    public List<T> Items { get; init; } = new List<T>();
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
};