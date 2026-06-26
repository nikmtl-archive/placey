using placey_be.Models.DTOs.Listings;

namespace placey_be.Services.Interfaces;

public interface IListingService
{
    Task<(List<ListingResponse> Items, int Total)> SearchAsync(ListingSearchParams p);
    Task<ListingResponse> GetByIdAsync(Guid id);
    Task<ListingResponse> CreateAsync(Guid hostId, CreateListingRequest request);
    Task<ListingResponse> UpdateAsync(Guid listingId, Guid requesterId, UpdateListingRequest request);
    Task DeleteAsync(Guid listingId, Guid requesterId);
    Task<List<BlockingResponse>> GetBlockingsAsync(Guid listingId);
    Task<BlockingResponse> AddBlockingAsync(Guid listingId, Guid requesterId, AddBlockingRequest request);
    Task DeleteBlockingAsync(Guid listingId, Guid blockingId, Guid requesterId);
    Task<List<ListingPhotoResponse>> GetPhotosAsync(Guid listingId, Guid requesterId);
    Task<ListingPhotoResponse> UploadPhotoAsync(Guid listingId, Guid requesterId, IFormFile file);
    Task DeletePhotoAsync(Guid listingId, Guid photoId, Guid requesterId);
}
