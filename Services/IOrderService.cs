using Reboot.Models;

namespace Reboot.Services;

public interface IOrderService
{
    Order? GetOrderById(Guid id);
    IReadOnlyList<Order> GetOrdersForUser(string userId);
    IReadOnlyList<Order> GetOrdersByBuyer(string buyerId);
    IReadOnlyList<Order> GetOrdersBySeller(string sellerId);
    IReadOnlyList<Order> GetAllOrdersAdmin();

    (bool Success, string Message, Order? Order) CreateOrderReservation(Guid listingId, string buyerId, DeliveryMethod deliveryMethod);
    (bool Success, string Message) SimulatePayment(Guid orderId, string buyerId);
    (bool Success, string Message) MarkHandedOverOrShipped(Guid orderId, string sellerId);
    (bool Success, string Message) ConfirmInspection(Guid orderId, string buyerId);
    (bool Success, string Message) CancelOrder(Guid orderId, string userId);
    (bool Success, string Message) CheckAutoRelease(Guid orderId);
    void CheckAndExpireReservations();

    // Meetup & Shipping Coordination
    (bool Success, string Message) ProposeMeetup(Guid orderId, string userId, string location, string? time, string? notes);
    (bool Success, string Message) ConfirmMeetup(Guid orderId, string userId);
    (bool Success, string Message) UpdateShippingDetails(Guid orderId, string sellerId, string courierName, string trackingNumber);
    (bool Success, string Message) AddOrderMessage(Guid orderId, string senderId, string message);
    (bool Success, string Message) ClaimUnresponsiveSellerRefund(Guid orderId, string buyerId);

    // Fast-forward demo helpers
    (bool Success, string Message) SimulateExpireInspectionWindow(Guid orderId);
    (bool Success, string Message) SimulateExpireReservationWindow(Guid orderId);
    (bool Success, string Message) SimulateExpireDispatchWindow(Guid orderId);

    // Reviews
    (bool Success, string Message, Review? Review) SubmitReview(Guid orderId, string reviewerId, int rating, string comment);
    Review? GetReviewForOrder(Guid orderId, string reviewerId);
}
