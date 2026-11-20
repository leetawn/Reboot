using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Reboot.Data;
using Reboot.Models;

namespace Reboot.Services;

public class PostgresDataStore : IMarketplaceDataStore
{
    public List<User> Users { get; } = new();
    public List<Listing> Listings { get; } = new();
    public List<Offer> Offers { get; } = new();
    public List<Order> Orders { get; } = new();
    public List<Dispute> Disputes { get; } = new();
    public List<Review> Reviews { get; } = new();

    public object SyncRoot { get; } = new();

    private readonly IDbContextFactory<RebootDbContext> _contextFactory;
    private readonly ILogger<PostgresDataStore> _logger;
    private readonly IConfiguration _configuration;
    private bool _databaseConnected = false;

    public PostgresDataStore(
        IDbContextFactory<RebootDbContext> contextFactory,
        ILogger<PostgresDataStore> logger,
        IConfiguration configuration)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _configuration = configuration;

        InitializeDatabase();
    }

    public bool IsDatabaseConnected => _databaseConnected;

    private void InitializeDatabase()
    {
        var rawConn = _configuration.GetConnectionString("Supabase");
        if (string.IsNullOrWhiteSpace(rawConn) || rawConn.Contains("[YOUR-DATABASE-PASSWORD]", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("⚠️ Supabase connection string is not configured with your actual database password. Running in In-Memory mode. Replace '[YOUR-DATABASE-PASSWORD]' in appsettings.Development.json to connect to Supabase.");
            _databaseConnected = false;
            return;
        }

        try
        {
            using var db = _contextFactory.CreateDbContext();

            // Check if tables already exist in Supabase before attempting creation
            var conn = db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
            {
                conn.Open();
            }
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'users');";
            var tablesExist = (bool)(cmd.ExecuteScalar() ?? false);

            if (!tablesExist)
            {
                var databaseCreator = db.Database.GetService<IRelationalDatabaseCreator>();
                try
                {
                    databaseCreator.CreateTables();
                    _logger.LogInformation("Database tables created successfully in Supabase.");
                }
                catch (PostgresException pex) when (pex.SqlState == "42P07")
                {
                    // Tables already exist
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("CreateTables notice: {Message}", ex.Message);
                }
            }

            // Ensure an admin user exists if the database is brand new
            if (!db.Users.Any())
            {
                var defaultHash = PasswordHasher.HashPassword("Password123!");
                var admin = new User
                {
                    Id = "usr-admin",
                    FirstName = "Marco",
                    LastName = "Tan",
                    DisplayName = "Marco T.",
                    Email = "admin@reboot.ph",
                    PasswordHash = defaultHash,
                    Location = "Cebu City",
                    JoinedDate = DateTime.UtcNow,
                    IsVerified = true,
                    IsAdmin = true
                };
                db.Users.Add(admin);
                db.SaveChanges();
            }

            _logger.LogInformation("✅ Connected to Supabase PostgreSQL! Loading data...");
            lock (SyncRoot)
            {
                Users.Clear();
                Users.AddRange(db.Users.AsNoTracking().ToList());

                Listings.Clear();
                Listings.AddRange(db.Listings.AsNoTracking().ToList());

                Offers.Clear();
                Offers.AddRange(db.Offers.AsNoTracking().ToList());

                Orders.Clear();
                Orders.AddRange(db.Orders.AsNoTracking().ToList());

                Disputes.Clear();
                Disputes.AddRange(db.Disputes.AsNoTracking().ToList());

                Reviews.Clear();
                Reviews.AddRange(db.Reviews.AsNoTracking().ToList());
            }
            _databaseConnected = true;
            _logger.LogInformation("Loaded {UserCount} users, {ListingCount} listings, and {OrderCount} orders from Supabase.", Users.Count, Listings.Count, Orders.Count);
        }
        catch (Exception ex)
        {
            _databaseConnected = false;
            _logger.LogWarning("⚠️ Could not connect to Supabase PostgreSQL: {Message}. Operating in In-Memory mode.", ex.Message);
        }
    }

    public void SaveChanges()
    {
        if (!_databaseConnected) return;

        lock (SyncRoot)
        {
            try
            {
                using var db = _contextFactory.CreateDbContext();

                // 1. Users
                var dbUsers = db.Users.ToDictionary(u => u.Id);
                foreach (var u in Users)
                {
                    if (dbUsers.TryGetValue(u.Id, out var existing))
                    {
                        db.Entry(existing).CurrentValues.SetValues(u);
                    }
                    else
                    {
                        db.Users.Add(u);
                    }
                }

                // 2. Listings
                var currentListingIds = Listings.Select(l => l.Id).ToHashSet();
                var dbListings = db.Listings.ToDictionary(l => l.Id);

                // Delete listings that were removed
                var listingsToDelete = dbListings.Values.Where(dbItem => !currentListingIds.Contains(dbItem.Id)).ToList();
                if (listingsToDelete.Count > 0)
                {
                    foreach (var item in listingsToDelete)
                    {
                        var relatedOffers = db.Offers.Where(o => o.ListingId == item.Id);
                        db.Offers.RemoveRange(relatedOffers);
                        var relatedOrders = db.Orders.Where(o => o.ListingId == item.Id);
                        db.Orders.RemoveRange(relatedOrders);
                        db.Listings.Remove(item);
                    }
                }

                foreach (var l in Listings)
                {
                    if (dbListings.TryGetValue(l.Id, out var existing))
                    {
                        db.Entry(existing).CurrentValues.SetValues(l);
                        existing.Photos = new List<string>(l.Photos);
                        existing.ConditionReport = l.ConditionReport;
                    }
                    else
                    {
                        db.Listings.Add(l);
                    }
                }

                // 3. Offers
                var dbOffers = db.Offers.ToDictionary(o => o.Id);
                foreach (var o in Offers)
                {
                    if (dbOffers.TryGetValue(o.Id, out var existing))
                    {
                        db.Entry(existing).CurrentValues.SetValues(o);
                    }
                    else
                    {
                        db.Offers.Add(o);
                    }
                }

                // 4. Orders
                var dbOrders = db.Orders.ToDictionary(o => o.Id);
                foreach (var o in Orders)
                {
                    if (dbOrders.TryGetValue(o.Id, out var existing))
                    {
                        db.Entry(existing).CurrentValues.SetValues(o);
                        existing.Messages = new List<OrderMessage>(o.Messages);
                    }
                    else
                    {
                        db.Orders.Add(o);
                    }
                }

                // 5. Disputes
                var dbDisputes = db.Disputes.ToDictionary(d => d.Id);
                foreach (var d in Disputes)
                {
                    if (dbDisputes.TryGetValue(d.Id, out var existing))
                    {
                        db.Entry(existing).CurrentValues.SetValues(d);
                    }
                    else
                    {
                        db.Disputes.Add(d);
                    }
                }

                // 6. Reviews
                var dbReviews = db.Reviews.ToDictionary(r => r.Id);
                foreach (var r in Reviews)
                {
                    if (dbReviews.TryGetValue(r.Id, out var existing))
                    {
                        db.Entry(existing).CurrentValues.SetValues(r);
                    }
                    else
                    {
                        db.Reviews.Add(r);
                    }
                }

                db.SaveChanges();
                _lastReloadTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist changes to Supabase PostgreSQL.");
            }
        }
    }

    private DateTime _lastReloadTime = DateTime.MinValue;

    public void EnsureFreshData()
    {
        if (!_databaseConnected) return;

        // If more than 1 second has passed since last reload, fetch fresh from Supabase
        if ((DateTime.UtcNow - _lastReloadTime).TotalSeconds >= 1)
        {
            Reload();
        }
    }

    public void Reload()
    {
        if (!_databaseConnected) return;

        lock (SyncRoot)
        {
            try
            {
                using var db = _contextFactory.CreateDbContext();
                Users.Clear();
                Users.AddRange(db.Users.AsNoTracking().ToList());

                Listings.Clear();
                Listings.AddRange(db.Listings.AsNoTracking().ToList());

                Offers.Clear();
                Offers.AddRange(db.Offers.AsNoTracking().ToList());

                Orders.Clear();
                Orders.AddRange(db.Orders.AsNoTracking().ToList());

                Disputes.Clear();
                Disputes.AddRange(db.Disputes.AsNoTracking().ToList());

                Reviews.Clear();
                Reviews.AddRange(db.Reviews.AsNoTracking().ToList());

                _lastReloadTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload data from Supabase.");
            }
        }
    }
}
