using Microsoft.EntityFrameworkCore;
using placey_be.Models.Entities;

namespace placey_be.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingPhoto> ListingPhotos => Set<ListingPhoto>();
    public DbSet<Availability> Availabilities => Set<Availability>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
        });

        model.Entity<Listing>(e =>
        {
            e.Property(l => l.PricePerNight).HasColumnType("numeric(10,2)");
            e.Property(l => l.Amenities).HasColumnType("text[]");
            e.HasOne(l => l.Host)
                .WithMany(u => u.Listings)
                .HasForeignKey(l => l.HostId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<Booking>(e =>
        {
            e.Property(b => b.TotalPrice).HasColumnType("numeric(10,2)");
            e.HasOne(b => b.Guest)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.GuestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<Review>(e =>
        {
            e.HasIndex(r => r.BookingId).IsUnique();
            e.HasOne(r => r.Author)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<Favorite>(e =>
        {
            e.HasIndex(f => new { f.UserId, f.ListingId }).IsUnique();
        });
    }
}
