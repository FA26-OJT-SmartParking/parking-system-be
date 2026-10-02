namespace Identity.Application.DTOs;

/// <summary>The "result" of a successful login.</summary>
public class UserSessionDto
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;
}
