using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Usecase.SampleUser;

public class GetSampleUserQuery : IRequest<UserDto>
{
    public string? Name { get; set; }

    public string? Password { get; set; }

    public string? PhoneNumber { get; set; }

    public GetSampleUserQuery()
    {
    }

    public GetSampleUserQuery(string? name)
    {
        Name = name;
    }

    public GetSampleUserQuery(string? name, string? password, string? phoneNumber)
    {
        Name = name;
        Password = password;
        PhoneNumber = phoneNumber;
    }
}
