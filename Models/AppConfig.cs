namespace Reboot.Models;

public static class AppConfig
{
    public const string AppName = "Reboot";
    public const string Tagline = "Verified used electronics & gaming gear with escrow protection";
    public const string CurrencySymbol = "₱";
    public const string CurrencyCode = "PHP";

    public static string FormatPrice(decimal amount) => $"{CurrencySymbol}{amount:N0}";
}
