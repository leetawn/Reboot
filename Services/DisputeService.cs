using Reboot.Models;

namespace Reboot.Services;

public class DisputeService : IDisputeService
{
    private readonly IMarketplaceDataStore _dataStore;

    public DisputeService(IMarketplaceDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Dispute? GetDisputeById(Guid id)
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Disputes.FirstOrDefault(d => d.Id == id);
        }
    }

    public Dispute? GetDisputeByOrderId(Guid orderId)
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Disputes.FirstOrDefault(d => d.OrderId == orderId);
        }
    }

    public IReadOnlyList<Dispute> GetOpenDisputes()
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Disputes
                .Where(d => d.Status == DisputeStatus.Open)
                .OrderByDescending(d => d.CreatedDate)
                .ToList();
        }
    }

    public IReadOnlyList<Dispute> GetAllDisputes()
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Disputes
                .OrderByDescending(d => d.CreatedDate)
                .ToList();
        }
    }

    public (bool Success, string Message, Dispute? Dispute) OpenDispute(Guid orderId, string openedById, string reason, string evidenceNotes)
    {
        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return (false, "Order not found.", null);

            if (order.BuyerId != openedById)
            {
                return (false, "Only the buyer can open a dispute during inspection.", null);
            }

            if (order.Status != OrderStatus.HandedOverOrShipped)
            {
                return (false, $"Cannot open dispute when order is in state '{order.Status}'.", null);
            }

            var existing = _dataStore.Disputes.FirstOrDefault(d => d.OrderId == orderId && d.Status == DisputeStatus.Open);
            if (existing != null)
            {
                return (false, "A dispute is already active for this order.", null);
            }

            var dispute = new Dispute
            {
                OrderId = orderId,
                OpenedById = openedById,
                Reason = reason.Trim(),
                EvidenceNotes = evidenceNotes.Trim(),
                Status = DisputeStatus.Open,
                CreatedDate = DateTime.UtcNow
            };

            order.Status = OrderStatus.Disputed;
            order.DisputedDate = DateTime.UtcNow;

            _dataStore.Disputes.Add(dispute);

            return (true, "Dispute opened. Escrow funds are frozen pending administrative review.", dispute);
        }
    }

    public (bool Success, string Message) ResolveRefund(Guid disputeId, string adminUserId, string adminNote)
    {
        lock (_dataStore.SyncRoot)
        {
            var dispute = _dataStore.Disputes.FirstOrDefault(d => d.Id == disputeId);
            if (dispute == null) return (false, "Dispute not found.");

            if (dispute.Status != DisputeStatus.Open) return (false, "Dispute is already resolved.");

            var admin = _dataStore.Users.FirstOrDefault(u => u.Id == adminUserId);
            if (admin == null || !admin.IsAdmin) return (false, "Unauthorized: Admin privileges required.");

            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == dispute.OrderId);
            if (order == null) return (false, "Associated order not found.");

            var now = DateTime.UtcNow;
            dispute.Status = DisputeStatus.ResolvedRefund;
            dispute.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? "Full refund approved to buyer." : adminNote.Trim();
            dispute.ResolvedDate = now;

            order.Status = OrderStatus.Resolved;
            order.ResolvedDate = now;

            // Return listing to active or removed
            var listing = _dataStore.Listings.FirstOrDefault(l => l.Id == order.ListingId);
            if (listing != null)
            {
                listing.Status = ListingStatus.Active;
            }

            return (true, "Dispute resolved: Full refund returned to buyer. Listing returned to active.");
        }
    }

    public (bool Success, string Message) ResolveRelease(Guid disputeId, string adminUserId, string adminNote)
    {
        lock (_dataStore.SyncRoot)
        {
            var dispute = _dataStore.Disputes.FirstOrDefault(d => d.Id == disputeId);
            if (dispute == null) return (false, "Dispute not found.");

            if (dispute.Status != DisputeStatus.Open) return (false, "Dispute is already resolved.");

            var admin = _dataStore.Users.FirstOrDefault(u => u.Id == adminUserId);
            if (admin == null || !admin.IsAdmin) return (false, "Unauthorized: Admin privileges required.");

            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == dispute.OrderId);
            if (order == null) return (false, "Associated order not found.");

            var now = DateTime.UtcNow;
            dispute.Status = DisputeStatus.ResolvedRelease;
            dispute.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? "Funds released to seller after review." : adminNote.Trim();
            dispute.ResolvedDate = now;

            order.Status = OrderStatus.Resolved;
            order.ResolvedDate = now;

            var listing = _dataStore.Listings.FirstOrDefault(l => l.Id == order.ListingId);
            if (listing != null)
            {
                listing.Status = ListingStatus.Sold;
            }

            return (true, "Dispute resolved: Funds released to seller.");
        }
    }

    public (bool Success, string Message) ResolvePartial(Guid disputeId, string adminUserId, decimal partialRefundAmount, string adminNote)
    {
        lock (_dataStore.SyncRoot)
        {
            var dispute = _dataStore.Disputes.FirstOrDefault(d => d.Id == disputeId);
            if (dispute == null) return (false, "Dispute not found.");

            if (dispute.Status != DisputeStatus.Open) return (false, "Dispute is already resolved.");

            var admin = _dataStore.Users.FirstOrDefault(u => u.Id == adminUserId);
            if (admin == null || !admin.IsAdmin) return (false, "Unauthorized: Admin privileges required.");

            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == dispute.OrderId);
            if (order == null) return (false, "Associated order not found.");

            if (partialRefundAmount <= 0 || partialRefundAmount >= order.Price)
            {
                return (false, $"Partial refund amount must be between ₱1 and {AppConfig.FormatPrice(order.Price - 1)}.");
            }

            var now = DateTime.UtcNow;
            dispute.Status = DisputeStatus.ResolvedPartial;
            dispute.PartialRefundAmount = partialRefundAmount;
            dispute.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? $"Partial refund of {AppConfig.FormatPrice(partialRefundAmount)} granted to buyer; remainder released to seller." : adminNote.Trim();
            dispute.ResolvedDate = now;

            order.Status = OrderStatus.Resolved;
            order.ResolvedDate = now;

            var listing = _dataStore.Listings.FirstOrDefault(l => l.Id == order.ListingId);
            if (listing != null)
            {
                listing.Status = ListingStatus.Sold;
            }

            return (true, $"Dispute resolved: Partial refund of {AppConfig.FormatPrice(partialRefundAmount)} released to buyer, remainder to seller.");
        }
    }
}
