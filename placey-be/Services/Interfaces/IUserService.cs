using placey_be.Models.DTOs.Users;

namespace placey_be.Services.Interfaces;

public interface IUserService
{
    Task<PrivateUserResponse> GetMeAsync(Guid userId);
    Task<PrivateUserResponse> UpdateMeAsync(Guid userId, UpdateProfileRequest request);
    Task DeleteMeAsync(Guid userId);
    Task<PublicUserResponse> GetPublicAsync(Guid userId);
    Task<string> UploadAvatarAsync(Guid userId, IFormFile file);
}
