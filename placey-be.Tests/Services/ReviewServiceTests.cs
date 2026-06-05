using placey_be.Models.DTOs.Reviews;
using placey_be.Models.Entities;
using placey_be.Services;
using placey_be.Tests.Helpers;

namespace placey_be.Tests.Services;

public class ReviewServiceTests
{
    private (ReviewService svc, User host, User guest, Listing listing, Booking booking) Setup(string dbName)
    {
        var db = DbHelper.CreateDb(dbName);

        var host = new User { Name = "Host", Email = "host@test.com", PasswordHash = "x" };
        var guest = new User { Name = "Guest", Email = "guest@test.com", PasswordHash = "x" };
        db.Users.AddRange(host, guest);

        var listing = new Listing
        {
            HostId = host.Id,
            Title = "Cabin",
            Description = "Cozy",
            City = "Forest",
            Country = "Germany",
            PricePerNight = 80m,
            MaxGuests = 2
        };
        db.Listings.Add(listing);

        var booking = new Booking
        {
            ListingId = listing.Id,
            GuestId = guest.Id,
            CheckIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            CheckOut = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            Guests = 1,
            TotalPrice = 480m,
            Status = BookingStatus.Completed
        };
        db.Bookings.Add(booking);
        db.SaveChanges();

        return (new ReviewService(db), host, guest, listing, booking);
    }

    [Fact]
    public async Task Create_ValidCompletedBooking_ReturnsReview()
    {
        var (svc, _, guest, _, booking) = Setup(nameof(Create_ValidCompletedBooking_ReturnsReview));

        var result = await svc.CreateAsync(guest.Id, new CreateReviewRequest(booking.Id, 5, "Amazing!"));

        Assert.Equal(5, result.Rating);
        Assert.Equal("Amazing!", result.Comment);
        Assert.Equal(guest.Id, result.AuthorId);
    }

    [Fact]
    public async Task Create_NotGuestsOwnBooking_Throws()
    {
        var (svc, _, _, _, booking) = Setup(nameof(Create_NotGuestsOwnBooking_Throws));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.CreateAsync(Guid.NewGuid(), new CreateReviewRequest(booking.Id, 4, "Good")));
    }

    [Fact]
    public async Task Create_BookingNotCompleted_Throws()
    {
        var (svc, _, guest, _, booking) = Setup(nameof(Create_BookingNotCompleted_Throws));

        var db = DbHelper.CreateDb(nameof(Create_BookingNotCompleted_Throws));
        var host2 = new User { Name = "H", Email = "h@t.com", PasswordHash = "x" };
        var guest2 = new User { Name = "G", Email = "g@t.com", PasswordHash = "x" };
        db.Users.AddRange(host2, guest2);
        var listing2 = new Listing
        {
            HostId = host2.Id, Title = "T", Description = "D", City = "City", Country = "Country", PricePerNight = 1m, MaxGuests = 1
        };
        db.Listings.Add(listing2);
        var pendingBooking = new Booking
        {
            ListingId = listing2.Id,
            GuestId = guest2.Id,
            CheckIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            CheckOut = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            Guests = 1,
            TotalPrice = 2m,
            Status = BookingStatus.Pending
        };
        db.Bookings.Add(pendingBooking);
        db.SaveChanges();

        var svc2 = new ReviewService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc2.CreateAsync(guest2.Id, new CreateReviewRequest(pendingBooking.Id, 3, "Meh")));
    }

    [Fact]
    public async Task Create_DuplicateReview_Throws()
    {
        var (svc, _, guest, _, booking) = Setup(nameof(Create_DuplicateReview_Throws));
        await svc.CreateAsync(guest.Id, new CreateReviewRequest(booking.Id, 4, "Good"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreateAsync(guest.Id, new CreateReviewRequest(booking.Id, 5, "Even better")));
    }

    [Fact]
    public async Task Create_BookingNotFound_Throws()
    {
        var (svc, _, guest, _, _) = Setup(nameof(Create_BookingNotFound_Throws));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.CreateAsync(guest.Id, new CreateReviewRequest(Guid.NewGuid(), 3, "?")));
    }

    [Fact]
    public async Task Delete_AuthorCanDelete()
    {
        var (svc, _, guest, listing, booking) = Setup(nameof(Delete_AuthorCanDelete));
        var review = await svc.CreateAsync(guest.Id, new CreateReviewRequest(booking.Id, 5, "Loved it"));

        await svc.DeleteAsync(review.Id, guest.Id);

        var remaining = await svc.GetByListingAsync(listing.Id);
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task Delete_NonAuthor_Throws()
    {
        var (svc, _, guest, _, booking) = Setup(nameof(Delete_NonAuthor_Throws));
        var review = await svc.CreateAsync(guest.Id, new CreateReviewRequest(booking.Id, 5, "Loved it"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.DeleteAsync(review.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByListing_ReturnsReviewsForListing()
    {
        var (svc, _, guest, listing, booking) = Setup(nameof(GetByListing_ReturnsReviewsForListing));
        await svc.CreateAsync(guest.Id, new CreateReviewRequest(booking.Id, 4, "Nice"));

        var result = await svc.GetByListingAsync(listing.Id);

        Assert.Single(result);
        Assert.Equal(listing.Id, result[0].ListingId);
    }

    [Fact]
    public async Task GetByUser_ReturnsReviewsForHostsListings()
    {
        var (svc, host, guest, _, booking) = Setup(nameof(GetByUser_ReturnsReviewsForHostsListings));
        await svc.CreateAsync(guest.Id, new CreateReviewRequest(booking.Id, 4, "Nice"));

        var result = await svc.GetByUserAsync(host.Id);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetByUser_OtherUser_ReturnsEmpty()
    {
        var (svc, _, guest, _, booking) = Setup(nameof(GetByUser_OtherUser_ReturnsEmpty));
        await svc.CreateAsync(guest.Id, new CreateReviewRequest(booking.Id, 4, "Nice"));

        var result = await svc.GetByUserAsync(Guid.NewGuid());

        Assert.Empty(result);
    }
}
