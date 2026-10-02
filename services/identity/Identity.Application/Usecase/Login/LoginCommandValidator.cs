using FluentValidation;
using Identity.Application.Common.Enums;

namespace Identity.Application.Usecase.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.UserName)
            .NotEmpty()
            .WithErrorCode(nameof(ErrorCode.UsernameIsRequired))
            .WithMessage(Resources.UsernameIsRequired);

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithErrorCode(nameof(ErrorCode.PasswordIsRequired))
            .WithMessage(Resources.PasswordIsRequired);
    }
}
