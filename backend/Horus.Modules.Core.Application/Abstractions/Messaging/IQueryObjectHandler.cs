using MediatR;

namespace Horus.Modules.Core.Application.Abstractions.Messaging;

public interface IQueryObjectHandler<in TQueryObject, TResponse> : IRequestHandler<TQueryObject, TResponse>
    where TQueryObject : IQueryObject<TResponse>
{
}