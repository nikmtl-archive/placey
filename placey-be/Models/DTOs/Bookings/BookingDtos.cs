using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using placey_be.Models.DTOs.Reviews;
using placey_be.Models.Entities;

namespace placey_be.Models.DTOs.Bookings;

public record CreateBookingRequest(
    [Required] Guid ListingId,
    [Required] DateOnly CheckIn,
    [Required] DateOnly CheckOut,
    [Required, Range(1, 50)] int Guests
);

public record UpdateBookingStatusRequest([Required] BookingStatus Status);

public record BookingResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("listingId")] Guid ListingId,
    [property: JsonPropertyName("listingTitle")] string ListingTitle,
    [property: JsonPropertyName("listingPhotoUrl")] string? ListingPhotoUrl,
    [property: JsonPropertyName("guestId")] Guid GuestId,
    [property: JsonPropertyName("guestName")] string GuestName,
    [property: JsonPropertyName("guestAvatarUrl")] string? GuestAvatarUrl,
    [property: JsonPropertyName("checkIn")] DateOnly CheckIn,
    [property: JsonPropertyName("checkOut")] DateOnly CheckOut,
    [property: JsonPropertyName("guests")] int Guests,
    [property: JsonPropertyName("totalPrice")] decimal TotalPrice,
    [property: JsonPropertyName("status")] BookingStatus Status,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("review")] ReviewResponse? Review
);
