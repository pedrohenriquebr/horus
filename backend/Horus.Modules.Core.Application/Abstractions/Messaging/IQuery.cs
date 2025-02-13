using MediatR;

namespace Horus.Modules.Core.Application.Abstractions.Messaging;

public interface IQuery<out TResponse> : IRequest<TResponse>
{
}