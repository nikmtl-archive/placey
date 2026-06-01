namespace placey_be.Models.Entities;

public enum BookingStatus
{
    Pending,
    Confirmed,
    Completed,
    Cancelled
}

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;
    public Guid GuestId { get; set; }
    public User Guest { get; set; } = null!;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Guests { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
