using placey_be.Models.Entities;
using placey_be.Services;
using placey_be.Tests.Helpers;

namespace placey_be.Tests.Services;

public class FavoriteServiceTests
{
    private (FavoriteService svc, User user, Listing listing) Setup(string dbName)
    {
        var db = DbHelper.CreateDb(dbName);

        var host = new User { Name = "Host", Email = "host@test.com", PasswordHash = "x" };
        var user = new User { Name = "User", Email = "user@test.com", PasswordHash = "x" };
        db.Users.AddRange(host, user);

        var listing = new Listing
        {
            HostId = host.Id,
            Title = "Loft",
            Description = "Cool",
            City = "NYC",
            Country = "USA",
            PricePerNight = 200m,
            MaxGuests = 2
        };
        db.Listings.Add(listing);
        db.SaveChanges();

        return (new FavoriteService(db), user, listing);
    }

    [Fact]
    public async Task Add_ValidListing_AddsFavorite()
    {
        var (svc, user, listing) = Setup(nameof(Add_ValidListing_AddsFavorite));

        await svc.AddAsync(user.Id, listing.Id);

        var favorites = await svc.GetFavoritesAsync(user.Id);
        Assert.Single(favorites);
        Assert.Equal(listing.Id, favorites[0].Id);
    }

    [Fact]
    public async Task Add_ListingNotFound_Throws()
    {
        var (svc, user, _) = Setup(nameof(Add_ListingNotFound_Throws));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.AddAsync(user.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Add_AlreadyFavorited_IsIdempotent()
    {
        var (svc, user, listing) = Setup(nameof(Add_AlreadyFavorited_IsIdempotent));

        await svc.AddAsync(user.Id, listing.Id);
        await svc.AddAsync(user.Id, listing.Id);

        var favorites = await svc.GetFavoritesAsync(user.Id);
        Assert.Single(favorites);
    }

    [Fact]
    public async Task Remove_ExistingFavorite_RemovesIt()
    {
        var (svc, user, listing) = Setup(nameof(Remove_ExistingFavorite_RemovesIt));
        await svc.AddAsync(user.Id, listing.Id);

        await svc.RemoveAsync(user.Id, listing.Id);

        var favorites = await svc.GetFavoritesAsync(user.Id);
        Assert.Empty(favorites);
    }

    [Fact]
    public async Task Remove_NotFavorited_IsIdempotent()
    {
        var (svc, user, listing) = Setup(nameof(Remove_NotFavorited_IsIdempotent));

        var ex = await Record.ExceptionAsync(() => svc.RemoveAsync(user.Id, listing.Id));

        Assert.Null(ex);
    }

    [Fact]
    public async Task GetFavorites_EmptyForNewUser_ReturnsEmpty()
    {
        var (svc, user, _) = Setup(nameof(GetFavorites_EmptyForNewUser_ReturnsEmpty));

        var favorites = await svc.GetFavoritesAsync(user.Id);

        Assert.Empty(favorites);
    }
}
