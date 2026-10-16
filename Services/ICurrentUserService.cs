using Reboot.Models;

namespace Reboot.Services;

public interface ICurrentUserService
{
    User? CurrentUser { get; }
    bool IsSignedIn { get; }
    bool IsAdmin { get; }
    bool IsVerified { get; }
    string UserId { get; }

    event Action? OnChange;
}
