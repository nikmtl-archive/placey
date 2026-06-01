using System.ComponentModel.DataAnnotations;

namespace placey_be.Models.DTOs.Reviews;

public record CreateReviewRequest(
    [Required] Guid BookingId,
    [Required, Range(1, 5)] int Rating,
    [Required] string Comment
);

public record ReviewResponse(
    Guid Id,
    Guid ListingId,
    Guid AuthorId,
    string AuthorName,
    string? AuthorAvatarUrl,
    int Rating,
    string Comment,
    DateTime CreatedAt
);
