using Reboot.Models;

namespace Reboot.Services;

public class UserService : IUserService
{
    private readonly IMarketplaceDataStore _dataStore;
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public UserService(IMarketplaceDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public IReadOnlyList<User> GetAllUsers()
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Users.ToList();
        }
    }

    public User? GetUserById(string id)
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Users.FirstOrDefault(u => u.Id == id);
        }
    }

    public User? GetUserByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Users.FirstOrDefault(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public (bool Success, string? Error, User? User) Authenticate(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Incorrect email or password.", null);
        }

        lock (_dataStore.SyncRoot)
        {
            var user = _dataStore.Users.FirstOrDefault(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                return (false, "Incorrect email or password.", null);
            }

            // Check lockout
            if (user.IsLockedOut)
            {
                var remaining = (user.LockoutEnd!.Value - DateTime.UtcNow).Minutes + 1;
                return (false, $"Account is locked due to too many failed attempts. Try again in {remaining} minutes.", null);
            }

            // Verify password
            bool verified = PasswordHasher.VerifyPassword(password, user.PasswordHash);
            if (!verified)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                    return (false, "Account is locked due to too many failed attempts. Try again in 15 minutes.", null);
                }
                return (false, "Incorrect email or password.", null);
            }

            // Reset failed attempts on success
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            return (true, null, user);
        }
    }

    public (bool Success, string? Error, User? User) RegisterUser(string firstName, string lastName, string email, string city, string password)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return (false, "Please enter your first and last name.", null);
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return (false, "Please enter a valid email address.", null);
        }

        var (isValidPass, passError) = PasswordHasher.ValidatePasswordComplexity(password);
        if (!isValidPass)
        {
            return (false, passError, null);
        }

        lock (_dataStore.SyncRoot)
        {
            var existing = _dataStore.Users.FirstOrDefault(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                return (false, "An account with this email address already exists.", null);
            }

            var cleanFirst = firstName.Trim();
            var cleanLast = lastName.Trim();
            var display = $"{cleanFirst} {cleanLast[..1].ToUpperInvariant()}.";

            var user = new User
            {
                Id = "usr-" + Guid.NewGuid().ToString("N")[..8],
                FirstName = cleanFirst,
                LastName = cleanLast,
                DisplayName = display,
                Email = email.Trim().ToLowerInvariant(),
                PasswordHash = PasswordHasher.HashPassword(password),
                Location = string.IsNullOrWhiteSpace(city) ? "Cebu City" : city.Trim(),
                JoinedDate = DateTime.UtcNow,
                IsVerified = false,
                IsAdmin = false
            };

            _dataStore.Users.Add(user);
            return (true, null, user);
        }
    }

    public bool VerifyUser(string userId)
    {
        lock (_dataStore.SyncRoot)
        {
            var user = _dataStore.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.IsVerified = true;
                return true;
            }
            return false;
        }
    }

    public IReadOnlyList<User> GetUnverifiedUsers()
    {
        lock (_dataStore.SyncRoot)
        {
            return _dataStore.Users.Where(u => !u.IsVerified).ToList();
        }
    }

    public (bool Success, string? ResetToken) GeneratePasswordResetToken(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return (false, null);

        lock (_dataStore.SyncRoot)
        {
            var user = _dataStore.Users.FirstOrDefault(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                // Return simulated token anyway to avoid email enumeration
                return (true, Guid.NewGuid().ToString("N")[..12]);
            }

            var token = Guid.NewGuid().ToString("N")[..12];
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            return (true, token);
        }
    }

    public (bool Success, string? Error) ResetPasswordWithToken(string email, string token, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            return (false, "Invalid password reset token.");
        }

        var (isValidPass, passError) = PasswordHasher.ValidatePasswordComplexity(newPassword);
        if (!isValidPass)
        {
            return (false, passError);
        }

        lock (_dataStore.SyncRoot)
        {
            var user = _dataStore.Users.FirstOrDefault(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
            if (user == null || user.PasswordResetToken != token || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                return (false, "Invalid or expired password reset token.");
            }

            user.PasswordHash = PasswordHasher.HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            return (true, null);
        }
    }
}
