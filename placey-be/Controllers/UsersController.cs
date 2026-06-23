using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using placey_be.Data;
using placey_be.Extensions;
using placey_be.Models.DTOs.Bookings;
using placey_be.Models.DTOs.Listings;
using placey_be.Models.DTOs.Users;
using placey_be.Services.Interfaces;

namespace placey_be.Controllers;

[ApiController]
[Route("users")]
public class UsersController(
    IUserService userService,
    IReviewService reviewService,
    AppDbContext db) : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var result = await userService.GetMeAsync(User.GetUserId());
        return Ok(result);
    }

    [Authorize]
    [HttpPatch("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var result = await userService.UpdateMeAsync(User.GetUserId(), request);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        await userService.DeleteMeAsync(User.GetUserId());
        return NoContent();
    }

    [Authorize]
    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var url = await userService.UploadAvatarAsync(User.GetUserId(), file);
        return Ok(new { url });
    }

    [Authorize]
    [HttpGet("me/listings")]
    public async Task<IActionResult> MyListings()
    {
        var userId = User.GetUserId();
        var listings = await db.Listings
            .Include(l => l.Host)
            .Include(l => l.Photos)
            .Include(l => l.Reviews)
            .Where(l => l.HostId == userId)
            .ToListAsync();

        return Ok(listings.Select(l => new {
            l.Id, l.Title, Location = $"{l.City}, {l.Country}", l.City, l.Country, l.PricePerNight, l.Bedrooms, l.MaxGuests, l.Category, l.PropertyType, l.CreatedAt
        }));
    }

    [Authorize]
    [HttpGet("me/bookings")]
    public async Task<IActionResult> MyBookings()
    {
        var userId = User.GetUserId();
        var bookings = await db.Bookings
            .Include(b => b.Listing)
            .Include(b => b.Guest)
            .Where(b => b.GuestId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BookingResponse(
                b.Id, b.ListingId, b.Listing.Title,
                b.GuestId, b.Guest.Name,
                b.CheckIn, b.CheckOut, b.Guests, b.TotalPrice, b.Status, b.CreatedAt))
            .ToListAsync();

        return Ok(bookings);
    }

    [Authorize]
    [HttpGet("me/reservations")]
    public async Task<IActionResult> MyReservations()
    {
        var userId = User.GetUserId();
        var reservations = await db.Bookings
            .Include(b => b.Listing)
            .Include(b => b.Guest)
            .Where(b => b.Listing.HostId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BookingResponse(
                b.Id, b.ListingId, b.Listing.Title,
                b.GuestId, b.Guest.Name,
                b.CheckIn, b.CheckOut, b.Guests, b.TotalPrice, b.Status, b.CreatedAt))
            .ToListAsync();

        return Ok(reservations);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPublic(Guid id)
    {
        var result = await userService.GetPublicAsync(id);
        return Ok(result);
    }

    [HttpGet("{id:guid}/listings")]
    public async Task<IActionResult> GetHostListings(Guid id)
    {
        var listings = await db.Listings
            .Include(l => l.Host)
            .Include(l => l.Photos)
            .Include(l => l.Reviews)
            .Where(l => l.HostId == id)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        var response = listings.Select(l => new ListingResponse(
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
        ));

        return Ok(response);
    }

    [HttpGet("{id:guid}/reviews")]
    public async Task<IActionResult> GetReviewsAboutHost(Guid id)
    {
        var result = await reviewService.GetByUserAsync(id);
        return Ok(result);
    }
}
