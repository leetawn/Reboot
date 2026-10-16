namespace Reboot.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ListingId { get; set; }
    public string BuyerId { get; set; } = string.Empty;
    public string SellerId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.Meetup;
    public OrderStatus Status { get; set; } = OrderStatus.Reserved;

    // Transition Timestamps
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ReservedUntil { get; set; }
    public DateTime? PaidDate { get; set; }
    public DateTime? HandedOverDate { get; set; }
    public DateTime? InspectionDeadline { get; set; }
    public DateTime? ConfirmedDate { get; set; }
    public DateTime? ReleasedDate { get; set; }
    public DateTime? CancelledDate { get; set; }
    public DateTime? DisputedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }

    public bool IsReservationExpired =>
        Status == OrderStatus.Reserved &&
        ReservedUntil.HasValue &&
        DateTime.UtcNow > ReservedUntil.Value;

    public bool IsInspectionExpired =>
        Status == OrderStatus.HandedOverOrShipped &&
        InspectionDeadline.HasValue &&
        DateTime.UtcNow > InspectionDeadline.Value;

    public TimeSpan? RemainingReservationTime =>
        ReservedUntil.HasValue && Status == OrderStatus.Reserved
            ? (ReservedUntil.Value > DateTime.UtcNow ? ReservedUntil.Value - DateTime.UtcNow : TimeSpan.Zero)
            : null;

    public TimeSpan? RemainingInspectionTime =>
        InspectionDeadline.HasValue && Status == OrderStatus.HandedOverOrShipped
            ? (InspectionDeadline.Value > DateTime.UtcNow ? InspectionDeadline.Value - DateTime.UtcNow : TimeSpan.Zero)
            : null;
}
