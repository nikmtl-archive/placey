using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using placey_be.Data;

namespace placey_be.Tests.Helpers;

public static class DbHelper
{
    public static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    public static IConfiguration CreateJwtConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "super-secret-key-that-is-long-enough-32ch",
                ["Jwt:Issuer"] = "placey",
                ["Jwt:Audience"] = "placey"
            })
            .Build();
}
