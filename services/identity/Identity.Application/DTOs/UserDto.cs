namespace Identity.Application.DTOs;

/// <summary>User data returned by the API.</summary>
public record UserDto(Guid Id, string? Name, string? Email, string? PhoneNumber);
