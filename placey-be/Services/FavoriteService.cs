using Microsoft.EntityFrameworkCore;
using placey_be.Data;
using placey_be.Models.DTOs.Listings;
using placey_be.Models.Entities;
using placey_be.Services.Interfaces;

namespace placey_be.Services;

public class FavoriteService(AppDbContext db) : IFavoriteService
{
    public async Task<List<ListingResponse>> GetFavoritesAsync(Guid userId)
    {
        return await db
            .Favorites.Where(f => f.UserId == userId && !f.Listing.IsDeleted)
            .Include(f => f.Listing)
                .ThenInclude(l => l.Host)
            .Include(f => f.Listing)
                .ThenInclude(l => l.Photos)
            .Include(f => f.Listing)
                .ThenInclude(l => l.Reviews)
            .Select(f => ToListingResponse(f.Listing))
            .ToListAsync();
    }

    public async Task AddAsync(Guid userId, Guid listingId)
    {
        if (!await db.Listings.AnyAsync(l => l.Id == listingId && !l.IsDeleted))
            throw new KeyNotFoundException("Listing not found.");

        if (await db.Favorites.AnyAsync(f => f.UserId == userId && f.ListingId == listingId))
            return;

        db.Favorites.Add(new Favorite { UserId = userId, ListingId = listingId });
        await db.SaveChangesAsync();
    }

    public async Task RemoveAsync(Guid userId, Guid listingId)
    {
        var fav = await db.Favorites.FirstOrDefaultAsync(f =>
            f.UserId == userId && f.ListingId == listingId
        );
        if (fav is null)
            return;

        db.Favorites.Remove(fav);
        await db.SaveChangesAsync();
    }

    private static ListingResponse ToListingResponse(Listing l) =>
        new(
            l.Id,
            l.HostId,
            l.Host.Name,
            l.Host.AvatarUrl,
            l.Title,
            l.Description,
            l.City,
            l.Country,
            string.IsNullOrEmpty(l.Street) ? null : l.Street,
            $"{l.City}, {l.Country}",
            l.PricePerNight,
            l.Bedrooms,
            l.Bathrooms,
            l.MaxGuests,
            l.IsInstantBook,
            l.Amenities,
            l.Category,
            l.PropertyType,
            l.Photos.OrderBy(p => p.Order).Select(p => p.Url).ToList(),
            l.Reviews.Count > 0 ? l.Reviews.Average(r => r.Rating) : null,
            l.Reviews.Count,
            l.CreatedAt
        );
}
