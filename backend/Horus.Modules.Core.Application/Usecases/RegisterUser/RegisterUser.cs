using System.Reactive;
using FluentValidation;
using Horus.Modules.Core.Application.Abstractions.Messaging;
using Horus.Modules.Core.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Horus.Modules.Core.Application.Usecases.RegisterUser;

public class RegisterUserCommand : ICommand
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
}

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .NotNull()
            .MaximumLength(255)
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .NotNull()
            .MaximumLength(255)
            .MinimumLength(6);
        
        RuleFor(x => x.Name)
            .NotEmpty()
            .NotNull()
            .MaximumLength(255)
            .MinimumLength(6);
        
    }
}


public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand>
{
    private readonly DbContext _dbContext;
    private readonly IOptions<SecurityOptions> _securityOptions;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(DbContext dbContext, IOptions<SecurityOptions> securityOptions, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _securityOptions = securityOptions;
        _passwordHasher = passwordHasher;
    }
    
    public async Task Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var (hash, salt) = _passwordHasher.HashPassword(request.Password);
        
        var user = new Domain.Entities.User(
            id: Guid.NewGuid(),
            email: request.Email,
            passwordHash: hash,
            name:request.Name,
            salt: salt
        );
        
        await _dbContext.AddAsync(user, cancellationToken);
    }
    
}