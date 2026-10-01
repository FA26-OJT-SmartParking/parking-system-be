namespace Identity.Application.DTOs;

public class UserDto
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public UserDto()
    {
    }

    public UserDto(Guid id, string? name, string? email, string? phoneNumber)
    {
        Id = id;
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
    }
}
