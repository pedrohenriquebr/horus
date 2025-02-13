using Ardalis.ApiEndpoints;

namespace Horus.Modules.Core.Application.Common.CQRS;

public abstract class
    BaseApiQueryHandler<TRequest, TResponse> : EndpointBaseAsync.WithRequest<TRequest>.WithActionResult<TResponse>
{
}