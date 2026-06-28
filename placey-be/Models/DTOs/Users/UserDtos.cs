using System.ComponentModel.DataAnnotations;

namespace placey_be.Models.DTOs.Users;

public record UpdateProfileRequest([MaxLength(100)] string? Name, [MaxLength(500)] string? Bio);

public record PublicUserResponse(
    Guid Id,
    string Name,
    string? Bio,
    string? AvatarUrl,
    DateTime CreatedAt
);

public record PrivateUserResponse(
    Guid Id,
    string Email,
    string Name,
    string? Bio,
    string? AvatarUrl,
    DateTime CreatedAt
);
