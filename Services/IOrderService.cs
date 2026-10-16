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

    // Fast-forward demo helpers
    (bool Success, string Message) SimulateExpireInspectionWindow(Guid orderId);
    (bool Success, string Message) SimulateExpireReservationWindow(Guid orderId);

    // Reviews
    (bool Success, string Message, Review? Review) SubmitReview(Guid orderId, string reviewerId, int rating, string comment);
    Review? GetReviewForOrder(Guid orderId, string reviewerId);
}
