using FluentValidation;

namespace Identity.Application.Usecase.SampleUser;

public class GetSampleUserQueryValidator : AbstractValidator<GetSampleUserQuery>
{
    public GetSampleUserQueryValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("name is missing.");

        RuleFor(x => x.Password)
            .Must(password => !string.IsNullOrWhiteSpace(password))
            .WithMessage("password is missing.");

        RuleFor(x => x.PhoneNumber)
            .Must(phone => !string.IsNullOrWhiteSpace(phone))
            .WithMessage("phoneNumber is missing.")
            .Matches(@"^(0|\+84)(3|5|7|8|9)[0-9]{8}$")
            .WithMessage("phoneNumber must be a valid Vietnamese phone number.");
    }
}
