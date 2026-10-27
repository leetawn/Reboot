namespace Reboot.Services;

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

public class ToastMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Message { get; set; } = string.Empty;
    public string? Title { get; set; }
    public ToastType Type { get; set; } = ToastType.Info;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int DurationMs { get; set; } = 4500;
}

public interface IToastService
{
    event Action<ToastMessage>? OnShow;
    event Action<Guid>? OnDismiss;

    void Show(string message, ToastType type = ToastType.Info, string? title = null, int durationMs = 4500);
    void ShowSuccess(string message, string? title = null, int durationMs = 4500);
    void ShowError(string message, string? title = null, int durationMs = 6000);
    void ShowWarning(string message, string? title = null, int durationMs = 5000);
    void ShowInfo(string message, string? title = null, int durationMs = 4500);
    void Dismiss(Guid id);
}
