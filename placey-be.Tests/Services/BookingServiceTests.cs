using placey_be.Models.DTOs.Bookings;
using placey_be.Models.Entities;
using placey_be.Services;
using placey_be.Tests.Helpers;

namespace placey_be.Tests.Services;

public class BookingServiceTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    private static readonly DateOnly Tomorrow = Today.AddDays(1);
    private static readonly DateOnly NextWeek = Today.AddDays(7);

    private (BookingService svc, User host, User guest, Listing listing) Setup(string dbName, bool instantBook = true)
    {
        var db = DbHelper.CreateDb(dbName);

        var host = new User { Name = "Host", Email = "host@test.com", PasswordHash = "x" };
        var guest = new User { Name = "Guest", Email = "guest@test.com", PasswordHash = "x" };
        db.Users.AddRange(host, guest);

        var listing = new Listing
        {
            HostId = host.Id,
            Title = "Beach House",
            Description = "Nice",
            City = "Cape Town",
            Country = "South Africa",
            PricePerNight = 100m,
            MaxGuests = 4,
            IsInstantBook = instantBook
        };
        db.Listings.Add(listing);
        db.SaveChanges();

        return (new BookingService(db), host, guest, listing);
    }

    [Fact]
    public async Task Create_InstantBook_StatusIsConfirmed()
    {
        var (svc, _, guest, listing) = Setup(nameof(Create_InstantBook_StatusIsConfirmed));

        var result = await svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow, NextWeek, 2));

        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.Equal(600m, result.TotalPrice);
    }

    [Fact]
    public async Task Create_NotInstantBook_StatusIsPending()
    {
        var (svc, _, guest, listing) = Setup(nameof(Create_NotInstantBook_StatusIsPending), instantBook: false);

        var result = await svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow, NextWeek, 2));

        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task Create_HostCantBookOwnListing_Throws()
    {
        var (svc, host, _, listing) = Setup(nameof(Create_HostCantBookOwnListing_Throws));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreateAsync(host.Id, new CreateBookingRequest(listing.Id, Tomorrow, NextWeek, 2)));
    }

    [Fact]
    public async Task Create_CheckInAfterCheckOut_Throws()
    {
        var (svc, _, guest, listing) = Setup(nameof(Create_CheckInAfterCheckOut_Throws));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, NextWeek, Tomorrow, 2)));
    }

    [Fact]
    public async Task Create_CheckInEqualsCheckOut_Throws()
    {
        var (svc, _, guest, listing) = Setup(nameof(Create_CheckInEqualsCheckOut_Throws));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow, Tomorrow, 2)));
    }

    [Fact]
    public async Task Create_DateConflict_Throws()
    {
        var (svc, _, guest, listing) = Setup(nameof(Create_DateConflict_Throws));

        await svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow, NextWeek, 2));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow.AddDays(1), NextWeek.AddDays(-1), 2)));
    }

    [Fact]
    public async Task Create_NonOverlappingDatesAfterCancel_Succeeds()
    {
        var (svc, _, guest, listing) = Setup(nameof(Create_NonOverlappingDatesAfterCancel_Succeeds));
        var first = await svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow, NextWeek, 2));

        await svc.CancelAsync(first.Id, guest.Id);

        var second = await svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow, NextWeek, 2));
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task Create_ListingNotFound_Throws()
    {
        var (svc, _, guest, _) = Setup(nameof(Create_ListingNotFound_Throws));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.CreateAsync(guest.Id, new CreateBookingRequest(Guid.NewGuid(), Tomorrow, NextWeek, 2)));
    }

    [Fact]
    public async Task GetById_AsGuest_ReturnsBooking()
    {
        var (svc, _, guest, listing) = Setup(nameof(GetById_AsGuest_ReturnsBooking));
        var booking = await svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow, NextWeek, 2));

        var result = await svc.GetByIdAsync(booking.Id, guest.Id);

        Assert.Equal(booking.Id, result.Id);
    }

    [Fact]
    public async Task GetById_AsHost_ReturnsBooking()
    {
        var (svc, host, guest, listing) = Setup(nameof(GetById_AsHost_ReturnsBooking));
        var booking = await svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow, NextWeek, 2));

        var result = await svc.GetByIdAsync(booking.Id, host.Id);

        Assert.Equal(booking.Id, result.Id);
    }

    [Fact]
    public async Task GetById_UnauthorizedUser_Throws()
    {
        var (svc, _, guest, listing) = Setup(nameof(GetById_UnauthorizedUser_Throws));
        var booking = await svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow, NextWeek, 2));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.GetByIdAsync(booking.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetById_NotFound_Throws()
    {
        var (svc, _, _, _) = Setup(nameof(GetById_NotFound_Throws));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task Cancel_GuestCanCancel()
    {
        var (svc, _, guest, listing) = Setup(nameof(Cancel_GuestCanCancel));
        var booking = await svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow, NextWeek, 2));

        await svc.CancelAsync(booking.Id, guest.Id);

        var updated = await svc.GetByIdAsync(booking.Id, guest.Id);
        Assert.Equal(BookingStatus.Cancelled, updated.Status);
    }

    [Fact]
    public async Task Cancel_HostCanCancel()
    {
        var (svc, host, guest, listing) = Setup(nameof(Cancel_HostCanCancel));
        var booking = await svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow, NextWeek, 2));

        await svc.CancelAsync(booking.Id, host.Id);

        var updated = await svc.GetByIdAsync(booking.Id, host.Id);
        Assert.Equal(BookingStatus.Cancelled, updated.Status);
    }

    [Fact]
    public async Task Cancel_UnauthorizedUser_Throws()
    {
        var (svc, _, guest, listing) = Setup(nameof(Cancel_UnauthorizedUser_Throws));
        var booking = await svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow, NextWeek, 2));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.CancelAsync(booking.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Cancel_CompletedBooking_Throws()
    {
        var (svc, host, guest, listing) = Setup(nameof(Cancel_CompletedBooking_Throws));
        var booking = await svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow, NextWeek, 2));
        await svc.UpdateStatusAsync(booking.Id, host.Id, BookingStatus.Completed);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CancelAsync(booking.Id, guest.Id));
    }

    [Fact]
    public async Task UpdateStatus_HostCanUpdate()
    {
        var (svc, host, guest, listing) = Setup(nameof(UpdateStatus_HostCanUpdate));
        var booking = await svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow, NextWeek, 2));

        var result = await svc.UpdateStatusAsync(booking.Id, host.Id, BookingStatus.Completed);

        Assert.Equal(BookingStatus.Completed, result.Status);
    }

    [Fact]
    public async Task UpdateStatus_NonHost_Throws()
    {
        var (svc, _, guest, listing) = Setup(nameof(UpdateStatus_NonHost_Throws));
        var booking = await svc.CreateAsync(guest.Id, new CreateBookingRequest(listing.Id, Tomorrow, NextWeek, 2));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.UpdateStatusAsync(booking.Id, guest.Id, BookingStatus.Completed));
    }
}
