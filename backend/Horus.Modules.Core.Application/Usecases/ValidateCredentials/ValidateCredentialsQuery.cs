using Horus.Modules.Core.Application.Abstractions.Messaging;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Horus.Modules.Core.Application.Usecases.ValidateCredentials;

public class ValidateCredentialsQuery : IQuery<ValidateCredentialsQueryResponse>
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class ValidateCredentialsQueryResponse
{
    public bool IsValid { get; init; }
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
    public string? Name { get; init; }
}

public class ValidateCredentialsQueryHandler : IQueryHandler<ValidateCredentialsQuery, ValidateCredentialsQueryResponse>
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly DbContext _dbContext;

    public ValidateCredentialsQueryHandler(IPasswordHasher passwordHasher, DbContext dbContext)
    {
        _passwordHasher = passwordHasher;
        _dbContext = dbContext;
    }

    public async Task<ValidateCredentialsQueryResponse> Handle(ValidateCredentialsQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken: cancellationToken);

        if (user == null)
            return null;

        var passwordHash = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.Salt);

        if (!passwordHash)
            return new ValidateCredentialsQueryResponse
            {
                IsValid = false,
                UserId = null,
                Email = null,
                Name = null
            };

        return new ValidateCredentialsQueryResponse
        {
            IsValid = true,
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name
        };
    }
}