using Microsoft.EntityFrameworkCore;
using Reboot.Models;

namespace Reboot.Data;

public class RebootDbContext : DbContext
{
    public RebootDbContext(DbContextOptions<RebootDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Dispute> Disputes => Set<Dispute>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Id);
            b.HasIndex(u => u.Email).IsUnique();
            b.Ignore(u => u.IsLockedOut);
            b.Ignore(u => u.AvatarInitial);
        });

        // Configure Listing
        modelBuilder.Entity<Listing>(b =>
        {
            b.ToTable("listings");
            b.HasKey(l => l.Id);
            b.Property(l => l.Price).HasPrecision(18, 2);
            b.Property(l => l.MinimumOffer).HasPrecision(18, 2);
            b.Ignore(l => l.AllowsOffers);

            // ConditionReport as JSONB
            b.OwnsOne(l => l.ConditionReport, cr =>
            {
                cr.ToJson();
                cr.OwnsMany(c => c.Checks);
            });
        });

        // Configure Order
        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("orders");
            b.HasKey(o => o.Id);
            b.Property(o => o.Price).HasPrecision(18, 2);
            b.Ignore(o => o.IsMeetupAgreed);
            b.Ignore(o => o.IsReservationExpired);
            b.Ignore(o => o.IsInspectionExpired);
            b.Ignore(o => o.IsDispatchOverdue);
            b.Ignore(o => o.RemainingReservationTime);
            b.Ignore(o => o.RemainingInspectionTime);
            b.Ignore(o => o.RemainingDispatchTime);

            // Messages thread as JSONB
            b.OwnsMany(o => o.Messages, m =>
            {
                m.ToJson();
                m.Ignore(x => x.Id);
            });
        });

        // Configure Offer
        modelBuilder.Entity<Offer>(b =>
        {
            b.ToTable("offers");
            b.HasKey(o => o.Id);
            b.Property(o => o.Amount).HasPrecision(18, 2);
        });

        // Configure Dispute
        modelBuilder.Entity<Dispute>(b =>
        {
            b.ToTable("disputes");
            b.HasKey(d => d.Id);
            b.Property(d => d.PartialRefundAmount).HasPrecision(18, 2);
        });

        // Configure Review
        modelBuilder.Entity<Review>(b =>
        {
            b.ToTable("reviews");
            b.HasKey(r => r.Id);
        });
    }
}
