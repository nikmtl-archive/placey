using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
        });

        model.Entity<Listing>(e =>
        {
            e.Property(l => l.PricePerNight).HasColumnType("numeric(10,2)");
            e.Property(l => l.Amenities)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!,
                    new ValueComparer<List<string>>(
                        (a, b) => a != null && b != null && a.SequenceEqual(b),
                        v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                        v => v.ToList()));
            e.Property(l => l.Category).HasConversion<string>();
            e.Property(l => l.PropertyType).HasConversion<string>();
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

        model.Entity<Conversation>(e =>
        {
            e.HasIndex(c => new { c.User1Id, c.User2Id }).IsUnique();
            e.HasOne(c => c.User1)
                .WithMany(u => u.ConversationsAsUser1)
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.User2)
                .WithMany(u => u.ConversationsAsUser2)
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<Message>(e =>
        {
            e.Property(m => m.Body).HasMaxLength(4000);
            e.HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
