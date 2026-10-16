using Reboot.Models;

namespace Reboot.Services;

public interface IDisputeService
{
    Dispute? GetDisputeById(Guid id);
    Dispute? GetDisputeByOrderId(Guid orderId);
    IReadOnlyList<Dispute> GetOpenDisputes();
    IReadOnlyList<Dispute> GetAllDisputes();

    (bool Success, string Message, Dispute? Dispute) OpenDispute(Guid orderId, string openedById, string reason, string evidenceNotes);
    (bool Success, string Message) ResolveRefund(Guid disputeId, string adminUserId, string adminNote);
    (bool Success, string Message) ResolveRelease(Guid disputeId, string adminUserId, string adminNote);
    (bool Success, string Message) ResolvePartial(Guid disputeId, string adminUserId, decimal partialRefundAmount, string adminNote);
}
