using Microsoft.EntityFrameworkCore;
using placey_be.Data;
using placey_be.Models.DTOs.Reviews;
using placey_be.Models.Entities;
using placey_be.Services.Interfaces;

namespace placey_be.Services;

public class ReviewService(AppDbContext db) : IReviewService
{
    public async Task<ReviewResponse> CreateAsync(Guid authorId, CreateReviewRequest request)
    {
        var booking = await db.Bookings
            .Include(b => b.Listing)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.GuestId != authorId)
            throw new UnauthorizedAccessException("You can only review your own bookings.");

        if (booking.Status != BookingStatus.Completed)
            throw new InvalidOperationException("You can only review completed bookings.");

        if (await db.Reviews.AnyAsync(r => r.BookingId == request.BookingId))
            throw new InvalidOperationException("A review for this booking already exists.");

        var review = new Review
        {
            ListingId = booking.ListingId,
            BookingId = request.BookingId,
            AuthorId = authorId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        await db.Entry(review).Reference(r => r.Author).LoadAsync();
        return ToResponse(review);
    }

    public async Task DeleteAsync(Guid reviewId, Guid requesterId)
    {
        var review = await db.Reviews.FindAsync(reviewId)
            ?? throw new KeyNotFoundException("Review not found.");

        if (review.AuthorId != requesterId)
            throw new UnauthorizedAccessException("You can only delete your own reviews.");

        db.Reviews.Remove(review);
        await db.SaveChangesAsync();
    }

    public async Task<List<ReviewResponse>> GetByListingAsync(Guid listingId)
    {
        return await db.Reviews
            .Include(r => r.Author)
            .Where(r => r.ListingId == listingId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToResponse(r))
            .ToListAsync();
    }

    public async Task<List<ReviewResponse>> GetByUserAsync(Guid userId)
    {
        return await db.Reviews
            .Include(r => r.Author)
            .Include(r => r.Listing)
            .Where(r => r.Listing.HostId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToResponse(r))
            .ToListAsync();
    }

    private static ReviewResponse ToResponse(Review r) => new(
        r.Id,
        r.ListingId,
        r.AuthorId,
        r.Author.Name,
        r.Author.AvatarUrl,
        r.Rating,
        r.Comment,
        r.CreatedAt
    );
}
