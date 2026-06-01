namespace placey_be.Models.Entities;

public class ListingPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public int Order { get; set; }
}
