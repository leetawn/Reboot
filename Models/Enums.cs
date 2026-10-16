namespace Reboot.Models;

public enum ListingCategory
{
    Console,
    Controller,
    Handheld,
    PCPart,
    Laptop,
    Accessory
}

public enum ItemGrade
{
    A,         // Like New / Mint: All checklist passed, zero defects
    B,         // Good: Minor cosmetic wear, 1 non-critical check failed or minor blemish
    C,         // Fair: Noticeable cosmetic wear, multiple minor check failures, fully functional
    ForParts   // Not functional / Severe defects / Needs repair
}

public enum ListingStatus
{
    Active,
    Reserved,
    Sold,
    Removed
}

public enum DeliveryOption
{
    Meetup,
    Shipping,
    Both
}

public enum DeliveryMethod
{
    Meetup,
    Shipping
}

public enum OfferStatus
{
    Pending,
    Accepted,
    Declined,
    Expired
}

public enum OrderStatus
{
    Reserved,               // Listing reserved by buyer for 24h
    Paid,                   // Simulated payment held in escrow
    HandedOverOrShipped,    // Seller marked handed over or shipped, 48h inspection window begins
    Confirmed,              // Buyer confirmed item is good
    Released,               // Platform funds released to seller (Completed)
    Cancelled,              // Reservation expired or cancelled
    Disputed,               // Buyer opened dispute during inspection window
    Resolved                // Dispute resolved by admin (refund, release, or partial)
}

public enum DisputeStatus
{
    Open,
    ResolvedRefund,
    ResolvedRelease,
    ResolvedPartial
}
