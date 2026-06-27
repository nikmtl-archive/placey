using System.ComponentModel.DataAnnotations;
using placey_be.Models.Entities;

namespace placey_be.Models.DTOs.Listings;

public record CreateListingRequest(
    [Required] string Title,
    [Required] string Description,
    [Required] string City,
    [Required] string Country,
    [Required, Range(1, 100000)] decimal PricePerNight,
    [Required, Range(1, 50)] int MaxGuests,
    [Required, Range(0, 20)] int Bedrooms,
    [Required, Range(0, 20)] int Bathrooms,
    [Required] ListingCategory Category,
    [Required] PropertyType PropertyType,
    string? Street = null,
    bool IsInstantBook = false,
    List<string>? Amenities = null
);

public record UpdateListingRequest(
    string? Title,
    string? Description,
    string? City,
    string? Country,
    string? Street,
    [Range(1, 100000)] decimal? PricePerNight,
    [Range(1, 50)] int? MaxGuests,
    [Range(0, 20)] int? Bedrooms,
    [Range(0, 20)] int? Bathrooms,
    bool? IsInstantBook,
    List<string>? Amenities,
    ListingCategory? Category,
    PropertyType? PropertyType
);

public record ListingResponse(
    Guid Id,
    Guid HostId,
    string HostName,
    string? HostAvatarUrl,
    string Title,
    string Description,
    string City,
    string Country,
    string? Street,
    string Location,
    decimal PricePerNight,
    int Bedrooms,
    int Bathrooms,
    int MaxGuests,
    bool IsInstantBook,
    List<string> Amenities,
    ListingCategory Category,
    PropertyType PropertyType,
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
    ListingCategory? Category = null,
    PropertyType? PropertyType = null,
    int? MinBedrooms = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int Page = 1,
    int Limit = 20
);

public record ListingPhotoResponse(Guid Id, string Url, int Order);

public record AddBlockingRequest([Required] DateOnly StartDate, [Required] DateOnly EndDate);

public record BlockingResponse(Guid Id, DateOnly StartDate, DateOnly EndDate);
