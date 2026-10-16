using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Reboot.Models;

namespace Reboot.Services;

/// <summary>
/// Scoped session service that reads the authenticated user from the active
/// AuthenticationStateProvider / HttpContext claims.
/// </summary>
public class CurrentUserService : ICurrentUserService, IDisposable
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMarketplaceDataStore _dataStore;

    public event Action? OnChange;

    public CurrentUserService(
        AuthenticationStateProvider authStateProvider,
        IHttpContextAccessor httpContextAccessor,
        IMarketplaceDataStore dataStore)
    {
        _authStateProvider = authStateProvider;
        _httpContextAccessor = httpContextAccessor;
        _dataStore = dataStore;

        _authStateProvider.AuthenticationStateChanged += OnAuthStateChanged;
    }

    private void OnAuthStateChanged(Task<AuthenticationState> task)
    {
        OnChange?.Invoke();
    }

    private string? GetCurrentUserId()
    {
        // 1. Check HttpContext (works during static SSR and initial HTTP request)
        var httpUser = _httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            var id = httpUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(id)) return id;
        }

        // 2. Check AuthenticationStateProvider (works during interactive circuits)
        try
        {
            var task = _authStateProvider.GetAuthenticationStateAsync();
            if (task.IsCompleted)
            {
                var principal = task.Result.User;
                if (principal.Identity?.IsAuthenticated == true)
                {
                    return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }
            }
            else
            {
                var principal = task.GetAwaiter().GetResult().User;
                if (principal.Identity?.IsAuthenticated == true)
                {
                    return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }
            }
        }
        catch
        {
            // fallback
        }

        return null;
    }

    public User? CurrentUser
    {
        get
        {
            var id = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(id)) return null;

            lock (_dataStore.SyncRoot)
            {
                return _dataStore.Users.FirstOrDefault(u => u.Id == id);
            }
        }
    }

    public bool IsSignedIn => CurrentUser != null;

    public bool IsAdmin => CurrentUser?.IsAdmin ?? false;

    public bool IsVerified => CurrentUser?.IsVerified ?? false;

    public string UserId => CurrentUser?.Id ?? string.Empty;

    public void Dispose()
    {
        _authStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;
    }
}
