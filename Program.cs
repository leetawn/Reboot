using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Reboot.Components;
using Reboot.Models;
using Reboot.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB for image uploads
});

// Cookie Authentication (14-day persistent sliding expiration)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "reboot_auth";
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// In-Memory Data Store (Singleton, realistic platform state)
builder.Services.AddSingleton<IMarketplaceDataStore, InMemoryDataStore>();

// Current User Session (Scoped per connection, resolves from ClaimsPrincipal)
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Domain Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IListingService, ListingService>();
builder.Services.AddScoped<IOfferService, OfferService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IDisputeService, DisputeService>();
builder.Services.AddScoped<IReputationService, ReputationService>();
builder.Services.AddScoped<IToastService, ToastService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Minimal API Authentication Endpoints
app.MapPost("/auth/login", async (HttpContext context, IUserService userService) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    var result = userService.Authenticate(email, password);
    if (!result.Success)
    {
        var error = result.Error ?? "Incorrect email or password.";
        var redirectUrl = $"/login?error={Uri.EscapeDataString(error)}";
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            redirectUrl += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
        }
        return Results.Redirect(redirectUrl);
    }

    var user = result.User!;
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Name, user.DisplayName),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
    });

    if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
    {
        return Results.Redirect(returnUrl);
    }

    return Results.Redirect("/browse");
}).DisableAntiforgery();

app.MapPost("/auth/register", async (HttpContext context, IUserService userService) =>
{
    var form = await context.Request.ReadFormAsync();
    var firstName = form["firstName"].ToString();
    var lastName = form["lastName"].ToString();
    var email = form["email"].ToString();
    var city = form["city"].ToString();
    var password = form["password"].ToString();
    var confirmPassword = form["confirmPassword"].ToString();
    var terms = form["termsAccepted"].ToString();

    string MakeErrorRedirect(string errorMsg)
    {
        return $"/register?error={Uri.EscapeDataString(errorMsg)}&firstName={Uri.EscapeDataString(firstName)}&lastName={Uri.EscapeDataString(lastName)}&email={Uri.EscapeDataString(email)}&city={Uri.EscapeDataString(city)}";
    }

    if (terms != "on" && terms != "true")
    {
        return Results.Redirect(MakeErrorRedirect("You must agree to the Terms of Service and Privacy Policy."));
    }

    if (password != confirmPassword)
    {
        return Results.Redirect(MakeErrorRedirect("Passwords do not match."));
    }

    var result = userService.RegisterUser(firstName, lastName, email, city, password);
    if (!result.Success)
    {
        return Results.Redirect(MakeErrorRedirect(result.Error ?? "Registration failed."));
    }

    var user = result.User!;
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Name, user.DisplayName),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
    });

    return Results.Redirect("/browse");
}).DisableAntiforgery();

app.MapPost("/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/browse");
}).DisableAntiforgery();

app.MapGet("/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/browse");
});

app.MapPost("/auth/forgot-password", (HttpContext context, IUserService userService) =>
{
    var form = context.Request.HasFormContentType ? context.Request.Form : null;
    var email = form != null ? form["email"].ToString() : string.Empty;
    var result = userService.GeneratePasswordResetToken(email);

    var redirectUrl = "/forgot-password?submitted=true";
    if (result.Success && !string.IsNullOrWhiteSpace(result.ResetToken))
    {
        redirectUrl += $"&demoToken={Uri.EscapeDataString(result.ResetToken)}&email={Uri.EscapeDataString(email)}";
    }
    return Results.Redirect(redirectUrl);
}).DisableAntiforgery();

app.MapPost("/auth/reset-password", (HttpContext context, IUserService userService) =>
{
    var form = context.Request.HasFormContentType ? context.Request.Form : null;
    var email = form != null ? form["email"].ToString() : string.Empty;
    var token = form != null ? form["token"].ToString() : string.Empty;
    var newPassword = form != null ? form["newPassword"].ToString() : string.Empty;
    var confirmPassword = form != null ? form["confirmPassword"].ToString() : string.Empty;

    if (newPassword != confirmPassword)
    {
        return Results.Redirect($"/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}&error={Uri.EscapeDataString("Passwords do not match.")}");
    }

    var result = userService.ResetPasswordWithToken(email, token, newPassword);
    if (!result.Success)
    {
        return Results.Redirect($"/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}&error={Uri.EscapeDataString(result.Error ?? "Password reset failed.")}");
    }

    return Results.Redirect("/login?resetSuccess=true");
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
