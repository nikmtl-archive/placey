using placey_be.Models.DTOs.Auth;
using placey_be.Models.Entities;
using placey_be.Services;
using placey_be.Tests.Helpers;

namespace placey_be.Tests.Services;

public class AuthServiceTests
{
    private AuthService CreateService(string dbName) =>
        new(DbHelper.CreateDb(dbName), DbHelper.CreateJwtConfig());

    [Fact]
    public async Task Register_CreatesUserAndReturnsTokens()
    {
        var svc = CreateService(nameof(Register_CreatesUserAndReturnsTokens));
        var req = new RegisterRequest("alice@example.com", "password123", "Alice");

        var result = await svc.RegisterAsync(req);

        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.Equal("alice@example.com", result.User.Email);
        Assert.Equal("Alice", result.User.Name);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Throws()
    {
        var svc = CreateService(nameof(Register_DuplicateEmail_Throws));
        var req = new RegisterRequest("alice@example.com", "password123", "Alice");
        await svc.RegisterAsync(req);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.RegisterAsync(req));
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var svc = CreateService(nameof(Login_ValidCredentials_ReturnsTokens));
        await svc.RegisterAsync(new RegisterRequest("bob@example.com", "pass1234", "Bob"));

        var result = await svc.LoginAsync(new LoginRequest("bob@example.com", "pass1234"));

        Assert.NotEmpty(result.AccessToken);
        Assert.Equal("bob@example.com", result.User.Email);
    }

    [Fact]
    public async Task Login_UnknownEmail_Throws()
    {
        var svc = CreateService(nameof(Login_UnknownEmail_Throws));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.LoginAsync(new LoginRequest("nobody@example.com", "pass1234")));
    }

    [Fact]
    public async Task Login_WrongPassword_Throws()
    {
        var svc = CreateService(nameof(Login_WrongPassword_Throws));
        await svc.RegisterAsync(new RegisterRequest("carol@example.com", "correct", "Carol"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.LoginAsync(new LoginRequest("carol@example.com", "wrong")));
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        var svc = CreateService(nameof(Refresh_ValidToken_ReturnsNewTokens));
        var registered = await svc.RegisterAsync(new RegisterRequest("dave@example.com", "pass1234", "Dave"));

        var result = await svc.RefreshAsync(registered.RefreshToken);

        Assert.NotEmpty(result.AccessToken);
        Assert.NotEqual(registered.RefreshToken, result.RefreshToken);
    }

    [Fact]
    public async Task Refresh_InvalidToken_Throws()
    {
        var svc = CreateService(nameof(Refresh_InvalidToken_Throws));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.RefreshAsync("bad-token"));
    }

    [Fact]
    public async Task Refresh_RevokedToken_Throws()
    {
        var svc = CreateService(nameof(Refresh_RevokedToken_Throws));
        var auth = await svc.RegisterAsync(new RegisterRequest("eve@example.com", "pass1234", "Eve"));

        await svc.LogoutAsync(auth.RefreshToken);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.RefreshAsync(auth.RefreshToken));
    }

    [Fact]
    public async Task Logout_RevokesToken()
    {
        var svc = CreateService(nameof(Logout_RevokesToken));
        var auth = await svc.RegisterAsync(new RegisterRequest("frank@example.com", "pass1234", "Frank"));

        await svc.LogoutAsync(auth.RefreshToken);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.RefreshAsync(auth.RefreshToken));
    }

    [Fact]
    public async Task Logout_UnknownToken_DoesNotThrow()
    {
        var svc = CreateService(nameof(Logout_UnknownToken_DoesNotThrow));

        var ex = await Record.ExceptionAsync(() => svc.LogoutAsync("nonexistent-token"));

        Assert.Null(ex);
    }
}
