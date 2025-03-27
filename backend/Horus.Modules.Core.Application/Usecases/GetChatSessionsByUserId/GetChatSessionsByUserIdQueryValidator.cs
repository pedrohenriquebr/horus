using FluentValidation;
using Horus.Modules.Core.Application.Usecases.GetChatsByUserId;

namespace Horus.Modules.Core.Application.Usecases.GetChatSessionsByUserId;

public class GetChatSessionsByUserIdQueryValidator : AbstractValidator<GetChatSessionsByUserIdQuery>
{
    public GetChatSessionsByUserIdQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}