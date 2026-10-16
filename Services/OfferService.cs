using Reboot.Models;

namespace Reboot.Services;

public class OfferService : IOfferService
{
    private readonly IMarketplaceDataStore _dataStore;

    public OfferService(IMarketplaceDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public IReadOnlyList<Offer> GetOffersForListing(Guid listingId)
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Offers
                .Where(o => o.ListingId == listingId)
                .OrderByDescending(o => o.CreatedDate)
                .ToList();
        }
    }

    public IReadOnlyList<Offer> GetIncomingOffersForSeller(string sellerId)
    {
        lock (_dataStore.SyncRoot)
        {
            var sellerListingIds = _dataStore.Listings
                .Where(l => l.SellerId == sellerId)
                .Select(l => l.Id)
                .ToHashSet();

            return _dataStore.Offers
                .Where(o => sellerListingIds.Contains(o.ListingId) && o.Status == OfferStatus.Pending)
                .OrderByDescending(o => o.CreatedDate)
                .ToList();
        }
    }

    public IReadOnlyList<Offer> GetOffersByBuyer(string buyerId)
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Offers
                .Where(o => o.BuyerId == buyerId)
                .OrderByDescending(o => o.CreatedDate)
                .ToList();
        }
    }

    public Offer? GetOfferById(Guid offerId)
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Offers.FirstOrDefault(o => o.Id == offerId);
        }
    }

    public (bool Success, string Message, Offer? Offer) CreateOffer(Guid listingId, string buyerId, decimal amount)
    {
        lock (_dataStore.SyncRoot)
        {
            var listing = _dataStore.Listings.FirstOrDefault(l => l.Id == listingId);
            if (listing == null)
            {
                return (false, "Listing not found.", null);
            }

            if (listing.Status != ListingStatus.Active)
            {
                return (false, "This item is no longer available for offers.", null);
            }

            if (listing.SellerId == buyerId)
            {
                return (false, "You cannot make an offer on your own listing.", null);
            }

            if (!listing.MinimumOffer.HasValue)
            {
                return (false, "This seller is not accepting offers for this item.", null);
            }

            if (amount < listing.MinimumOffer.Value)
            {
                return (false, $"Offer must be at least {AppConfig.FormatPrice(listing.MinimumOffer.Value)}.", null);
            }

            if (amount >= listing.Price)
            {
                return (false, $"Offer cannot exceed or equal list price {AppConfig.FormatPrice(listing.Price)}. Use Buy Now instead.", null);
            }

            var offer = new Offer
            {
                ListingId = listingId,
                BuyerId = buyerId,
                Amount = amount,
                Status = OfferStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            _dataStore.Offers.Add(offer);
            return (true, "Offer submitted to seller.", offer);
        }
    }

    public (bool Success, string Message, Order? CreatedOrder) AcceptOffer(Guid offerId, string sellerId)
    {
        lock (_dataStore.SyncRoot)
        {
            var offer = _dataStore.Offers.FirstOrDefault(o => o.Id == offerId);
            if (offer == null)
            {
                return (false, "Offer not found.", null);
            }

            var listing = _dataStore.Listings.FirstOrDefault(l => l.Id == offer.ListingId);
            if (listing == null || listing.SellerId != sellerId)
            {
                return (false, "Unauthorized to accept this offer.", null);
            }

            if (listing.Status != ListingStatus.Active)
            {
                return (false, "Listing is no longer active.", null);
            }

            offer.Status = OfferStatus.Accepted;

            // Enforce atomic reservation: listing status -> Reserved
            listing.Status = ListingStatus.Reserved;

            // Create escrow order in Reserved state
            var order = new Order
            {
                ListingId = listing.Id,
                BuyerId = offer.BuyerId,
                SellerId = listing.SellerId,
                Price = offer.Amount,
                DeliveryMethod = listing.MeetupOrShipping == DeliveryOption.Shipping ? DeliveryMethod.Shipping : DeliveryMethod.Meetup,
                Status = OrderStatus.Reserved,
                CreatedDate = DateTime.UtcNow,
                ReservedUntil = DateTime.UtcNow.AddHours(24)
            };
            _dataStore.Orders.Add(order);

            // Expire all other pending offers on this listing
            foreach (var otherOffer in _dataStore.Offers.Where(o => o.ListingId == listing.Id && o.Id != offerId && o.Status == OfferStatus.Pending))
            {
                otherOffer.Status = OfferStatus.Expired;
            }

            return (true, "Offer accepted. Item is now reserved for the buyer.", order);
        }
    }

    public (bool Success, string Message) DeclineOffer(Guid offerId, string sellerId)
    {
        lock (_dataStore.SyncRoot)
        {
            var offer = _dataStore.Offers.FirstOrDefault(o => o.Id == offerId);
            if (offer == null)
            {
                return (false, "Offer not found.");
            }

            var listing = _dataStore.Listings.FirstOrDefault(l => l.Id == offer.ListingId);
            if (listing == null || listing.SellerId != sellerId)
            {
                return (false, "Unauthorized to decline this offer.");
            }

            offer.Status = OfferStatus.Declined;
            return (true, "Offer declined.");
        }
    }
}
