using AspNetCore.IQueryable.Extensions.Attributes;
using AspNetCore.IQueryable.Extensions.Pagination;
using AspNetCore.IQueryable.Extensions.Sort;

namespace Horus.Modules.Core.Application.Common;

public class BasePaginated : IQuerySort, IQueryPaging
{
    [QueryOperator(Max = 50)] public int? Limit { get; set; } = 10;

    [QueryOperator(HasName = "Page")] public int? Offset { get; set; } = 0;

    public string Sort { get; set; } = "name";
}