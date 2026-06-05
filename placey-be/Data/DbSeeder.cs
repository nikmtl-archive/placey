using Microsoft.EntityFrameworkCore;
using placey_be.Models.Entities;

namespace placey_be.Data;

public static class DbSeeder
{
    // Fixed GUIDs so seeding is idempotent
    private static readonly Guid User1Id = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid User2Id = new("11111111-0000-0000-0000-000000000002");
    private static readonly Guid User3Id = new("11111111-0000-0000-0000-000000000003");

    private static readonly Guid Listing1Id = new("22222222-0000-0000-0000-000000000001");
    private static readonly Guid Listing2Id = new("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Listing3Id = new("22222222-0000-0000-0000-000000000003");
    private static readonly Guid Listing4Id = new("22222222-0000-0000-0000-000000000004");
    private static readonly Guid Listing5Id = new("22222222-0000-0000-0000-000000000005");

    private static readonly Guid Booking1Id = new("33333333-0000-0000-0000-000000000001");
    private static readonly Guid Booking2Id = new("33333333-0000-0000-0000-000000000002");
    private static readonly Guid Booking3Id = new("33333333-0000-0000-0000-000000000003");

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync(u => u.Id == User1Id))
            return;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");

        var users = new List<User>
        {
            new()
            {
                Id = User1Id,
                Email = "alice@example.com",
                Name = "Alice Müller",
                Bio = "Passionate traveller and Airbnb superhost. I love sharing my cozy spaces with guests from around the world.",
                PasswordHash = passwordHash,
                CreatedAt = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            },
            new()
            {
                Id = User2Id,
                Email = "bob@example.com",
                Name = "Bob Schneider",
                Bio = "Architect and design enthusiast. My apartments reflect my love for clean lines and comfortable living.",
                PasswordHash = passwordHash,
                CreatedAt = new DateTime(2024, 2, 5, 0, 0, 0, DateTimeKind.Utc),
            },
            new()
            {
                Id = User3Id,
                Email = "carol@example.com",
                Name = "Carol Jensen",
                Bio = "Frequent traveller, always looking for a home away from home.",
                PasswordHash = passwordHash,
                CreatedAt = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            },
        };

        var listings = new List<Listing>
        {
            new()
            {
                Id = Listing1Id,
                HostId = User1Id,
                Title = "Cozy Studio in Mitte",
                Description = "A charming studio apartment in the heart of Berlin's Mitte district. Walking distance to Museum Island, the Hackescher Markt, and countless cafés. The apartment features exposed brick walls, a fully equipped kitchen, and a comfortable queen-size bed.",
                Street = "Rosenthaler Straße 42",
                City = "Berlin",
                Country = "Germany",
                Bedrooms = 1,
                Bathrooms = 1,
                PricePerNight = 85.00m,
                MaxGuests = 2,
                IsInstantBook = true,
                Category = ListingCategory.Urban,
                PropertyType = PropertyType.Apartment,
                Amenities = new List<string> { "WiFi", "Kitchen", "Heating", "TV", "Elevator", "Washer" },
                CreatedAt = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            },
            new()
            {
                Id = Listing2Id,
                HostId = User1Id,
                Title = "Modern Loft with Rooftop Access",
                Description = "Stunning two-bedroom loft on the 5th floor with exclusive rooftop access. Panoramic views over the city skyline. Features include an open-plan living area, designer furniture, and a private terrace. Perfect for couples or small families.",
                Street = "Mariahilfer Straße 18",
                City = "Vienna",
                Country = "Austria",
                Bedrooms = 2,
                Bathrooms = 2,
                PricePerNight = 140.00m,
                MaxGuests = 4,
                IsInstantBook = false,
                Category = ListingCategory.Luxe,
                PropertyType = PropertyType.Apartment,
                Amenities = new List<string> { "WiFi", "Kitchen", "Air Conditioning", "Heating", "TV", "Balcony", "Washer", "Elevator", "Gym" },
                CreatedAt = new DateTime(2024, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            },
            new()
            {
                Id = Listing3Id,
                HostId = User2Id,
                Title = "Architect-Designed Townhouse",
                Description = "A beautifully restored 19th-century townhouse redesigned with modern minimalist interiors. Three floors, three bedrooms, a private garden, and a fully equipped kitchen. Located in a quiet residential area, 10 minutes from the old town.",
                Street = "Mánesova 7",
                City = "Prague",
                Country = "Czech Republic",
                Bedrooms = 3,
                Bathrooms = 2,
                PricePerNight = 195.00m,
                MaxGuests = 6,
                IsInstantBook = false,
                Category = ListingCategory.Urban,
                PropertyType = PropertyType.House,
                Amenities = new List<string> { "WiFi", "Kitchen", "Heating", "TV", "Garden", "Washer", "Parking", "Fireplace", "Pet Friendly" },
                CreatedAt = new DateTime(2024, 2, 8, 0, 0, 0, DateTimeKind.Utc),
            },
            new()
            {
                Id = Listing4Id,
                HostId = User2Id,
                Title = "Lakeside Cabin Retreat",
                Description = "Escape to this peaceful wooden cabin on the shores of Lake Bled. Wake up to stunning mountain views, swim in crystal-clear water, and hike directly from the doorstep. Sleeps up to 4 guests with a wood-burning stove and a private dock.",
                Street = "Cesta svobode 15",
                City = "Bled",
                Country = "Slovenia",
                Bedrooms = 2,
                Bathrooms = 1,
                PricePerNight = 120.00m,
                MaxGuests = 4,
                IsInstantBook = true,
                Category = ListingCategory.Lake,
                PropertyType = PropertyType.House,
                Amenities = new List<string> { "WiFi", "Kitchen", "Heating", "Fireplace", "BBQ Grill", "Lake Access", "Mountain View", "Parking", "Pet Friendly" },
                CreatedAt = new DateTime(2024, 2, 14, 0, 0, 0, DateTimeKind.Utc),
            },
            new()
            {
                Id = Listing5Id,
                HostId = User1Id,
                Title = "Bright Apartment near Old Town",
                Description = "Light-filled one-bedroom apartment just two blocks from the Old Town Square. Recently renovated with Scandi-inspired décor, high-speed WiFi, and a well-stocked kitchen. Great base for exploring the city by foot.",
                Street = "Viru 3",
                City = "Tallinn",
                Country = "Estonia",
                Bedrooms = 1,
                Bathrooms = 1,
                PricePerNight = 70.00m,
                MaxGuests = 2,
                IsInstantBook = true,
                Category = ListingCategory.Urban,
                PropertyType = PropertyType.Apartment,
                Amenities = new List<string> { "WiFi", "Kitchen", "Heating", "TV", "Washer" },
                CreatedAt = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            },
        };

        var availabilities = new List<Availability>
        {
            new() { Id = new Guid("44444444-0000-0000-0000-000000000001"), ListingId = Listing1Id, StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 12, 31), IsBlocked = false },
            new() { Id = new Guid("44444444-0000-0000-0000-000000000002"), ListingId = Listing2Id, StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 12, 31), IsBlocked = false },
            new() { Id = new Guid("44444444-0000-0000-0000-000000000003"), ListingId = Listing3Id, StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 12, 31), IsBlocked = false },
            new() { Id = new Guid("44444444-0000-0000-0000-000000000004"), ListingId = Listing4Id, StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 12, 31), IsBlocked = false },
            new() { Id = new Guid("44444444-0000-0000-0000-000000000005"), ListingId = Listing5Id, StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 12, 31), IsBlocked = false },
        };

        // Completed past bookings so reviews can be attached
        var bookings = new List<Booking>
        {
            new()
            {
                Id = Booking1Id,
                ListingId = Listing1Id,
                GuestId = User3Id,
                CheckIn = new DateOnly(2026, 3, 10),
                CheckOut = new DateOnly(2026, 3, 15),
                Guests = 2,
                TotalPrice = 85.00m * 5,
                Status = BookingStatus.Completed,
                CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            },
            new()
            {
                Id = Booking2Id,
                ListingId = Listing3Id,
                GuestId = User3Id,
                CheckIn = new DateOnly(2026, 4, 5),
                CheckOut = new DateOnly(2026, 4, 9),
                Guests = 3,
                TotalPrice = 195.00m * 4,
                Status = BookingStatus.Completed,
                CreatedAt = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            },
            new()
            {
                Id = Booking3Id,
                ListingId = Listing4Id,
                GuestId = User1Id,
                CheckIn = new DateOnly(2026, 7, 1),
                CheckOut = new DateOnly(2026, 7, 7),
                Guests = 2,
                TotalPrice = 120.00m * 6,
                Status = BookingStatus.Confirmed,
                CreatedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            },
        };

        var reviews = new List<Review>
        {
            new()
            {
                Id = new Guid("55555555-0000-0000-0000-000000000001"),
                ListingId = Listing1Id,
                BookingId = Booking1Id,
                AuthorId = User3Id,
                Rating = 5,
                Comment = "Alice's studio is absolutely lovely. The location in Mitte is unbeatable — we walked everywhere. The apartment was spotless and had everything we needed. Would definitely stay again!",
                CreatedAt = new DateTime(2026, 3, 16, 0, 0, 0, DateTimeKind.Utc),
            },
            new()
            {
                Id = new Guid("55555555-0000-0000-0000-000000000002"),
                ListingId = Listing3Id,
                BookingId = Booking2Id,
                AuthorId = User3Id,
                Rating = 4,
                Comment = "The townhouse is stunning — every detail has been thoughtfully designed. Plenty of space for our group and the garden was a bonus. Minor note: parking nearby can be tricky during peak hours.",
                CreatedAt = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            },
        };

        var favorites = new List<Favorite>
        {
            new() { Id = new Guid("66666666-0000-0000-0000-000000000001"), UserId = User3Id, ListingId = Listing4Id },
            new() { Id = new Guid("66666666-0000-0000-0000-000000000002"), UserId = User3Id, ListingId = Listing2Id },
            new() { Id = new Guid("66666666-0000-0000-0000-000000000003"), UserId = User1Id, ListingId = Listing3Id },
        };

        await db.Users.AddRangeAsync(users);
        await db.Listings.AddRangeAsync(listings);
        await db.Availabilities.AddRangeAsync(availabilities);
        await db.Bookings.AddRangeAsync(bookings);
        await db.Reviews.AddRangeAsync(reviews);
        await db.Favorites.AddRangeAsync(favorites);

        await db.SaveChangesAsync();
    }
}
