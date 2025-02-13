using FluentValidation;
using Horus.Modules.Shared.Contracts.SharedKernel.Exceptions;
using MediatR;

namespace Horus.Modules.Core.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);

        var failures = _validators
            .Select(validator => validator.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(error => error != null)
            .ToList();

        if (failures.Any())
            throw new GlobalApplicationException(
                    ApplicationExceptionType.Validation,
                    "There some invalid data on your request"
                )
                .AddErrors(failures
                    .Select(d => d.ErrorMessage)
                    .ToList());

        return await next();
    }
}