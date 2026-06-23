using Microsoft.EntityFrameworkCore;
using placey_be.Data;
using placey_be.Models.DTOs.Listings;
using placey_be.Models.Entities;
using placey_be.Services.Interfaces;

namespace placey_be.Services;

public class ListingService(AppDbContext db, IWebHostEnvironment? env = null) : IListingService
{
    public async Task<(List<ListingResponse> Items, int Total)> SearchAsync(ListingSearchParams p)
    {
        var query = db.Listings
            .Include(l => l.Host)
            .Include(l => l.Photos)
            .Include(l => l.Reviews)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.Location))
        {
            var loc = p.Location.ToLower();
            query = query.Where(l =>
                l.City.ToLower().Contains(loc) ||
                l.Country.ToLower().Contains(loc) ||
                l.Street.ToLower().Contains(loc));
        }

        if (p.Guests.HasValue)
            query = query.Where(l => l.MaxGuests >= p.Guests.Value);

        if (p.Category.HasValue)
            query = query.Where(l => l.Category == p.Category.Value);

        if (p.PropertyType.HasValue)
            query = query.Where(l => l.PropertyType == p.PropertyType.Value);

        if (p.MinBedrooms.HasValue)
            query = query.Where(l => l.Bedrooms >= p.MinBedrooms.Value);

        if (p.MinPrice.HasValue)
            query = query.Where(l => l.PricePerNight >= p.MinPrice.Value);

        if (p.MaxPrice.HasValue)
            query = query.Where(l => l.PricePerNight <= p.MaxPrice.Value);

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
            Street = request.Street ?? string.Empty,
            City = request.City,
            Country = request.Country,
            Bedrooms = request.Bedrooms,
            Bathrooms = request.Bathrooms,
            PricePerNight = request.PricePerNight,
            MaxGuests = request.MaxGuests,
            IsInstantBook = request.IsInstantBook,
            Amenities = request.Amenities ?? new List<string>(),
            Category = request.Category,
            PropertyType = request.PropertyType
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
        if (request.City is not null) listing.City = request.City;
        if (request.Country is not null) listing.Country = request.Country;
        if (request.Street is not null) listing.Street = request.Street;
        if (request.PricePerNight.HasValue) listing.PricePerNight = request.PricePerNight.Value;
        if (request.MaxGuests.HasValue) listing.MaxGuests = request.MaxGuests.Value;
        if (request.Bedrooms.HasValue) listing.Bedrooms = request.Bedrooms.Value;
        if (request.Bathrooms.HasValue) listing.Bathrooms = request.Bathrooms.Value;
        if (request.IsInstantBook.HasValue) listing.IsInstantBook = request.IsInstantBook.Value;
        if (request.Amenities is not null) listing.Amenities = request.Amenities;
        if (request.Category.HasValue) listing.Category = request.Category.Value;
        if (request.PropertyType.HasValue) listing.PropertyType = request.PropertyType.Value;

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

    public async Task<List<ListingPhotoResponse>> GetPhotosAsync(Guid listingId, Guid requesterId)
    {
        var listing = await db.Listings.FindAsync(listingId)
            ?? throw new KeyNotFoundException("Listing not found.");

        if (listing.HostId != requesterId)
            throw new UnauthorizedAccessException("Only the host can view photo details.");

        return await db.ListingPhotos
            .Where(p => p.ListingId == listingId)
            .OrderBy(p => p.Order)
            .Select(p => new ListingPhotoResponse(p.Id, p.Url, p.Order))
            .ToListAsync();
    }

    public async Task<ListingPhotoResponse> UploadPhotoAsync(Guid listingId, Guid requesterId, IFormFile file)
    {
        var listing = await db.Listings.Include(l => l.Photos).FirstOrDefaultAsync(l => l.Id == listingId)
            ?? throw new KeyNotFoundException("Listing not found.");

        if (listing.HostId != requesterId)
            throw new UnauthorizedAccessException("Only the host can upload photos.");

        var webRoot = env?.WebRootPath ?? "wwwroot";
        var uploads = Path.Combine(webRoot, "listing-photos");
        Directory.CreateDirectory(uploads);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var filename = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploads, filename);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        var order = listing.Photos.Count > 0 ? listing.Photos.Max(p => p.Order) + 1 : 0;
        var photo = new ListingPhoto
        {
            ListingId = listingId,
            Url = $"/listing-photos/{filename}",
            Order = order
        };

        db.ListingPhotos.Add(photo);
        await db.SaveChangesAsync();

        return new ListingPhotoResponse(photo.Id, photo.Url, photo.Order);
    }

    public async Task DeletePhotoAsync(Guid listingId, Guid photoId, Guid requesterId)
    {
        var listing = await db.Listings.FindAsync(listingId)
            ?? throw new KeyNotFoundException("Listing not found.");

        if (listing.HostId != requesterId)
            throw new UnauthorizedAccessException("Only the host can delete photos.");

        var photo = await db.ListingPhotos.FirstOrDefaultAsync(p => p.Id == photoId && p.ListingId == listingId)
            ?? throw new KeyNotFoundException("Photo not found.");

        var webRoot = env?.WebRootPath ?? "wwwroot";
        var physicalPath = Path.Combine(webRoot, photo.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(physicalPath))
            File.Delete(physicalPath);

        db.ListingPhotos.Remove(photo);
        await db.SaveChangesAsync();
    }

    private static ListingResponse ToResponse(Listing l) => new(
        l.Id,
        l.HostId,
        l.Host.Name,
        l.Host.AvatarUrl,
        l.Title,
        l.Description,
        l.City,
        l.Country,
        string.IsNullOrEmpty(l.Street) ? null : l.Street,
        $"{l.City}, {l.Country}",
        l.PricePerNight,
        l.Bedrooms,
        l.Bathrooms,
        l.MaxGuests,
        l.IsInstantBook,
        l.Amenities,
        l.Category,
        l.PropertyType,
        l.Photos.OrderBy(p => p.Order).Select(p => p.Url).ToList(),
        l.Reviews.Count > 0 ? l.Reviews.Average(r => r.Rating) : null,
        l.Reviews.Count,
        l.CreatedAt
    );
}
