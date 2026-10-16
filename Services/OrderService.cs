using Reboot.Models;

namespace Reboot.Services;

public class OrderService : IOrderService
{
    private readonly IMarketplaceDataStore _dataStore;

    public OrderService(IMarketplaceDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Order? GetOrderById(Guid id)
    {
        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == id);
            if (order != null)
            {
                CheckOrderDeadlines(order);
            }
            return order;
        }
    }

    public IReadOnlyList<Order> GetOrdersForUser(string userId)
    {
        lock (_dataStore.SyncRoot)
        {
            var orders = _dataStore.Orders
                .Where(o => o.BuyerId == userId || o.SellerId == userId)
                .OrderByDescending(o => o.CreatedDate)
                .ToList();

            foreach (var order in orders)
            {
                CheckOrderDeadlines(order);
            }

            return orders;
        }
    }

    public IReadOnlyList<Order> GetOrdersByBuyer(string buyerId)
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Orders
                .Where(o => o.BuyerId == buyerId)
                .OrderByDescending(o => o.CreatedDate)
                .ToList();
        }
    }

    public IReadOnlyList<Order> GetOrdersBySeller(string sellerId)
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Orders
                .Where(o => o.SellerId == sellerId)
                .OrderByDescending(o => o.CreatedDate)
                .ToList();
        }
    }

    public IReadOnlyList<Order> GetAllOrdersAdmin()
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Orders
                .OrderByDescending(o => o.CreatedDate)
                .ToList();
        }
    }

    /// <summary>
    /// Escrow Step 1: Reserve
    /// Enforces strict concurrency control: Two buyers cannot reserve or buy the same listing.
    /// Listing is marked Reserved with a 24-hour expiration timer.
    /// </summary>
    public (bool Success, string Message, Order? Order) CreateOrderReservation(Guid listingId, string buyerId, DeliveryMethod deliveryMethod)
    {
        lock (_dataStore.SyncRoot)
        {
            var listing = _dataStore.Listings.FirstOrDefault(l => l.Id == listingId);
            if (listing == null)
            {
                return (false, "Item listing was not found.", null);
            }

            if (listing.SellerId == buyerId)
            {
                return (false, "You cannot buy your own listing.", null);
            }

            // Strictly prevent race conditions: must be active
            if (listing.Status != ListingStatus.Active)
            {
                return (false, "This item has already been reserved or purchased by another buyer.", null);
            }

            // Atomically change listing status to Reserved
            listing.Status = ListingStatus.Reserved;

            var now = DateTime.UtcNow;
            var order = new Order
            {
                ListingId = listing.Id,
                BuyerId = buyerId,
                SellerId = listing.SellerId,
                Price = listing.Price,
                DeliveryMethod = deliveryMethod,
                Status = OrderStatus.Reserved,
                CreatedDate = now,
                ReservedUntil = now.AddHours(24) // 24-hour reservation window
            };

            _dataStore.Orders.Add(order);
            return (true, "Item reserved. Payment must be confirmed within 24 hours.", order);
        }
    }

    /// <summary>
    /// Escrow Step 2: Paid (held)
    /// Simulated payment held securely by platform escrow.
    /// </summary>
    public (bool Success, string Message) SimulatePayment(Guid orderId, string buyerId)
    {
        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return (false, "Order not found.");

            if (order.BuyerId != buyerId) return (false, "Only the buyer can simulate payment for this order.");

            if (order.Status != OrderStatus.Reserved)
            {
                return (false, $"Cannot pay for order in state {order.Status}.");
            }

            if (order.IsReservationExpired)
            {
                // Expired
                CancelReservationInternal(order);
                return (false, "Reservation expired before payment was submitted. The listing has returned to active.");
            }

            order.Status = OrderStatus.Paid;
            order.PaidDate = DateTime.UtcNow;

            return (true, "Simulated payment held in escrow. Seller has been notified to hand over or ship the item.");
        }
    }

    /// <summary>
    /// Escrow Step 3: Handed over / Shipped
    /// Seller marks the item as handed over or shipped. Starts 48-hour inspection window.
    /// </summary>
    public (bool Success, string Message) MarkHandedOverOrShipped(Guid orderId, string sellerId)
    {
        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return (false, "Order not found.");

            if (order.SellerId != sellerId) return (false, "Only the seller can mark this item handed over or shipped.");

            if (order.Status != OrderStatus.Paid)
            {
                return (false, $"Cannot mark handed over when order status is {order.Status}.");
            }

            var now = DateTime.UtcNow;
            order.Status = OrderStatus.HandedOverOrShipped;
            order.HandedOverDate = now;
            order.InspectionDeadline = now.AddHours(48); // 48-hour inspection window

            return (true, "Hand-over recorded. Buyer has 48 hours to inspect the item against the condition checklist.");
        }
    }

    /// <summary>
    /// Escrow Step 4 & 5: Confirmed -> Released
    /// Buyer verifies the item matches condition checklist and confirms receipt.
    /// Escrow funds immediately release to seller, and listing marks Sold.
    /// </summary>
    public (bool Success, string Message) ConfirmInspection(Guid orderId, string buyerId)
    {
        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return (false, "Order not found.");

            if (order.BuyerId != buyerId) return (false, "Only the buyer can confirm inspection.");

            if (order.Status != OrderStatus.HandedOverOrShipped)
            {
                return (false, $"Cannot confirm order in status {order.Status}.");
            }

            var now = DateTime.UtcNow;
            order.Status = OrderStatus.Confirmed;
            order.ConfirmedDate = now;

            // Automatically transition to Released
            order.Status = OrderStatus.Released;
            order.ReleasedDate = now;

            // Mark listing as Sold
            var listing = _dataStore.Listings.FirstOrDefault(l => l.Id == order.ListingId);
            if (listing != null)
            {
                listing.Status = ListingStatus.Sold;
            }

            return (true, "Item confirmed! Escrow funds have been successfully released to the seller.");
        }
    }

    /// <summary>
    /// Auto-release when 48h inspection window has expired without dispute.
    /// </summary>
    public (bool Success, string Message) CheckAutoRelease(Guid orderId)
    {
        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return (false, "Order not found.");

            if (order.Status == OrderStatus.HandedOverOrShipped && order.IsInspectionExpired)
            {
                var now = DateTime.UtcNow;
                order.Status = OrderStatus.Released;
                order.ReleasedDate = now;

                var listing = _dataStore.Listings.FirstOrDefault(l => l.Id == order.ListingId);
                if (listing != null)
                {
                    listing.Status = ListingStatus.Sold;
                }

                return (true, "48-hour inspection window expired with no dispute. Funds automatically released to seller.");
            }

            return (false, "Order inspection window has not expired yet.");
        }
    }

    public (bool Success, string Message) CancelOrder(Guid orderId, string userId)
    {
        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return (false, "Order not found.");

            if (order.BuyerId != userId && order.SellerId != userId)
            {
                return (false, "Unauthorized to cancel this order.");
            }

            if (order.Status != OrderStatus.Reserved)
            {
                return (false, "Orders can only be cancelled while in Reserved state prior to payment.");
            }

            CancelReservationInternal(order);
            return (true, "Reservation cancelled. Item is active again on the marketplace.");
        }
    }

    public void CheckAndExpireReservations()
    {
        lock (_dataStore.SyncRoot)
        {
            var expiredReserved = _dataStore.Orders
                .Where(o => o.Status == OrderStatus.Reserved && o.IsReservationExpired)
                .ToList();

            foreach (var order in expiredReserved)
            {
                CancelReservationInternal(order);
            }

            var expiredInspections = _dataStore.Orders
                .Where(o => o.Status == OrderStatus.HandedOverOrShipped && o.IsInspectionExpired)
                .ToList();

            foreach (var order in expiredInspections)
            {
                var now = DateTime.UtcNow;
                order.Status = OrderStatus.Released;
                order.ReleasedDate = now;

                var listing = _dataStore.Listings.FirstOrDefault(l => l.Id == order.ListingId);
                if (listing != null)
                {
                    listing.Status = ListingStatus.Sold;
                }
            }
        }
    }

    public (bool Success, string Message) SimulateExpireInspectionWindow(Guid orderId)
    {
        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return (false, "Order not found.");

            if (order.Status != OrderStatus.HandedOverOrShipped)
            {
                return (false, "Order is not currently in the inspection window.");
            }

            // Fast-forward inspection deadline to 1 second ago
            order.InspectionDeadline = DateTime.UtcNow.AddSeconds(-1);
            CheckOrderDeadlines(order);
            return (true, "Simulated 48-hour inspection window expiration. Funds automatically released.");
        }
    }

    public (bool Success, string Message) SimulateExpireReservationWindow(Guid orderId)
    {
        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return (false, "Order not found.");

            if (order.Status != OrderStatus.Reserved)
            {
                return (false, "Order is not currently in the reservation window.");
            }

            order.ReservedUntil = DateTime.UtcNow.AddSeconds(-1);
            CancelReservationInternal(order);
            return (true, "Simulated 24-hour reservation window expiration. Listing returned to active.");
        }
    }

    public (bool Success, string Message, Review? Review) SubmitReview(Guid orderId, string reviewerId, int rating, string comment)
    {
        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return (false, "Order not found.", null);

            if (order.Status != OrderStatus.Released && order.Status != OrderStatus.Resolved)
            {
                return (false, "Reviews can only be submitted after the transaction is completed.", null);
            }

            if (order.BuyerId != reviewerId && order.SellerId != reviewerId)
            {
                return (false, "Only parties to this order may leave a review.", null);
            }

            var existing = _dataStore.Reviews.FirstOrDefault(r => r.OrderId == orderId && r.ReviewerId == reviewerId);
            if (existing != null)
            {
                return (false, "You have already submitted a review for this transaction.", null);
            }

            var subjectId = (order.BuyerId == reviewerId) ? order.SellerId : order.BuyerId;

            var review = new Review
            {
                OrderId = orderId,
                ReviewerId = reviewerId,
                SubjectId = subjectId,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = comment.Trim(),
                CreatedDate = DateTime.UtcNow
            };

            _dataStore.Reviews.Add(review);
            return (true, "Review submitted successfully.", review);
        }
    }

    public Review? GetReviewForOrder(Guid orderId, string reviewerId)
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Reviews.FirstOrDefault(r => r.OrderId == orderId && r.ReviewerId == reviewerId);
        }
    }

    private void CancelReservationInternal(Order order)
    {
        order.Status = OrderStatus.Cancelled;
        order.CancelledDate = DateTime.UtcNow;

        var listing = _dataStore.Listings.FirstOrDefault(l => l.Id == order.ListingId);
        if (listing != null && listing.Status == ListingStatus.Reserved)
        {
            listing.Status = ListingStatus.Active;
        }
    }

    private void CheckOrderDeadlines(Order order)
    {
        if (order.Status == OrderStatus.Reserved && order.IsReservationExpired)
        {
            CancelReservationInternal(order);
        }
        else if (order.Status == OrderStatus.HandedOverOrShipped && order.IsInspectionExpired)
        {
            var now = DateTime.UtcNow;
            order.Status = OrderStatus.Released;
            order.ReleasedDate = now;

            var listing = _dataStore.Listings.FirstOrDefault(l => l.Id == order.ListingId);
            if (listing != null)
            {
                listing.Status = ListingStatus.Sold;
            }
        }
    }
}
