using Identity.Application.DTOs;
using Identity.Application.Usecase.Login;

namespace Identity.Application.Common.Mappers;

/// <summary>Mapping is written by hand (no AutoMapper).</summary>
public static class LoginMapper
{
    public static LoginCommand ToCommand(this LoginDto dto) => new()
    {
        UserName = dto.UserName,
        Password = dto.Password,
    };
}
