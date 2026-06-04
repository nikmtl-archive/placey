using placey_be.Models.DTOs.Listings;

namespace placey_be.Services.Interfaces;

public interface IListingService
{
    Task<(List<ListingResponse> Items, int Total)> SearchAsync(ListingSearchParams p);
    Task<ListingResponse> GetByIdAsync(Guid id);
    Task<ListingResponse> CreateAsync(Guid hostId, CreateListingRequest request);
    Task<ListingResponse> UpdateAsync(Guid listingId, Guid requesterId, UpdateListingRequest request);
    Task DeleteAsync(Guid listingId, Guid requesterId);
    Task<List<AvailabilityResponse>> GetAvailabilityAsync(Guid listingId);
    Task<AvailabilityResponse> SetAvailabilityAsync(Guid listingId, Guid requesterId, SetAvailabilityRequest request);
    Task<ListingPhotoResponse> UploadPhotoAsync(Guid listingId, Guid requesterId, IFormFile file);
    Task DeletePhotoAsync(Guid listingId, Guid photoId, Guid requesterId);
}
