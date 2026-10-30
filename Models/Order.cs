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
    public DateTime? DispatchDeadline { get; set; } // 72-hour window for seller to hand over/ship once paid
    public DateTime? HandedOverDate { get; set; }
    public DateTime? InspectionDeadline { get; set; }
    public DateTime? ConfirmedDate { get; set; }
    public DateTime? ReleasedDate { get; set; }
    public DateTime? CancelledDate { get; set; }
    public DateTime? DisputedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }

    // Meetup Agreement & Coordination
    public string? MeetupLocation { get; set; }
    public string? MeetupTime { get; set; }
    public string? MeetupNotes { get; set; }
    public bool MeetupConfirmedByBuyer { get; set; }
    public bool MeetupConfirmedBySeller { get; set; }
    public bool IsMeetupAgreed => MeetupConfirmedByBuyer && MeetupConfirmedBySeller;

    // Courier Shipping Details (if shipping)
    public string? CourierName { get; set; }
    public string? TrackingNumber { get; set; }

    // Coordination message thread
    public List<OrderMessage> Messages { get; set; } = new();

    public bool IsReservationExpired =>
        Status == OrderStatus.Reserved &&
        ReservedUntil.HasValue &&
        DateTime.UtcNow > ReservedUntil.Value;

    public bool IsInspectionExpired =>
        Status == OrderStatus.HandedOverOrShipped &&
        InspectionDeadline.HasValue &&
        DateTime.UtcNow > InspectionDeadline.Value;

    public bool IsDispatchOverdue =>
        Status == OrderStatus.Paid &&
        DispatchDeadline.HasValue &&
        DateTime.UtcNow > DispatchDeadline.Value;

    public TimeSpan? RemainingReservationTime =>
        ReservedUntil.HasValue && Status == OrderStatus.Reserved
            ? (ReservedUntil.Value > DateTime.UtcNow ? ReservedUntil.Value - DateTime.UtcNow : TimeSpan.Zero)
            : null;

    public TimeSpan? RemainingInspectionTime =>
        InspectionDeadline.HasValue && Status == OrderStatus.HandedOverOrShipped
            ? (InspectionDeadline.Value > DateTime.UtcNow ? InspectionDeadline.Value - DateTime.UtcNow : TimeSpan.Zero)
            : null;

    public TimeSpan? RemainingDispatchTime =>
        DispatchDeadline.HasValue && Status == OrderStatus.Paid
            ? (DispatchDeadline.Value > DateTime.UtcNow ? DispatchDeadline.Value - DateTime.UtcNow : TimeSpan.Zero)
            : null;
}

public class OrderMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsSystem { get; set; } = false;
}
