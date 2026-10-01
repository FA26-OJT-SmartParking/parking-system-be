using FluentValidation;

namespace Identity.Application.Usecase.SampleUser;

public class GetSampleUserQueryValidator : AbstractValidator<GetSampleUserQuery>
{
    public GetSampleUserQueryValidator()
    {
        RuleFor(query => query.Name)
            .NotEmpty()
            .WithMessage(Resources.NameIsRequired);
    }
}
