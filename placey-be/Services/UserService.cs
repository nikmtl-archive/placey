using Microsoft.EntityFrameworkCore;
using placey_be.Data;
using placey_be.Models.DTOs.Users;
using placey_be.Models.Entities;
using placey_be.Services.Interfaces;

namespace placey_be.Services;

public class UserService(AppDbContext db, IWebHostEnvironment env) : IUserService
{
    public async Task<PrivateUserResponse> GetMeAsync(Guid userId)
    {
        var user =
            await db.Users.FindAsync(userId) ?? throw new KeyNotFoundException("User not found.");

        if (user.IsDeleted)
            throw new KeyNotFoundException("User not found.");

        return new PrivateUserResponse(
            user.Id,
            user.Email,
            user.Name,
            user.Bio,
            user.AvatarUrl,
            user.CreatedAt
        );
    }

    public async Task<PrivateUserResponse> UpdateMeAsync(Guid userId, UpdateProfileRequest request)
    {
        var user =
            await db.Users.FindAsync(userId) ?? throw new KeyNotFoundException("User not found.");

        if (request.Name is not null)
        {
            if (request.Name.Trim().Length == 0)
                throw new InvalidOperationException("Name cannot be blank.");
            user.Name = request.Name.Trim();
        }
        if (request.Bio is not null)
            user.Bio = request.Bio;

        await db.SaveChangesAsync();
        return new PrivateUserResponse(
            user.Id,
            user.Email,
            user.Name,
            user.Bio,
            user.AvatarUrl,
            user.CreatedAt
        );
    }

    public async Task DeleteMeAsync(Guid userId)
    {
        var user =
            await db.Users.FindAsync(userId) ?? throw new KeyNotFoundException("User not found.");

        // Wipe PII and mark as deleted — row is kept so FK references (bookings, reviews) remain valid for other users
        user.IsDeleted = true;
        user.Email = $"deleted_{userId}@deleted";
        user.PasswordHash = string.Empty;
        user.Name = "Deleted User";
        user.Bio = null;
        user.AvatarUrl = null;

        // Cancel active bookings on the user's listings and soft-delete the listings
        var listings = await db
            .Listings.Where(l => l.HostId == userId && !l.IsDeleted)
            .ToListAsync();
        var listingIds = listings.Select(l => l.Id).ToList();
        var activeBookingsOnListings = await db
            .Bookings.Where(b =>
                listingIds.Contains(b.ListingId)
                && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
            )
            .ToListAsync();
        foreach (var booking in activeBookingsOnListings)
            booking.Status = BookingStatus.Cancelled;
        foreach (var listing in listings)
            listing.IsDeleted = true;

        // Cancel active bookings made by this user as a guest
        var activeGuestBookings = await db
            .Bookings.Where(b =>
                b.GuestId == userId
                && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
            )
            .ToListAsync();
        foreach (var booking in activeGuestBookings)
            booking.Status = BookingStatus.Cancelled;

        // Revoke all refresh tokens so existing sessions stop working
        var tokens = await db
            .RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();
        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;

        // Remove personal favourites and conversations (private data with no value to others)
        var favorites = await db.Favorites.Where(f => f.UserId == userId).ToListAsync();
        db.Favorites.RemoveRange(favorites);

        var conversationIds = await db
            .Conversations.Where(c => c.User1Id == userId || c.User2Id == userId)
            .Select(c => c.Id)
            .ToListAsync();
        var conversationMessages = await db
            .Messages.Where(m => conversationIds.Contains(m.ConversationId))
            .ToListAsync();
        db.Messages.RemoveRange(conversationMessages);
        var conversations = await db
            .Conversations.Where(c => conversationIds.Contains(c.Id))
            .ToListAsync();
        db.Conversations.RemoveRange(conversations);

        await db.SaveChangesAsync();
    }

    public async Task<PublicUserResponse> GetPublicAsync(Guid userId)
    {
        var user =
            await db.Users.FindAsync(userId) ?? throw new KeyNotFoundException("User not found.");

        return new PublicUserResponse(user.Id, user.Name, user.Bio, user.AvatarUrl, user.CreatedAt);
    }

    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/heic", "image/heif"];
    private const long MaxAvatarBytes = 5 * 1024 * 1024;

    public async Task<string> UploadAvatarAsync(Guid userId, IFormFile file)
    {
        var user =
            await db.Users.FindAsync(userId) ?? throw new KeyNotFoundException("User not found.");

        if (file == null || file.Length == 0)
            throw new InvalidOperationException("No file provided.");
        if (file.Length > MaxAvatarBytes)
            throw new InvalidOperationException("File exceeds the 5 MB limit.");
        if (!AllowedImageTypes.Contains(file.ContentType.ToLowerInvariant()))
            throw new InvalidOperationException("Only JPEG, PNG, WebP, and HEIC images are allowed.");

        var uploads = Path.Combine(env.WebRootPath ?? "wwwroot", "avatars");
        Directory.CreateDirectory(uploads);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var filename = $"{userId}{ext}";
        var fullPath = Path.Combine(uploads, filename);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        user.AvatarUrl = $"/api/avatars/{filename}";
        await db.SaveChangesAsync();

        return user.AvatarUrl;
    }
}
