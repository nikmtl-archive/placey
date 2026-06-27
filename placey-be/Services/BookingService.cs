using Microsoft.EntityFrameworkCore;
using placey_be.Data;
using placey_be.Models.DTOs.Bookings;
using placey_be.Models.Entities;
using placey_be.Services.Interfaces;

namespace placey_be.Services;

public class BookingService(AppDbContext db) : IBookingService
{
    public async Task<BookingResponse> CreateAsync(Guid guestId, CreateBookingRequest request)
    {
        var listing = await db.Listings.FindAsync(request.ListingId)
            ?? throw new KeyNotFoundException("Listing not found.");

        if (listing.HostId == guestId)
            throw new InvalidOperationException("Hosts cannot book their own listing.");

        if (request.CheckIn >= request.CheckOut)
            throw new InvalidOperationException("Check-in must be before check-out.");

        var hasConflict = await db.Bookings.AnyAsync(b =>
            b.ListingId == request.ListingId &&
            b.Status != BookingStatus.Cancelled &&
            b.CheckIn < request.CheckOut &&
            b.CheckOut > request.CheckIn);

        if (hasConflict)
            throw new InvalidOperationException("Listing is not available for the selected dates.");

        var isBlocked = await db.ListingBlocks.AnyAsync(b =>
            b.ListingId == request.ListingId &&
            b.StartDate < request.CheckOut &&
            b.EndDate > request.CheckIn);

        if (isBlocked)
            throw new InvalidOperationException("Listing is not available for the selected dates.");

        var nights = request.CheckOut.DayNumber - request.CheckIn.DayNumber;
        var booking = new Booking
        {
            ListingId = request.ListingId,
            GuestId = guestId,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            Guests = request.Guests,
            TotalPrice = listing.PricePerNight * nights,
            Status = listing.IsInstantBook ? BookingStatus.Confirmed : BookingStatus.Pending
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        return await LoadResponseAsync(booking.Id);
    }

    public async Task<BookingResponse> GetByIdAsync(Guid bookingId, Guid requesterId)
    {
        var booking = await db.Bookings
            .Include(b => b.Listing).ThenInclude(l => l.Photos)
            .Include(b => b.Guest)
            .FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.GuestId != requesterId && booking.Listing.HostId != requesterId)
            throw new UnauthorizedAccessException("Access denied.");

        return ToResponse(booking);
    }

    public async Task CancelAsync(Guid bookingId, Guid requesterId)
    {
        var booking = await db.Bookings
            .Include(b => b.Listing)
            .FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.GuestId != requesterId && booking.Listing.HostId != requesterId)
            throw new UnauthorizedAccessException("Access denied.");

        if (booking.Status == BookingStatus.Completed)
            throw new InvalidOperationException("Completed bookings cannot be cancelled.");

        booking.Status = BookingStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    public async Task<BookingResponse> UpdateStatusAsync(Guid bookingId, Guid requesterId, BookingStatus status)
    {
        var booking = await db.Bookings
            .Include(b => b.Listing).ThenInclude(l => l.Photos)
            .Include(b => b.Guest)
            .FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.Listing.HostId != requesterId)
            throw new UnauthorizedAccessException("Only the host can update booking status.");

        booking.Status = status;
        await db.SaveChangesAsync();

        return ToResponse(booking);
    }

    private async Task<BookingResponse> LoadResponseAsync(Guid bookingId)
    {
        var booking = await db.Bookings
            .Include(b => b.Listing).ThenInclude(l => l.Photos)
            .Include(b => b.Guest)
            .FirstAsync(b => b.Id == bookingId);
        return ToResponse(booking);
    }

    private static BookingResponse ToResponse(Booking b) => new(
        b.Id,
        b.ListingId,
        b.Listing.Title,
        b.Listing.Photos.OrderBy(p => p.Order).Select(p => p.Url).FirstOrDefault(),
        b.GuestId,
        b.Guest.Name,
        b.Guest.AvatarUrl,
        b.CheckIn,
        b.CheckOut,
        b.Guests,
        b.TotalPrice,
        b.Status,
        b.CreatedAt,
        null
    );
}
