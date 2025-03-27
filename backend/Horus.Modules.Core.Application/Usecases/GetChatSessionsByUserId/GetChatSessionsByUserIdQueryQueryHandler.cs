using Horus.Modules.Core.Application.Abstractions.Messaging;
using Horus.Modules.Core.Application.Usecases.GetChatsByUserId;
using Horus.Modules.Core.Domain;
using Horus.Modules.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
// using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;


namespace Horus.Modules.Core.Application.Usecases.GetChatSessionsByUserId;

public class GetChatSessionsByUserIdQueryQueryHandler: IQueryHandler<GetChatSessionsByUserIdQuery, GetChatSessionsByUserIdQueryResponse>
{
    private readonly DbContext _dbContext;

    public GetChatSessionsByUserIdQueryQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<GetChatSessionsByUserIdQueryResponse> Handle(GetChatSessionsByUserIdQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<ChatSession>()
            .FromSql($"SELECT * FROM chat_sessions WHERE metadata->>'userId' = {request.UserId.ToString()}")
            .AsNoTracking()
            .AsQueryable()
            .OrderByDescending(d => d.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);
        var items = await query
            .Skip((request.Offset - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new ()
        {
            Items = items, Total = total, Page = request.Offset, PageSize = request.PageSize, TotalPages = totalPages
        };
    }
}