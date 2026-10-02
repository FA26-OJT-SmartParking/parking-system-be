namespace Identity.Application.DTOs;

/// <summary>Request body of POST /api/auth/login. Properties are nullable so a missing field is reported by the validator, not by model binding.</summary>
public class LoginDto
{
    public string? UserName { get; set; }

    public string? Password { get; set; }
}
