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

    private CreateListingRequest DefaultRequest(string location = "Berlin") =>
        new("Beach House", "Great place", location, 150m, 4, IsInstantBook: false);

    [Fact]
    public async Task Create_ReturnsCreatedListing()
    {
        var (svc, host) = Setup(nameof(Create_ReturnsCreatedListing));

        var result = await svc.CreateAsync(host.Id, DefaultRequest());

        Assert.Equal("Beach House", result.Title);
        Assert.Equal(host.Id, result.HostId);
        Assert.Equal(150m, result.PricePerNight);
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
            Location: null,
            PricePerNight: 200m,
            MaxGuests: null,
            IsInstantBook: true
        ));

        Assert.Equal("Updated Title", result.Title);
        Assert.Equal(200m, result.PricePerNight);
        Assert.True(result.IsInstantBook);
    }

    [Fact]
    public async Task Update_NonHost_Throws()
    {
        var (svc, host) = Setup(nameof(Update_NonHost_Throws));
        var created = await svc.CreateAsync(host.Id, DefaultRequest());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.UpdateAsync(created.Id, Guid.NewGuid(), new UpdateListingRequest(Title: "Hack", null, null, null, null, null)));
    }

    [Fact]
    public async Task Update_NotFound_Throws()
    {
        var (svc, host) = Setup(nameof(Update_NotFound_Throws));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.UpdateAsync(Guid.NewGuid(), host.Id, new UpdateListingRequest(null, null, null, null, null, null)));
    }

    [Fact]
    public async Task Delete_HostCanDelete()
    {
        var (svc, host) = Setup(nameof(Delete_HostCanDelete));
        var created = await svc.CreateAsync(host.Id, DefaultRequest());

        await svc.DeleteAsync(created.Id, host.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync(created.Id));
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
        await svc.CreateAsync(host.Id, DefaultRequest("Berlin"));
        await svc.CreateAsync(host.Id, DefaultRequest("Paris"));

        var (items, total) = await svc.SearchAsync(new ListingSearchParams(null, null, null, null));

        Assert.Equal(2, total);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task Search_LocationFilter_ReturnMatchingListings()
    {
        var (svc, host) = Setup(nameof(Search_LocationFilter_ReturnMatchingListings));
        await svc.CreateAsync(host.Id, DefaultRequest("Berlin"));
        await svc.CreateAsync(host.Id, DefaultRequest("Paris"));

        var (items, total) = await svc.SearchAsync(new ListingSearchParams("berlin", null, null, null));

        Assert.Equal(1, total);
        Assert.Equal("Berlin", items[0].Location);
    }

    [Fact]
    public async Task Search_GuestFilter_ExcludesTooSmallListings()
    {
        var (svc, host) = Setup(nameof(Search_GuestFilter_ExcludesTooSmallListings));
        await svc.CreateAsync(host.Id, new CreateListingRequest("Small", "d", "City", 100m, 2, false));
        await svc.CreateAsync(host.Id, new CreateListingRequest("Large", "d", "City", 100m, 8, false));

        var (items, _) = await svc.SearchAsync(new ListingSearchParams(null, null, null, Guests: 5));

        Assert.Single(items);
        Assert.Equal("Large", items[0].Title);
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
    public async Task SetAvailability_HostCanSet()
    {
        var (svc, host) = Setup(nameof(SetAvailability_HostCanSet));
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());
        var req = new SetAvailabilityRequest(Today, Today.AddDays(5), IsBlocked: true);

        var result = await svc.SetAvailabilityAsync(listing.Id, host.Id, req);

        Assert.Equal(Today, result.StartDate);
        Assert.True(result.IsBlocked);
    }

    [Fact]
    public async Task SetAvailability_NonHost_Throws()
    {
        var (svc, host) = Setup(nameof(SetAvailability_NonHost_Throws));
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.SetAvailabilityAsync(listing.Id, Guid.NewGuid(),
                new SetAvailabilityRequest(Today, Today.AddDays(3), false)));
    }

    [Fact]
    public async Task GetAvailability_ReturnsEntriesInOrder()
    {
        var (svc, host) = Setup(nameof(GetAvailability_ReturnsEntriesInOrder));
        var listing = await svc.CreateAsync(host.Id, DefaultRequest());
        await svc.SetAvailabilityAsync(listing.Id, host.Id, new SetAvailabilityRequest(Today.AddDays(10), Today.AddDays(15), false));
        await svc.SetAvailabilityAsync(listing.Id, host.Id, new SetAvailabilityRequest(Today, Today.AddDays(5), true));

        var result = await svc.GetAvailabilityAsync(listing.Id);

        Assert.Equal(2, result.Count);
        Assert.True(result[0].StartDate < result[1].StartDate);
    }
}
