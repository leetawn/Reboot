namespace Reboot.Models;

public class Dispute
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string OpenedById { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string EvidenceNotes { get; set; } = string.Empty;
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public string? AdminNote { get; set; }
    public decimal? PartialRefundAmount { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedDate { get; set; }
}
