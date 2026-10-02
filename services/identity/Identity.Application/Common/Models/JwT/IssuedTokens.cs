namespace Identity.Application.Common.Models.JwT;

public record AccessToken(string Value, DateTimeOffset ExpiresOn);

public record RefreshToken(string Value, DateTimeOffset ExpiresOn);
