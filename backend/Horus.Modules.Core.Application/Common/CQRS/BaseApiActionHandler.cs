using Ardalis.ApiEndpoints;

namespace Horus.Modules.Core.Application.Common.CQRS;

public abstract class
    BaseApiActionHandler<TRequest, TResponse> : EndpointBaseAsync.WithRequest<TRequest>.WithResult<TResponse>
{
}