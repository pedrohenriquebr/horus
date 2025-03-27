using AspNetCore.IQueryable.Extensions.Attributes;
using AspNetCore.IQueryable.Extensions.Pagination;
using AspNetCore.IQueryable.Extensions.Sort;

namespace Horus.Modules.Core.Application.Common;

public class BasePaginated : IQuerySort, IQueryPaging
{
    public int? Limit { get; set; } = 10;

    public int? Offset { get; set; } = 0;

    public string Sort { get; set; } = "name";
    
    public int PageSize { get; set; } = 10;
}