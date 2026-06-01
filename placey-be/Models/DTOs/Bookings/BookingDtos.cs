using System.ComponentModel.DataAnnotations;
using placey_be.Models.Entities;

namespace placey_be.Models.DTOs.Bookings;

public record CreateBookingRequest(
    [Required] Guid ListingId,
    [Required] DateOnly CheckIn,
    [Required] DateOnly CheckOut,
    [Required, Range(1, 50)] int Guests
);

public record UpdateBookingStatusRequest(
    [Required] BookingStatus Status
);

public record BookingResponse(
    Guid Id,
    Guid ListingId,
    string ListingTitle,
    Guid GuestId,
    string GuestName,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Guests,
    decimal TotalPrice,
    BookingStatus Status,
    DateTime CreatedAt
);
