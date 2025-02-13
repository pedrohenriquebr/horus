using Ardalis.ApiEndpoints;

namespace Horus.Modules.Core.Application.Common.CQRS;

public abstract class BaseApiCommandHandler<TRequest> : EndpointBaseAsync.WithRequest<TRequest>.WithActionResult
{
}