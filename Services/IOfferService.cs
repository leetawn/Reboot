using Reboot.Models;

namespace Reboot.Services;

public interface IOfferService
{
    IReadOnlyList<Offer> GetOffersForListing(Guid listingId);
    IReadOnlyList<Offer> GetIncomingOffersForSeller(string sellerId);
    IReadOnlyList<Offer> GetOffersByBuyer(string buyerId);
    Offer? GetOfferById(Guid offerId);

    (bool Success, string Message, Offer? Offer) CreateOffer(Guid listingId, string buyerId, decimal amount);
    (bool Success, string Message, Order? CreatedOrder) AcceptOffer(Guid offerId, string sellerId);
    (bool Success, string Message) DeclineOffer(Guid offerId, string sellerId);
}
