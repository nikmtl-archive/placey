using placey_be.Models.DTOs.Listings;

namespace placey_be.Services.Interfaces;

public interface IFavoriteService
{
    Task<List<ListingResponse>> GetFavoritesAsync(Guid userId);
    Task AddAsync(Guid userId, Guid listingId);
    Task RemoveAsync(Guid userId, Guid listingId);
}
