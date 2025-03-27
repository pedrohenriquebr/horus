using FluentValidation;
using Horus.Modules.Core.Application.Abstractions.Messaging;
using Horus.Modules.Core.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Horus.Modules.Core.Application.Usecases.GetUserByEmail;

public class GetUserByEmailQuery : IQuery<GetUserByEmailQueryResponse>
{
    public string Email { get; set; }
}

public class GetUserByEmailQueryResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string PasswordHash { get; set; }
}


public class GetUserByEmailQueryValidator : AbstractValidator<GetUserByEmailQuery>
{
    public GetUserByEmailQueryValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .NotNull()
            .MaximumLength(255)
            .EmailAddress();
    }
}

public class GetUserByEmailQueryHandler : IQueryHandler<GetUserByEmailQuery, GetUserByEmailQueryResponse>
{
    private readonly DbContext _dbContext;

    public GetUserByEmailQueryHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetUserByEmailQueryResponse> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Set<User>().FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
            return null;

        return new GetUserByEmailQueryResponse()
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            PasswordHash = user.PasswordHash
        };
    }
}