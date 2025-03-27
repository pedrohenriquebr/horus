using Horus.Modules.Core.Application.Abstractions.Messaging;
using Horus.Modules.Core.Application.Usecases.GetChatSessionsByUserId;

namespace Horus.Modules.Core.Application.Usecases.GetChatsByUserId;

public class GetChatSessionsByUserIdQuery : IQuery<GetChatSessionsByUserIdQueryResponse>
{
    public Guid UserId { get; set; }
    public int Offset { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}