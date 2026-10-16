using Reboot.Models;

namespace Reboot.Services;

public class ListingService : IListingService
{
    private readonly IMarketplaceDataStore _dataStore;

    public ListingService(IMarketplaceDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public IReadOnlyList<Listing> GetListings(ListingFilterCriteria? filter = null)
    {
        lock (_dataStore.SyncRoot)
        {
            var query = _dataStore.Listings
                .Where(l => l.Status == ListingStatus.Active)
                .AsEnumerable();

            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
                {
                    var term = filter.SearchQuery.Trim().ToLowerInvariant();
                    query = query.Where(l =>
                        l.Title.ToLowerInvariant().Contains(term) ||
                        l.Brand.ToLowerInvariant().Contains(term) ||
                        l.Description.ToLowerInvariant().Contains(term));
                }

                if (filter.Category.HasValue)
                {
                    query = query.Where(l => l.Category == filter.Category.Value);
                }

                if (filter.Grade.HasValue)
                {
                    query = query.Where(l => l.Grade == filter.Grade.Value);
                }

                if (filter.MinPrice.HasValue)
                {
                    query = query.Where(l => l.Price >= filter.MinPrice.Value);
                }

                if (filter.MaxPrice.HasValue)
                {
                    query = query.Where(l => l.Price <= filter.MaxPrice.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.Location) && filter.Location != "All")
                {
                    query = query.Where(l =>
                    {
                        var seller = _dataStore.Users.FirstOrDefault(u => u.Id == l.SellerId);
                        return seller != null && seller.Location.Equals(filter.Location, StringComparison.OrdinalIgnoreCase);
                    });
                }

                if (filter.VerifiedSellersOnly)
                {
                    query = query.Where(l =>
                    {
                        var seller = _dataStore.Users.FirstOrDefault(u => u.Id == l.SellerId);
                        return seller?.IsVerified == true;
                    });
                }

                if (filter.WarrantyOfferedOnly)
                {
                    query = query.Where(l => l.WarrantyOffered.HasValue && l.WarrantyOffered.Value > 0);
                }

                query = filter.SortBy switch
                {
                    "price_asc" => query.OrderBy(l => l.Price),
                    "price_desc" => query.OrderByDescending(l => l.Price),
                    "grade" => query.OrderBy(l => l.Grade),
                    _ => query.OrderByDescending(l => l.CreatedDate)
                };
            }
            else
            {
                query = query.OrderByDescending(l => l.CreatedDate);
            }

            return query.ToList();
        }
    }

    public IReadOnlyList<Listing> GetListingsBySeller(string sellerId)
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Listings
                .Where(l => l.SellerId == sellerId)
                .OrderByDescending(l => l.CreatedDate)
                .ToList();
        }
    }

    public IReadOnlyList<Listing> GetAllListingsAdmin()
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Listings
                .OrderByDescending(l => l.CreatedDate)
                .ToList();
        }
    }

    public Listing? GetListingById(Guid id)
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Listings.FirstOrDefault(l => l.Id == id);
        }
    }

    public Listing CreateListing(Listing listing)
    {
        lock (_dataStore.SyncRoot)
        {
            // Calculate authoritative grade from checklist & defects
            listing.Grade = CalculateGrade(listing.ConditionReport);
            listing.CreatedDate = DateTime.UtcNow;
            listing.Status = ListingStatus.Active;
            if (listing.Photos.Count == 0)
            {
                listing.Photos.Add($"{listing.Category.ToString().ToLowerInvariant()}_default.jpg");
            }
            _dataStore.Listings.Add(listing);
            return listing;
        }
    }

    public bool UpdateListing(Listing listing)
    {
        lock (_dataStore.SyncRoot)
        {
            var existing = _dataStore.Listings.FirstOrDefault(l => l.Id == listing.Id);
            if (existing == null) return false;

            existing.Title = listing.Title;
            existing.Brand = listing.Brand;
            existing.Category = listing.Category;
            existing.Price = listing.Price;
            existing.MinimumOffer = listing.MinimumOffer;
            existing.Description = listing.Description;
            existing.ConditionReport = listing.ConditionReport;
            existing.Grade = CalculateGrade(listing.ConditionReport);
            existing.SerialNumber = listing.SerialNumber;
            existing.WarrantyOffered = listing.WarrantyOffered;
            existing.MeetupOrShipping = listing.MeetupOrShipping;
            return true;
        }
    }

    public bool RemoveListing(Guid id, string requestedByUserId)
    {
        lock (_dataStore.SyncRoot)
        {
            var listing = _dataStore.Listings.FirstOrDefault(l => l.Id == id);
            if (listing == null) return false;

            var user = _dataStore.Users.FirstOrDefault(u => u.Id == requestedByUserId);
            bool isAuthorized = user?.IsAdmin == true || listing.SellerId == requestedByUserId;
            if (!isAuthorized) return false;

            listing.Status = ListingStatus.Removed;
            return true;
        }
    }

    /// <summary>
    /// Documented Rule for Deriving Item Grade:
    /// - ForParts: Any critical failure (Booting, POST, Overheating, Power IC, Water damage) OR 3+ failed checks OR defect text flags non-working / parts.
    /// - Grade C: 2 non-critical failed checks OR noticeable cosmetic/hardware defect mentioned in KnownDefects (e.g. degraded battery, dent, missing part).
    /// - Grade B: Exactly 1 non-critical failed check OR minor cosmetic blemish noted in KnownDefects (light scratch, scuff, rub mark, shine).
    /// - Grade A: 100% of checks passed AND KnownDefects is empty or indicates no defects ("none", "clean", "pristine").
    /// </summary>
    public ItemGrade CalculateGrade(ConditionReport report)
    {
        if (report == null || report.Checks.Count == 0)
        {
            return ItemGrade.B;
        }

        var failedChecks = report.Checks.Where(c => !c.Passed).ToList();
        var failedCount = failedChecks.Count;
        var defectsLower = (report.KnownDefects ?? string.Empty).ToLowerInvariant().Trim();

        // 1. Check for critical failure terms in failed checks
        bool hasCriticalCheckFailure = failedChecks.Any(c =>
        {
            var name = c.Name.ToLowerInvariant();
            return name.Contains("boot") || name.Contains("post") || name.Contains("overheat") ||
                   name.Contains("charging port") || name.Contains("power");
        });

        // Check for severe defect keywords in free-text known defects
        bool defectFlagsForParts = defectsLower.Contains("for parts") ||
                                  defectsLower.Contains("not working") ||
                                  defectsLower.Contains("water damage") ||
                                  defectsLower.Contains("orange screen") ||
                                  defectsLower.Contains("blue screen") ||
                                  defectsLower.Contains("does not boot") ||
                                  defectsLower.Contains("broken");

        if (hasCriticalCheckFailure || failedCount >= 3 || defectFlagsForParts)
        {
            return ItemGrade.ForParts;
        }

        // 2. Check for Grade C criteria: 2 failed checks or noticeable defects
        bool defectFlagsGradeC = defectsLower.Contains("degraded") ||
                                 defectsLower.Contains("deep scratch") ||
                                 defectsLower.Contains("crack") ||
                                 defectsLower.Contains("dent") ||
                                 defectsLower.Contains("missing");

        if (failedCount >= 2 || defectFlagsGradeC)
        {
            return ItemGrade.C;
        }

        // 3. Check for Grade B criteria: 1 failed check or minor cosmetic note
        bool defectFlagsGradeB = !string.IsNullOrWhiteSpace(defectsLower) &&
                                 !defectsLower.Equals("none") &&
                                 !defectsLower.StartsWith("none.") &&
                                 !defectsLower.Contains("pristine") &&
                                 !defectsLower.Contains("clean");

        if (failedCount == 1 || defectFlagsGradeB)
        {
            return ItemGrade.B;
        }

        // 4. All checks passed and no defects -> Grade A
        return ItemGrade.A;
    }

    public List<ConditionCheck> GetDefaultChecksForCategory(ListingCategory category)
    {
        return category switch
        {
            ListingCategory.Console => new List<ConditionCheck>
            {
                new() { Name = "Boots properly", Passed = true },
                new() { Name = "All ports functional (HDMI, USB, Power)", Passed = true },
                new() { Name = "Disc drive / cartridge slot reads", Passed = true },
                new() { Name = "No overheating after 30 min", Passed = true },
                new() { Name = "Controller pairs wirelessly", Passed = true }
            },
            ListingCategory.Controller => new List<ConditionCheck>
            {
                new() { Name = "No stick drift", Passed = true },
                new() { Name = "All buttons and triggers register", Passed = true },
                new() { Name = "Battery holds charge", Passed = true },
                new() { Name = "Wireless pairing works", Passed = true },
                new() { Name = "Vibration / haptics work", Passed = true }
            },
            ListingCategory.Handheld => new List<ConditionCheck>
            {
                new() { Name = "Screen has no dead pixels or deep scratches", Passed = true },
                new() { Name = "Buttons and thumbsticks responsive", Passed = true },
                new() { Name = "Battery health >80%", Passed = true },
                new() { Name = "Speakers and headphone jack work", Passed = true },
                new() { Name = "Charging port functional", Passed = true }
            },
            ListingCategory.PCPart => new List<ConditionCheck>
            {
                new() { Name = "POSTs without error", Passed = true },
                new() { Name = "Stress test passed without artifacts or crash", Passed = true },
                new() { Name = "Fan(s) spin quietly without bearing rattle", Passed = true },
                new() { Name = "All display outputs / connectors work", Passed = true },
                new() { Name = "No thermal throttling under normal load", Passed = true }
            },
            ListingCategory.Laptop => new List<ConditionCheck>
            {
                new() { Name = "Boots to OS smoothly", Passed = true },
                new() { Name = "Display clear with no lines or dead pixels", Passed = true },
                new() { Name = "Keyboard and touchpad responsive", Passed = true },
                new() { Name = "Battery holds charge on unplug", Passed = true },
                new() { Name = "Wi-Fi and Bluetooth connect", Passed = true }
            },
            ListingCategory.Accessory => new List<ConditionCheck>
            {
                new() { Name = "Connects and operates reliably", Passed = true },
                new() { Name = "Cable / plug has no fraying or damage", Passed = true },
                new() { Name = "All dials / switches work", Passed = true },
                new() { Name = "Cosmetic surface is clean", Passed = true }
            },
            _ => new List<ConditionCheck>()
        };
    }
}
