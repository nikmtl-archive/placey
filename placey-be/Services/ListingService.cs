using Microsoft.EntityFrameworkCore;
using placey_be.Data;
using placey_be.Models.DTOs.Listings;
using placey_be.Models.Entities;
using placey_be.Services.Interfaces;

namespace placey_be.Services;

public class ListingService(AppDbContext db) : IListingService
{
    public async Task<(List<ListingResponse> Items, int Total)> SearchAsync(ListingSearchParams p)
    {
        var query = db.Listings
            .Include(l => l.Host)
            .Include(l => l.Photos)
            .Include(l => l.Reviews)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Location))
            query = query.Where(l => l.Location.ToLower().Contains(p.Location.ToLower()));

        if (p.Guests.HasValue)
            query = query.Where(l => l.MaxGuests >= p.Guests.Value);

        if (p.CheckIn.HasValue && p.CheckOut.HasValue)
        {
            var checkIn = p.CheckIn.Value;
            var checkOut = p.CheckOut.Value;
            query = query.Where(l => !l.Bookings.Any(b =>
                b.Status != BookingStatus.Cancelled &&
                b.CheckIn < checkOut &&
                b.CheckOut > checkIn));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((p.Page - 1) * p.Limit)
            .Take(p.Limit)
            .ToListAsync();

        return (items.Select(ToResponse).ToList(), total);
    }

    public async Task<ListingResponse> GetByIdAsync(Guid id)
    {
        var listing = await db.Listings
            .Include(l => l.Host)
            .Include(l => l.Photos)
            .Include(l => l.Reviews)
            .FirstOrDefaultAsync(l => l.Id == id)
            ?? throw new KeyNotFoundException("Listing not found.");

        return ToResponse(listing);
    }

    public async Task<ListingResponse> CreateAsync(Guid hostId, CreateListingRequest request)
    {
        var listing = new Listing
        {
            HostId = hostId,
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            PricePerNight = request.PricePerNight,
            MaxGuests = request.MaxGuests,
            IsInstantBook = request.IsInstantBook
        };

        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        return await GetByIdAsync(listing.Id);
    }

    public async Task<ListingResponse> UpdateAsync(Guid listingId, Guid requesterId, UpdateListingRequest request)
    {
        var listing = await db.Listings.FindAsync(listingId)
            ?? throw new KeyNotFoundException("Listing not found.");

        if (listing.HostId != requesterId)
            throw new UnauthorizedAccessException("Only the host can update this listing.");

        if (request.Title is not null) listing.Title = request.Title;
        if (request.Description is not null) listing.Description = request.Description;
        if (request.Location is not null) listing.Location = request.Location;
        if (request.PricePerNight.HasValue) listing.PricePerNight = request.PricePerNight.Value;
        if (request.MaxGuests.HasValue) listing.MaxGuests = request.MaxGuests.Value;
        if (request.IsInstantBook.HasValue) listing.IsInstantBook = request.IsInstantBook.Value;

        await db.SaveChangesAsync();
        return await GetByIdAsync(listingId);
    }

    public async Task DeleteAsync(Guid listingId, Guid requesterId)
    {
        var listing = await db.Listings.FindAsync(listingId)
            ?? throw new KeyNotFoundException("Listing not found.");

        if (listing.HostId != requesterId)
            throw new UnauthorizedAccessException("Only the host can delete this listing.");

        db.Listings.Remove(listing);
        await db.SaveChangesAsync();
    }

    public async Task<List<AvailabilityResponse>> GetAvailabilityAsync(Guid listingId)
    {
        return await db.Availabilities
            .Where(a => a.ListingId == listingId)
            .OrderBy(a => a.StartDate)
            .Select(a => new AvailabilityResponse(a.Id, a.StartDate, a.EndDate, a.IsBlocked))
            .ToListAsync();
    }

    public async Task<AvailabilityResponse> SetAvailabilityAsync(Guid listingId, Guid requesterId, SetAvailabilityRequest request)
    {
        var listing = await db.Listings.FindAsync(listingId)
            ?? throw new KeyNotFoundException("Listing not found.");

        if (listing.HostId != requesterId)
            throw new UnauthorizedAccessException("Only the host can set availability.");

        var entry = new Availability
        {
            ListingId = listingId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsBlocked = request.IsBlocked
        };

        db.Availabilities.Add(entry);
        await db.SaveChangesAsync();

        return new AvailabilityResponse(entry.Id, entry.StartDate, entry.EndDate, entry.IsBlocked);
    }

    private static ListingResponse ToResponse(Listing l) => new(
        l.Id,
        l.HostId,
        l.Host.Name,
        l.Host.AvatarUrl,
        l.Title,
        l.Description,
        l.Location,
        l.PricePerNight,
        l.MaxGuests,
        l.IsInstantBook,
        l.Photos.OrderBy(p => p.Order).Select(p => p.Url).ToList(),
        l.Reviews.Count > 0 ? l.Reviews.Average(r => r.Rating) : null,
        l.Reviews.Count,
        l.CreatedAt
    );
}
