using Reboot.Models;

namespace Reboot.Services;

public interface IReputationService
{
    SellerReliabilityRecord GetSellerRecord(string sellerId);
    IReadOnlyList<Review> GetValidReviewsForSeller(string sellerId);
}
