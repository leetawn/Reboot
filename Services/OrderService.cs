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

            if (order.BuyerId != buyerId) return (false, "Only the buyer can submit payment for this order.");

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
            order.DispatchDeadline = DateTime.UtcNow.AddHours(72); // 72-hour window for seller to dispatch

            var buyer = _dataStore.Users.FirstOrDefault(u => u.Id == buyerId);
            order.Messages.Add(new OrderMessage
            {
                SenderId = "system",
                SenderName = "Reboot Escrow",
                Content = $"Payment of {AppConfig.FormatPrice(order.Price)} confirmed and held in escrow. Seller has 72 hours to hand over or dispatch the item.",
                Timestamp = DateTime.UtcNow,
                IsSystem = true
            });

            return (true, "Payment secured in escrow. Seller has 72 hours to hand over or ship the item.");
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

            // Override inspection deadline to 1 second ago
            order.InspectionDeadline = DateTime.UtcNow.AddSeconds(-1);
            CheckOrderDeadlines(order);
            return (true, "48-hour inspection window expired. Funds automatically released to seller.");
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
            return (true, "24-hour reservation window expired. Listing returned to active.");
        }
    }

    public (bool Success, string Message) ProposeMeetup(Guid orderId, string userId, string location, string? time, string? notes)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return (false, "Please specify a meetup location.");
        }

        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return (false, "Order not found.");

            if (order.BuyerId != userId && order.SellerId != userId)
            {
                return (false, "Only parties to this order can coordinate meetup details.");
            }

            order.MeetupLocation = location.Trim();
            order.MeetupTime = string.IsNullOrWhiteSpace(time) ? null : time.Trim();
            order.MeetupNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

            bool isBuyer = order.BuyerId == userId;
            order.MeetupConfirmedByBuyer = isBuyer;
            order.MeetupConfirmedBySeller = !isBuyer;

            var user = _dataStore.Users.FirstOrDefault(u => u.Id == userId);
            var roleName = isBuyer ? "Buyer" : "Seller";
            var timeDesc = !string.IsNullOrWhiteSpace(time) ? $" on {time.Trim()}" : "";
            order.Messages.Add(new OrderMessage
            {
                SenderId = userId,
                SenderName = user?.DisplayName ?? roleName,
                Content = $"Proposed meetup at {location.Trim()}{timeDesc}. Awaiting counterparty confirmation.",
                Timestamp = DateTime.UtcNow,
                IsSystem = false
            });

            return (true, "Meetup details updated. Counterparty has been prompted to confirm.");
        }
    }

    public (bool Success, string Message) ConfirmMeetup(Guid orderId, string userId)
    {
        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return (false, "Order not found.");

            if (order.BuyerId != userId && order.SellerId != userId)
            {
                return (false, "Only parties to this order can confirm meetup details.");
            }

            if (string.IsNullOrWhiteSpace(order.MeetupLocation))
            {
                return (false, "No meetup location has been proposed yet.");
            }

            if (order.BuyerId == userId)
            {
                order.MeetupConfirmedByBuyer = true;
            }
            else
            {
                order.MeetupConfirmedBySeller = true;
            }

            var user = _dataStore.Users.FirstOrDefault(u => u.Id == userId);
            var roleName = order.BuyerId == userId ? "Buyer" : "Seller";

            if (order.IsMeetupAgreed)
            {
                order.Messages.Add(new OrderMessage
                {
                    SenderId = "system",
                    SenderName = "Reboot Escrow",
                    Content = $"Both buyer and seller have agreed to meet at: {order.MeetupLocation}{(string.IsNullOrWhiteSpace(order.MeetupTime) ? "" : $" ({order.MeetupTime})")}.",
                    Timestamp = DateTime.UtcNow,
                    IsSystem = true
                });
                return (true, $"Meetup location confirmed by both parties! Location: {order.MeetupLocation}.");
            }
            else
            {
                order.Messages.Add(new OrderMessage
                {
                    SenderId = userId,
                    SenderName = user?.DisplayName ?? roleName,
                    Content = $"Confirmed agreed meetup at {order.MeetupLocation}.",
                    Timestamp = DateTime.UtcNow,
                    IsSystem = false
                });
                return (true, "You confirmed the meetup spot. Awaiting counterparty confirmation.");
            }
        }
    }

    public (bool Success, string Message) UpdateShippingDetails(Guid orderId, string sellerId, string courierName, string trackingNumber)
    {
        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return (false, "Order not found.");

            if (order.SellerId != sellerId)
            {
                return (false, "Only the seller can update shipping/tracking details.");
            }

            order.CourierName = courierName.Trim();
            order.TrackingNumber = trackingNumber.Trim();

            order.Messages.Add(new OrderMessage
            {
                SenderId = sellerId,
                SenderName = "Seller",
                Content = $"Shipping dispatch info updated: {courierName.Trim()} — Tracking #{trackingNumber.Trim()}.",
                Timestamp = DateTime.UtcNow,
                IsSystem = false
            });

            return (true, "Courier tracking details saved.");
        }
    }

    public (bool Success, string Message) AddOrderMessage(Guid orderId, string senderId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return (false, "Message cannot be empty.");
        }

        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return (false, "Order not found.");

            if (order.BuyerId != senderId && order.SellerId != senderId)
            {
                return (false, "Only parties to this order may post coordination notes.");
            }

            var user = _dataStore.Users.FirstOrDefault(u => u.Id == senderId);
            var roleName = order.BuyerId == senderId ? "Buyer" : "Seller";

            order.Messages.Add(new OrderMessage
            {
                SenderId = senderId,
                SenderName = user?.DisplayName ?? roleName,
                Content = message.Trim(),
                Timestamp = DateTime.UtcNow,
                IsSystem = false
            });

            return (true, "Message posted to order thread.");
        }
    }

    public (bool Success, string Message) ClaimUnresponsiveSellerRefund(Guid orderId, string buyerId)
    {
        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return (false, "Order not found.");

            if (order.BuyerId != buyerId)
            {
                return (false, "Only the buyer can request an unresponsive seller refund.");
            }

            if (order.Status != OrderStatus.Paid)
            {
                return (false, $"Cannot claim refund for order in status {order.Status}.");
            }

            if (!order.IsDispatchOverdue)
            {
                var remaining = order.DispatchDeadline.HasValue ? order.DispatchDeadline.Value - DateTime.UtcNow : TimeSpan.FromHours(72);
                return (false, $"Seller dispatch deadline has not expired yet. {Math.Max(1, (int)remaining.TotalHours)} hours remaining.");
            }

            var now = DateTime.UtcNow;
            order.Status = OrderStatus.Cancelled;
            order.CancelledDate = now;

            // Return listing to active
            var listing = _dataStore.Listings.FirstOrDefault(l => l.Id == order.ListingId);
            if (listing != null && listing.Status == ListingStatus.Reserved)
            {
                listing.Status = ListingStatus.Active;
            }

            order.Messages.Add(new OrderMessage
            {
                SenderId = "system",
                SenderName = "Reboot Escrow",
                Content = $"Seller did not hand over or dispatch within the 72-hour window. Order cancelled; 100% full refund ({AppConfig.FormatPrice(order.Price)}) returned to buyer.",
                Timestamp = now,
                IsSystem = true
            });

            return (true, $"72-hour seller dispatch deadline expired. Order cancelled and full refund of {AppConfig.FormatPrice(order.Price)} returned to your account.");
        }
    }

    public (bool Success, string Message) SimulateExpireDispatchWindow(Guid orderId)
    {
        lock (_dataStore.SyncRoot)
        {
            var order = _dataStore.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return (false, "Order not found.");

            if (order.Status != OrderStatus.Paid)
            {
                return (false, "Order is not in Paid status.");
            }

            order.DispatchDeadline = DateTime.UtcNow.AddSeconds(-1);
            return (true, "72-hour dispatch window expired. Buyer can now claim an immediate refund.");
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
