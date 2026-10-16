namespace Reboot.Models;

public class SellerReliabilityRecord
{
    public string SellerId { get; set; } = string.Empty;
    public int CompletedSalesCount { get; set; }
    public int DisputeCount { get; set; }
    public double DisputeRatePercent { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string AverageResponseTime { get; set; } = "< 1 hour";
    public DateTime MemberSince { get; set; }
    public bool HasSalesHistory => CompletedSalesCount > 0;

    /// <summary>
    /// Returns the plain facts formatted representation, e.g.:
    /// "14 completed sales, 1 dispute, 4.8 average, member since Mar 2025"
    /// or "No completed sales yet, member since Oct 2026"
    /// </summary>
    public string FormattedSummary
    {
        get
        {
            if (!HasSalesHistory)
            {
                return $"No completed sales yet, member since {MemberSince:MMM yyyy}";
            }

            var disputeText = DisputeCount == 1 ? "1 dispute" : $"{DisputeCount} disputes";
            var ratingText = AverageRating.HasValue ? $"{AverageRating.Value:F1} rating" : "No ratings yet";
            return $"{CompletedSalesCount} completed {(CompletedSalesCount == 1 ? "sale" : "sales")}, {disputeText}, {ratingText}, member since {MemberSince:MMM yyyy}";
        }
    }
}
