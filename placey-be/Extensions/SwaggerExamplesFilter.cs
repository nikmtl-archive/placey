using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using placey_be.Models.DTOs.Auth;
using placey_be.Models.DTOs.Bookings;
using placey_be.Models.DTOs.Listings;
using placey_be.Models.DTOs.Reviews;
using placey_be.Models.DTOs.Users;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace placey_be.Extensions;

public class SwaggerExamplesFilter : ISchemaFilter
{
    private static readonly Dictionary<Type, OpenApiObject> Examples = new()
    {
        [typeof(RegisterRequest)] = new()
        {
            ["email"] = new OpenApiString("alice@example.com"),
            ["password"] = new OpenApiString("password123"),
            ["name"] = new OpenApiString("Alice Smith")
        },
        [typeof(LoginRequest)] = new()
        {
            ["email"] = new OpenApiString("alice@example.com"),
            ["password"] = new OpenApiString("password123")
        },
        [typeof(RefreshRequest)] = new()
        {
            ["refreshToken"] = new OpenApiString("<paste refresh token here>")
        },
        [typeof(CreateListingRequest)] = new()
        {
            ["title"] = new OpenApiString("Cozy Studio in Munich City Centre"),
            ["description"] = new OpenApiString("Bright studio apartment, 5 min walk from Marienplatz. Kitchen, fast Wi-Fi, and a balcony."),
            ["location"] = new OpenApiString("Munich, Germany"),
            ["pricePerNight"] = new OpenApiDouble(89.00),
            ["maxGuests"] = new OpenApiInteger(2),
            ["isInstantBook"] = new OpenApiBoolean(true)
        },
        [typeof(UpdateListingRequest)] = new()
        {
            ["pricePerNight"] = new OpenApiDouble(99.00),
            ["isInstantBook"] = new OpenApiBoolean(false)
        },
        [typeof(AddBlockingRequest)] = new()
        {
            ["startDate"] = new OpenApiString("2026-08-01"),
            ["endDate"] = new OpenApiString("2026-08-15"),
        },
        [typeof(CreateBookingRequest)] = new()
        {
            ["listingId"] = new OpenApiString("00000000-0000-0000-0000-000000000001"),
            ["checkIn"] = new OpenApiString("2026-07-10"),
            ["checkOut"] = new OpenApiString("2026-07-14"),
            ["guests"] = new OpenApiInteger(2)
        },
        [typeof(UpdateBookingStatusRequest)] = new()
        {
            ["status"] = new OpenApiInteger(1) // 1 = Confirmed
        },
        [typeof(CreateReviewRequest)] = new()
        {
            ["bookingId"] = new OpenApiString("00000000-0000-0000-0000-000000000001"),
            ["rating"] = new OpenApiInteger(5),
            ["comment"] = new OpenApiString("Fantastic place — spotless, well-equipped, and exactly as described. Would book again.")
        },
        [typeof(UpdateProfileRequest)] = new()
        {
            ["name"] = new OpenApiString("Alice Smith"),
            ["bio"] = new OpenApiString("Traveller and occasional host based in Munich.")
        }
    };

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (Examples.TryGetValue(context.Type, out var example))
            schema.Example = example;
    }
}
