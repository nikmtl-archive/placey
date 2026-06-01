using placey_be.Models.DTOs.Reviews;

namespace placey_be.Services.Interfaces;

public interface IReviewService
{
    Task<ReviewResponse> CreateAsync(Guid authorId, CreateReviewRequest request);
    Task DeleteAsync(Guid reviewId, Guid requesterId);
    Task<List<ReviewResponse>> GetByListingAsync(Guid listingId);
    Task<List<ReviewResponse>> GetByUserAsync(Guid userId);
}
