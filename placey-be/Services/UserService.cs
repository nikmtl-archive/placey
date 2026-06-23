using Microsoft.EntityFrameworkCore;
using placey_be.Data;
using placey_be.Models.DTOs.Users;
using placey_be.Services.Interfaces;

namespace placey_be.Services;

public class UserService(AppDbContext db, IWebHostEnvironment env) : IUserService
{
    public async Task<PrivateUserResponse> GetMeAsync(Guid userId)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        return new PrivateUserResponse(user.Id, user.Email, user.Name, user.Bio, user.AvatarUrl, user.CreatedAt);
    }

    public async Task<PrivateUserResponse> UpdateMeAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (request.Name is not null) user.Name = request.Name;
        user.Bio = request.Bio;

        await db.SaveChangesAsync();
        return new PrivateUserResponse(user.Id, user.Email, user.Name, user.Bio, user.AvatarUrl, user.CreatedAt);
    }

    public async Task DeleteMeAsync(Guid userId)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        db.Users.Remove(user);
        await db.SaveChangesAsync();
    }

    public async Task<PublicUserResponse> GetPublicAsync(Guid userId)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        return new PublicUserResponse(user.Id, user.Name, user.Bio, user.AvatarUrl, user.CreatedAt);
    }

    public async Task<string> UploadAvatarAsync(Guid userId, IFormFile file)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var uploads = Path.Combine(env.WebRootPath ?? "wwwroot", "avatars");
        Directory.CreateDirectory(uploads);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var filename = $"{userId}{ext}";
        var fullPath = Path.Combine(uploads, filename);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        user.AvatarUrl = $"/avatars/{filename}";
        await db.SaveChangesAsync();

        return user.AvatarUrl;
    }
}
