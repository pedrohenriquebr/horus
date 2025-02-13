using MediatR;

namespace Horus.Modules.Core.Application.Abstractions.Messaging;

public interface IQueryObject<out TResponse> : IRequest<TResponse>
{
}