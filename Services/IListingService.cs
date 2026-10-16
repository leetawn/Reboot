using Reboot.Models;

namespace Reboot.Services;

public class ListingFilterCriteria
{
    public string? SearchQuery { get; set; }
    public ListingCategory? Category { get; set; }
    public ItemGrade? Grade { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Location { get; set; }
    public bool VerifiedSellersOnly { get; set; }
    public bool WarrantyOfferedOnly { get; set; }
    public string SortBy { get; set; } = "newest"; // newest, price_asc, price_desc, grade
}

public interface IListingService
{
    IReadOnlyList<Listing> GetListings(ListingFilterCriteria? filter = null);
    IReadOnlyList<Listing> GetListingsBySeller(string sellerId);
    IReadOnlyList<Listing> GetAllListingsAdmin();
    Listing? GetListingById(Guid id);

    Listing CreateListing(Listing listing);
    bool UpdateListing(Listing listing);
    bool RemoveListing(Guid id, string requestedByUserId);

    ItemGrade CalculateGrade(ConditionReport report);
    List<ConditionCheck> GetDefaultChecksForCategory(ListingCategory category);
}
