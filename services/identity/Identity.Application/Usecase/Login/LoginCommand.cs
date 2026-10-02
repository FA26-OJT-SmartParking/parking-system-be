using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Usecase.Login;

/// <summary>A class, not a record: a record's ToString would print the password.</summary>
public class LoginCommand : IRequest<UserSessionDto>
{
    public string? UserName { get; set; }

    public string? Password { get; set; }
}
