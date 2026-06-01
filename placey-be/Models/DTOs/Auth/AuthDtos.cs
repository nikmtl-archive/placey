using System.ComponentModel.DataAnnotations;

namespace placey_be.Models.DTOs.Auth;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required] string Name
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record RefreshRequest(
    [Required] string RefreshToken
);

public record ForgotPasswordRequest(
    [Required, EmailAddress] string Email
);

public record ResetPasswordRequest(
    [Required] string Token,
    [Required, MinLength(8)] string NewPassword
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User
);

public record UserDto(
    Guid Id,
    string Email,
    string Name,
    string? Bio,
    string? AvatarUrl
);
