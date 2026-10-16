namespace Reboot.Models;

public class Listing
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SellerId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public ListingCategory Category { get; set; }
    public decimal Price { get; set; }
    public decimal? MinimumOffer { get; set; }
    public string Description { get; set; } = string.Empty;
    public ItemGrade Grade { get; set; } = ItemGrade.A;
    public ConditionReport ConditionReport { get; set; } = new();
    public List<string> Photos { get; set; } = new();
    public string? SerialNumber { get; set; }
    public int? WarrantyOffered { get; set; } // null = None, or number of days (e.g. 7, 14, 30, 90)
    public DeliveryOption MeetupOrShipping { get; set; } = DeliveryOption.Both;
    public ListingStatus Status { get; set; } = ListingStatus.Active;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public bool AllowsOffers => MinimumOffer.HasValue && MinimumOffer.Value > 0;
}
