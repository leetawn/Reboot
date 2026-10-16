namespace Reboot.Models;

public class Offer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ListingId { get; set; }
    public string BuyerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public OfferStatus Status { get; set; } = OfferStatus.Pending;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
