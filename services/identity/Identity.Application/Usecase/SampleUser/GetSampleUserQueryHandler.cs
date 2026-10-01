using Identity.Application.Common.Interfaces.Persistence;
using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Usecase.SampleUser;

public class GetSampleUserQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetSampleUserQuery, UserDto>
{
    public Task<UserDto> Handle(GetSampleUserQuery request, CancellationToken cancellationToken)
    {
        var user = unitOfWork.UserRepository.GetSample(request.Name!);

        var userDto = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber
        };

        return Task.FromResult(userDto);
    }
}
