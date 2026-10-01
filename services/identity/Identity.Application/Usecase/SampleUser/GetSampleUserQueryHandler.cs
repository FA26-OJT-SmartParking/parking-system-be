using Identity.Application.Common.Interfaces.Persistence;
using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Usecase.SampleUser;

/// <summary>Takes the user from the Persistence layer through the unit of work and maps it to a DTO.</summary>
public class GetSampleUserQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetSampleUserQuery, UserDto>
{
    public Task<UserDto> Handle(GetSampleUserQuery request, CancellationToken cancellationToken)
    {
        // The validator has already rejected an empty name
        var user = unitOfWork.UserRepository.GetSample(request.Name!);

        return Task.FromResult(new UserDto(user.Id, user.Name, user.Email, user.PhoneNumber));
    }
}
