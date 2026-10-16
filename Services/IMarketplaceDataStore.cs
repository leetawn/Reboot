using Reboot.Models;

namespace Reboot.Services;

public interface IMarketplaceDataStore
{
    List<User> Users { get; }
    List<Listing> Listings { get; }
    List<Offer> Offers { get; }
    List<Order> Orders { get; }
    List<Dispute> Disputes { get; }
    List<Review> Reviews { get; }

    object SyncRoot { get; }
}
