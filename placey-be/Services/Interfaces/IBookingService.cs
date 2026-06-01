using placey_be.Models.DTOs.Bookings;
using placey_be.Models.Entities;

namespace placey_be.Services.Interfaces;

public interface IBookingService
{
    Task<BookingResponse> CreateAsync(Guid guestId, CreateBookingRequest request);
    Task<BookingResponse> GetByIdAsync(Guid bookingId, Guid requesterId);
    Task CancelAsync(Guid bookingId, Guid requesterId);
    Task<BookingResponse> UpdateStatusAsync(Guid bookingId, Guid requesterId, BookingStatus status);
}
