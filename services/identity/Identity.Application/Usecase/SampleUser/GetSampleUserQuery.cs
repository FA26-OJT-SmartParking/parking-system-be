using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Usecase.SampleUser;

/// <summary>Asks for the sample user under the given name.</summary>
public record GetSampleUserQuery(string? Name) : IRequest<UserDto>;
