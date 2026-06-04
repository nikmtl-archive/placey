using System.ComponentModel.DataAnnotations;

namespace placey_be.Models.DTOs.Listings;

public record CreateListingRequest(
    [Required] string Title,
    [Required] string Description,
    [Required] string Location,
    [Required, Range(1, 100000)] decimal PricePerNight,
    [Required, Range(1, 50)] int MaxGuests,
    bool IsInstantBook = false
);

public record UpdateListingRequest(
    string? Title,
    string? Description,
    string? Location,
    [Range(1, 100000)] decimal? PricePerNight,
    [Range(1, 50)] int? MaxGuests,
    bool? IsInstantBook
);

public record ListingResponse(
    Guid Id,
    Guid HostId,
    string HostName,
    string? HostAvatarUrl,
    string Title,
    string Description,
    string Location,
    decimal PricePerNight,
    int MaxGuests,
    bool IsInstantBook,
    List<string> PhotoUrls,
    double? AverageRating,
    int ReviewCount,
    DateTime CreatedAt
);

public record ListingSearchParams(
    string? Location,
    DateOnly? CheckIn,
    DateOnly? CheckOut,
    int? Guests,
    int Page = 1,
    int Limit = 20
);

public record ListingPhotoResponse(
    Guid Id,
    string Url,
    int Order
);

public record SetAvailabilityRequest(
    [Required] DateOnly StartDate,
    [Required] DateOnly EndDate,
    [Required] bool IsBlocked
);

public record AvailabilityResponse(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsBlocked
);
