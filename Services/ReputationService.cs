using Reboot.Models;

namespace Reboot.Services;

public class ReputationService : IReputationService
{
    private readonly IMarketplaceDataStore _dataStore;

    public ReputationService(IMarketplaceDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public SellerReliabilityRecord GetSellerRecord(string sellerId)
    {
        lock (_dataStore.SyncRoot)
        {
            var user = _dataStore.Users.FirstOrDefault(u => u.Id == sellerId);
            var memberSince = user?.JoinedDate ?? DateTime.UtcNow;

            // 1. Completed sales count
            var completedOrders = _dataStore.Orders
                .Where(o => o.SellerId == sellerId && (o.Status == OrderStatus.Released || o.Status == OrderStatus.Resolved))
                .ToList();

            int completedSalesCount = completedOrders.Count;

            // 2. Dispute count
            var disputes = _dataStore.Disputes
                .Where(d =>
                {
                    var order = _dataStore.Orders.FirstOrDefault(o => o.Id == d.OrderId);
                    return order != null && order.SellerId == sellerId;
                })
                .ToList();

            int disputeCount = disputes.Count;
            double disputeRatePercent = completedSalesCount > 0
                ? Math.Round(((double)disputeCount / (completedSalesCount + disputeCount)) * 100.0, 1)
                : 0.0;

            // 3. Valid reviews (excluding wash trading and applying time weights)
            var validReviews = GetValidReviewsForSellerInternal(sellerId);

            double? averageRating = null;
            if (validReviews.Count > 0)
            {
                var now = DateTime.UtcNow;
                double totalWeightedRating = 0;
                double totalWeights = 0;

                foreach (var review in validReviews)
                {
                    // Weight recent activity (within 60 days) 2x higher than older transactions
                    double weight = (now - review.CreatedDate).TotalDays <= 60 ? 2.0 : 1.0;
                    totalWeightedRating += review.Rating * weight;
                    totalWeights += weight;
                }

                averageRating = Math.Round(totalWeightedRating / totalWeights, 1);
            }

            // 4. Response time heuristic based on sales and activity
            string responseTime = completedSalesCount switch
            {
                >= 10 => "< 1 hour",
                >= 3 => "Within 2 hours",
                >= 1 => "Within 4 hours",
                _ => "Usually within 6 hours"
            };

            return new SellerReliabilityRecord
            {
                SellerId = sellerId,
                CompletedSalesCount = completedSalesCount,
                DisputeCount = disputeCount,
                DisputeRatePercent = disputeRatePercent,
                AverageRating = averageRating,
                ReviewCount = validReviews.Count,
                AverageResponseTime = responseTime,
                MemberSince = memberSince
            };
        }
    }

    public IReadOnlyList<Review> GetValidReviewsForSeller(string sellerId)
    {
        lock (_dataStore.SyncRoot)
        {
            return GetValidReviewsForSellerInternal(sellerId);
        }
    }

    /// <summary>
    /// Filters out circular / wash trading reviews between accounts that only transact with each other.
    /// </summary>
    private List<Review> GetValidReviewsForSellerInternal(string sellerId)
    {
        var rawReviews = _dataStore.Reviews
            .Where(r => r.SubjectId == sellerId)
            .OrderByDescending(r => r.CreatedDate)
            .ToList();

        if (rawReviews.Count == 0) return new List<Review>();

        // Identify counterparty trading relationships
        // If reviewer A has only ever traded with seller B, and seller B has only ever traded with A, ignore.
        var validReviews = new List<Review>();

        foreach (var review in rawReviews)
        {
            var reviewerId = review.ReviewerId;

            // All orders involving reviewer
            var reviewerCounterparties = _dataStore.Orders
                .Where(o => o.BuyerId == reviewerId || o.SellerId == reviewerId)
                .Select(o => o.BuyerId == reviewerId ? o.SellerId : o.BuyerId)
                .Distinct()
                .ToHashSet();

            // All orders involving seller
            var sellerCounterparties = _dataStore.Orders
                .Where(o => o.BuyerId == sellerId || o.SellerId == sellerId)
                .Select(o => o.BuyerId == sellerId ? o.SellerId : o.BuyerId)
                .Distinct()
                .ToHashSet();

            // Circular trading condition: both users have traded exclusively with each other
            bool isExclusivePair = reviewerCounterparties.Count == 1 &&
                                   reviewerCounterparties.Contains(sellerId) &&
                                   sellerCounterparties.Count == 1 &&
                                   sellerCounterparties.Contains(reviewerId);

            if (!isExclusivePair)
            {
                validReviews.Add(review);
            }
        }

        return validReviews;
    }
}
