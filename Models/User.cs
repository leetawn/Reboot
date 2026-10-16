namespace Reboot.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Location { get; set; } = "Cebu City";
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
    public bool IsVerified { get; set; } = false;
    public bool IsAdmin { get; set; } = false;

    // Security & lockout fields
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;

    public string? AvatarInitial => string.IsNullOrWhiteSpace(DisplayName) ? "U" : DisplayName[..1].ToUpperInvariant();
}
