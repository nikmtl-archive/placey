using placey_be.Middleware;
using placey_be.Models.DTOs.Listings;
using placey_be.Models.Entities;
using placey_be.Services;
using placey_be.Tests.Helpers;

namespace placey_be.Tests.Services;

public class ListingServiceTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private (ListingService svc, User host) Setup(string dbName)
    {
        var db = DbHelper.CreateDb(dbName);
        var host = new User { Name = "Host", Email = "host@test.com", PasswordHash = "x" };
        db.Users.Add(host);
        db.SaveChanges();
        return (new ListingService(db), host);
    }

    private CreateListingRequest DefaultRequest(string city = "Berlin", string country = "Germany") =>
        new("Beach House", "Great place", city, country, 150m, 4, 2, 1,
            ListingCategory.Urban, PropertyType.House);

    [Fact]
    public async Task Create_ReturnsCreatedListing()
    {
        var (svc, host) = Setup(nameof(Create_ReturnsCreatedListing));

        var result = await svc.CreateAsync(host.Id, DefaultRequest());

        Assert.Equal("Beach House", result.Title);
        Assert.Equal(host.Id, result.HostId);
        Assert.Equal(150m, result.PricePerNight);
        Assert.Equal(2, result.Bedrooms);
        Assert.Equal(1, result.Bathrooms);
        Assert.Equal(ListingCategory.Urban, result.Category);
        Assert.Equal(PropertyType.House, result.PropertyType);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsListing()
    {
        var (svc, host) = Setup(nameof(GetById_ExistingId_ReturnsListing));
        var created = await svc.CreateAsync(host.Id, DefaultRequest());

        var result = await svc.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
    }

    [Fact]
    public async Task GetById_NotFound_Throws()
    {
        var (svc, _) = Setup(nameof(GetById_NotFound_Throws));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_HostCanUpdateFields()
    {
        var (svc, host) = Setup(nameof(Update_HostCanUpdateFields));
        var created = await svc.CreateAsync(host.Id, DefaultRequest());

        var result = await svc.UpdateAsync(created.Id, host.Id, new UpdateListingRequest(
            Title: "Updated Title",
            Description: null,
            City: null,
            Country: null,
            Street: null,
            PricePerNight: 200m,
            MaxGuests: null,
            Bedrooms: 3,
            Bathrooms: null,
            IsInstantBook: true,
            Amenities: null,
            Category: ListingCategory.Luxe,
            PropertyType: null
        ));

        Assert.Equal("Updated Title", result.Title);
        Assert.Equal(200m, result.PricePerNight);
        Assert.True(result.IsInstantBook);
        Assert.Equal(3, result.Bedrooms);
        Assert.Equal(ListingCategory.Luxe, result.Category);
    }

    [Fact]
    public async Task Update_NonHost_Throws()
    {
        var (svc, host) = Setup(nameof(Update_NonHost_Throws));
        var created = await svc.CreateAsync(host.Id, DefaultRequest());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.UpdateAsync(created.Id, Guid.NewGuid(),
                new UpdateListingRequest("Hack", null, null, null, null, null, null, null, null, null, null, null, null)));
    }

    [Fact]
    public async Task Update_NotFound_Throws()
    {
        var (svc, host) = Setup(nameof(Update_NotFound_Throws));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.UpdateAsync(Guid.NewGuid(), host.Id,
                new UpdateListingRequest(null, null, null, null, null, null, null, null, null, null, null, null, null)));
    }

    [Fact]
    public async Task Delete_HostCanDelete()
    {
        var (svc, host) = Setup(nameof(Delete_HostCanDelete));
        var created = await svc.CreateAsync(host.Id, DefaultRequest());

        await svc.DeleteAsync(created.Id, host.Id);

        await Assert.ThrowsAsync<ResourceDeletedException>(() => svc.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task Delete_NonHost_Throws()
    {
        var (svc, host) = Setup(nameof(Delete_NonHost_Throws));
        var created = await svc.CreateAsync(host.Id, DefaultRequest());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.DeleteAsync(created.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Search_NoFilter_ReturnsAllListings()
    {
        var (svc, host) = Setup(nameof(Search_NoFilter_ReturnsAllListings));
        await svc.CreateAsync(host.Id, DefaultRequest("Berlin", "Germany"));
        await svc.CreateAsync(host.Id, DefaultRequest("Paris", "France"));

        var (items, total) = await svc.SearchAsync(new ListingSearchParams(null, null, null, null));

        Assert.Equal(2, total);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task Search_LocationFilter_ReturnMatchingListings()
    {
        var (svc, host) = Setup(nameof(Search_LocationFilter_ReturnMatchingListings));
        await svc.CreateAsync(host.Id, DefaultRequest("Berlin", "Germany"));
        await svc.CreateAsync(host.Id, DefaultRequest("Paris", "France"));

        var (items, total) = await svc.SearchAsync(new ListingSearchParams("berlin", null, null, null));

        Assert.Equal(1, total);
        Assert.Equal("Berlin", items[0].City);
    }

    [Fact]
    public async Task Search_GuestFilter_ExcludesTooSmallListings()
    {
        var (svc, host) = Setup(nameof(Search_GuestFilter_ExcludesTooSmallListings));
        await svc.CreateAsync(host.Id, new CreateListingRequest("Small", "d", "City", "Country", 100m, 2, 1, 1, ListingCategory.Urban, PropertyType.Apartment));
        await svc.CreateAsync(host.Id, new CreateListingRequest("Large", "d", "City", "Country", 100m, 8, 4, 2, ListingCategory.Urban, PropertyType.House));

        var (items, _) = await svc.SearchAsync(new ListingSearchParams(null, null, null, Guests: 5));

        Assert.Single(items);
        Assert.Equal("Large", items[0].Title);
    }

    [Fact]
    public async Task Search_CategoryFilter_ReturnMatchingListings()
    {
        var (svc, host) = Setup(nameof(Search_CategoryFilter_ReturnMatchingListings));
        await svc.CreateAsync(host.Id, new CreateListingRequest("Beach House", "d", "Malibu", "USA", 300m, 6, 3, 2, ListingCategory.Beach, PropertyType.House));
        await svc.CreateAsync(host.Id, new CreateListingRequest("City Flat", "d", "London", "UK", 120m, 2, 1, 1, ListingCategory.Urban, PropertyType.Apartment));

        var (items, total) = await svc.SearchAsync(new ListingSearchParams(null, null, null, null, Category: ListingCategory.Beach));

        Assert.Equal(1, total);
        Assert.Equal(ListingCategory.Beach, items[0].Category);
    }

    [Fact]
    public async Task Search_PropertyTypeFilter_ReturnMatchingListings()
    {
        var (svc, host) = Setup(nameof(Search_PropertyTypeFilter_ReturnMatchingListings));
        await svc.CreateAsync(host.Id, new CreateListingRequest("A House", "d", "City", "Country", 200m, 6, 3, 2, ListingCategory.Countryside, PropertyType.House));
        await svc.CreateAsync(host.Id, new CreateListingRequest("An Apartment", "d", "City", "Country", 80m, 2, 1, 1, ListingCategory.Urban, PropertyType.Apartment));

        var (items, total) = await svc.SearchAsync(new ListingSearchParams(null, null, null, null, PropertyType: PropertyType.Apartment));

        Assert.Equal(1, total);
        Assert.Equal(PropertyType.Apartment, items[0].PropertyType);
    }

    [Fact]
    public async Task Search_MinBedroomsFilter_ExcludesSmallListings()
    {
        var (svc, host) = Setup(nameof(Search_MinBedroomsFilter_ExcludesSmallListings));
        await svc.CreateAsync(host.Id, new CreateListingRequest("Studio", "d", "City", "Country", 80m, 2, 0, 1, ListingCategory.Urban, PropertyType.Apartment));
        await svc.CreateAsync(host.Id, new CreateListingRequest("Villa", "d", "City", "Country", 400m, 10, 5, 3, ListingCategory.Luxe, PropertyType.House));

        var (items, total) = await svc.SearchAsync(new ListingSearchParams(null, null, null, null, MinBedrooms: 3));

        Assert.Equal(1, total);
        Assert.Equal("Villa", items[0].Title);
    }

    [Fact]
    public async Task Search_PriceRangeFilter_ReturnMatchingListings()
    {
        var (svc, host) = Setup(nameof(Search_PriceRangeFilter_ReturnMatchingListings));
        await svc.CreateAsync(host.Id, new CreateListingRequest("Budget", "d", "City", "Country", 50m, 2, 1, 1, ListingCategory.Urban, PropertyType.Apartment));
        await svc.CreateAsync(host.Id, new CreateListingRequest("Mid", "d", "City", "Country", 150m, 4, 2, 1, ListingCategory.Urban, PropertyType.Apartment));
        await svc.CreateAsync(host.Id, new CreateListingRequest("Luxury", "d", "City", "Country", 500m, 8, 4, 3, ListingCategory.Luxe, PropertyType.House));

        var (items, total) = await svc.SearchAsync(new ListingSearchParams(null, null, null, null, MinPrice: 100m, MaxPrice: 200m));

        Assert.Equal(1, total);
        Assert.Equal("Mid", items[0].Title);
    }

    [Fact]
    public async Task Search_Pagination_RespectsPageAndLimit()
    {
        var (svc, host) = Setup(nameof(Search_Pagination_RespectsPageAndLimit));
        for (var i = 0; i < 5; i++)
            await svc.CreateAsync(host.Id, DefaultRequest());

        var (items, total) = await svc.SearchAsync(new ListingSearchParams(null, null, null, null, Page: 2, Limit: 2));

        Assert.Equal(5, total);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task AddBlocking_HostCanAdd()
    {
        var (svc, host) = Setup(nameof(AddBlocking_HostCanAdd));
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());
        var req = new AddBlockingRequest(Today, Today.AddDays(5));

        var result = await svc.AddBlockingAsync(listing.Id, host.Id, req);

        Assert.Equal(Today, result.StartDate);
        Assert.Equal(Today.AddDays(5), result.EndDate);
    }

    [Fact]
    public async Task AddBlocking_NonHost_Throws()
    {
        var (svc, host) = Setup(nameof(AddBlocking_NonHost_Throws));
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.AddBlockingAsync(listing.Id, Guid.NewGuid(),
                new AddBlockingRequest(Today, Today.AddDays(3))));
    }

    [Fact]
    public async Task GetBlockings_ReturnsEntriesInOrder()
    {
        var (svc, host) = Setup(nameof(GetBlockings_ReturnsEntriesInOrder));
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());
        await svc.AddBlockingAsync(listing.Id, host.Id, new AddBlockingRequest(Today.AddDays(10), Today.AddDays(15)));
        await svc.AddBlockingAsync(listing.Id, host.Id, new AddBlockingRequest(Today, Today.AddDays(5)));

        var result = await svc.GetBlockingsAsync(listing.Id);

        Assert.Equal(2, result.Count);
        Assert.True(result[0].StartDate < result[1].StartDate);
    }

    [Fact]
    public async Task GetBlockings_IncludesConfirmedBookings()
    {
        var db = DbHelper.CreateDb(nameof(GetBlockings_IncludesConfirmedBookings));
        var host = new User { Name = "Host", Email = "host@test.com", PasswordHash = "x" };
        var guest = new User { Name = "Guest", Email = "guest@test.com", PasswordHash = "x" };
        db.Users.AddRange(host, guest);
        db.SaveChanges();
        var svc = new ListingService(db);
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());

        db.Bookings.Add(new Booking
        {
            ListingId = listing.Id, GuestId = guest.Id,
            CheckIn = Today.AddDays(2), CheckOut = Today.AddDays(5),
            Guests = 1, TotalPrice = 300m, Status = BookingStatus.Confirmed
        });
        db.SaveChanges();

        var result = await svc.GetBlockingsAsync(listing.Id);

        Assert.Single(result);
        Assert.Equal(Today.AddDays(2), result[0].StartDate);
    }

    [Fact]
    public async Task GetBlockings_ExcludesCancelledBookings()
    {
        var db = DbHelper.CreateDb(nameof(GetBlockings_ExcludesCancelledBookings));
        var host = new User { Name = "Host", Email = "host@test.com", PasswordHash = "x" };
        var guest = new User { Name = "Guest", Email = "guest@test.com", PasswordHash = "x" };
        db.Users.AddRange(host, guest);
        db.SaveChanges();
        var svc = new ListingService(db);
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());

        db.Bookings.Add(new Booking
        {
            ListingId = listing.Id, GuestId = guest.Id,
            CheckIn = Today.AddDays(2), CheckOut = Today.AddDays(5),
            Guests = 1, TotalPrice = 300m, Status = BookingStatus.Cancelled
        });
        db.SaveChanges();

        var result = await svc.GetBlockingsAsync(listing.Id);

        Assert.Empty(result);
    }

    [Fact]
    public async Task DeleteBlocking_HostCanDelete()
    {
        var (svc, host) = Setup(nameof(DeleteBlocking_HostCanDelete));
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());
        var blocking = await svc.AddBlockingAsync(listing.Id, host.Id, new AddBlockingRequest(Today, Today.AddDays(3)));

        await svc.DeleteBlockingAsync(listing.Id, blocking.Id, host.Id);

        var result = await svc.GetBlockingsAsync(listing.Id);
        Assert.Empty(result);
    }

    [Fact]
    public async Task DeleteBlocking_NonHost_Throws()
    {
        var (svc, host) = Setup(nameof(DeleteBlocking_NonHost_Throws));
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());
        var blocking = await svc.AddBlockingAsync(listing.Id, host.Id, new AddBlockingRequest(Today, Today.AddDays(3)));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.DeleteBlockingAsync(listing.Id, blocking.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteBlocking_NotFound_Throws()
    {
        var (svc, host) = Setup(nameof(DeleteBlocking_NotFound_Throws));
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.DeleteBlockingAsync(listing.Id, Guid.NewGuid(), host.Id));
    }

    [Fact]
    public async Task Delete_CancelsActiveBookings()
    {
        var db = DbHelper.CreateDb(nameof(Delete_CancelsActiveBookings));
        var host = new User { Name = "Host", Email = "host@test.com", PasswordHash = "x" };
        var guest = new User { Name = "Guest", Email = "guest@test.com", PasswordHash = "x" };
        db.Users.AddRange(host, guest);
        db.SaveChanges();
        var svc = new ListingService(db);
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());
        var booking = new Booking
        {
            ListingId = listing.Id, GuestId = guest.Id,
            CheckIn = Today.AddDays(1), CheckOut = Today.AddDays(5),
            Guests = 2, TotalPrice = 600m, Status = BookingStatus.Confirmed
        };
        db.Bookings.Add(booking);
        db.SaveChanges();

        await svc.DeleteAsync(listing.Id, host.Id);

        var updated = db.Bookings.Find(booking.Id)!;
        Assert.Equal(BookingStatus.Cancelled, updated.Status);
    }

    [Fact]
    public async Task Search_DateRangeFilter_ExcludesBookedListings()
    {
        var db = DbHelper.CreateDb(nameof(Search_DateRangeFilter_ExcludesBookedListings));
        var host = new User { Name = "Host", Email = "host@test.com", PasswordHash = "x" };
        var guest = new User { Name = "Guest", Email = "guest@test.com", PasswordHash = "x" };
        db.Users.AddRange(host, guest);
        db.SaveChanges();
        var svc = new ListingService(db);
        var booked = await svc.CreateAsync(host.Id, DefaultRequest("Berlin", "Germany"));
        var free = await svc.CreateAsync(host.Id, DefaultRequest("Vienna", "Austria"));

        db.Bookings.Add(new Booking
        {
            ListingId = booked.Id, GuestId = guest.Id,
            CheckIn = Today.AddDays(2), CheckOut = Today.AddDays(6),
            Guests = 1, TotalPrice = 600m, Status = BookingStatus.Confirmed
        });
        db.SaveChanges();

        var (items, total) = await svc.SearchAsync(new ListingSearchParams(
            null, Today.AddDays(3), Today.AddDays(5), null));

        Assert.Equal(1, total);
        Assert.Equal(free.Id, items[0].Id);
    }

    [Fact]
    public async Task GetPhotos_HostCanView()
    {
        var (svc, host) = Setup(nameof(GetPhotos_HostCanView));
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());

        var photos = await svc.GetPhotosAsync(listing.Id, host.Id);

        Assert.Empty(photos);
    }

    [Fact]
    public async Task GetPhotos_NonHost_Throws()
    {
        var (svc, host) = Setup(nameof(GetPhotos_NonHost_Throws));
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.GetPhotosAsync(listing.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task DeletePhoto_NotFound_Throws()
    {
        var (svc, host) = Setup(nameof(DeletePhoto_NotFound_Throws));
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.DeletePhotoAsync(listing.Id, Guid.NewGuid(), host.Id));
    }

    [Fact]
    public async Task DeletePhoto_NonHost_Throws()
    {
        var db = DbHelper.CreateDb(nameof(DeletePhoto_NonHost_Throws));
        var host = new User { Name = "Host", Email = "host@test.com", PasswordHash = "x" };
        db.Users.Add(host);
        db.SaveChanges();
        var svc = new ListingService(db);
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());
        var photo = new ListingPhoto { ListingId = listing.Id, Url = "/api/listing-photos/test.jpg", Order = 0 };
        db.ListingPhotos.Add(photo);
        db.SaveChanges();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.DeletePhotoAsync(listing.Id, photo.Id, Guid.NewGuid()));
    }
}
