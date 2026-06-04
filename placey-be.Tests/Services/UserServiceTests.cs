using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using placey_be.Models.DTOs.Users;
using placey_be.Models.Entities;
using placey_be.Services;
using placey_be.Tests.Helpers;

namespace placey_be.Tests.Services;

public class UserServiceTests
{
    private sealed class FakeEnv : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "Test";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Test";
    }

    private (UserService svc, User user) Setup(string dbName)
    {
        var db = DbHelper.CreateDb(dbName);
        var user = new User { Name = "Alice", Email = "alice@test.com", PasswordHash = "x", Bio = "Traveler" };
        db.Users.Add(user);
        db.SaveChanges();
        return (new UserService(db, new FakeEnv()), user);
    }

    [Fact]
    public async Task GetMe_ExistingUser_ReturnsPrivateResponse()
    {
        var (svc, user) = Setup(nameof(GetMe_ExistingUser_ReturnsPrivateResponse));

        var result = await svc.GetMeAsync(user.Id);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal("alice@test.com", result.Email);
        Assert.Equal("Alice", result.Name);
        Assert.Equal("Traveler", result.Bio);
    }

    [Fact]
    public async Task GetMe_UnknownUser_Throws()
    {
        var (svc, _) = Setup(nameof(GetMe_UnknownUser_Throws));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetMeAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateMe_UpdatesNameAndBio()
    {
        var (svc, user) = Setup(nameof(UpdateMe_UpdatesNameAndBio));

        var result = await svc.UpdateMeAsync(user.Id, new UpdateProfileRequest("Bob", "Hiker"));

        Assert.Equal("Bob", result.Name);
        Assert.Equal("Hiker", result.Bio);
    }

    [Fact]
    public async Task UpdateMe_NullFields_KeepsExistingValues()
    {
        var (svc, user) = Setup(nameof(UpdateMe_NullFields_KeepsExistingValues));

        var result = await svc.UpdateMeAsync(user.Id, new UpdateProfileRequest(null, null));

        Assert.Equal("Alice", result.Name);
        Assert.Equal("Traveler", result.Bio);
    }

    [Fact]
    public async Task DeleteMe_RemovesUser()
    {
        var (svc, user) = Setup(nameof(DeleteMe_RemovesUser));

        await svc.DeleteMeAsync(user.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetMeAsync(user.Id));
    }

    [Fact]
    public async Task GetPublic_ReturnsPublicProfile()
    {
        var (svc, user) = Setup(nameof(GetPublic_ReturnsPublicProfile));

        var result = await svc.GetPublicAsync(user.Id);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal("Alice", result.Name);
    }

    [Fact]
    public async Task GetPublic_UnknownUser_Throws()
    {
        var (svc, _) = Setup(nameof(GetPublic_UnknownUser_Throws));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetPublicAsync(Guid.NewGuid()));
    }
}
