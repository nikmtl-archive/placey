namespace placey_be.Models.Entities;

public class Favorite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;
}
