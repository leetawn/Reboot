namespace Reboot.Services;

public class ToastService : IToastService
{
    public event Action<ToastMessage>? OnShow;
    public event Action<Guid>? OnDismiss;

    public void Show(string message, ToastType type = ToastType.Info, string? title = null, int durationMs = 4500)
    {
        var toast = new ToastMessage
        {
            Id = Guid.NewGuid(),
            Message = message,
            Title = title,
            Type = type,
            CreatedAt = DateTime.UtcNow,
            DurationMs = durationMs
        };

        OnShow?.Invoke(toast);
    }

    public void ShowSuccess(string message, string? title = null, int durationMs = 4500)
    {
        Show(message, ToastType.Success, title ?? "Success", durationMs);
    }

    public void ShowError(string message, string? title = null, int durationMs = 6000)
    {
        Show(message, ToastType.Error, title ?? "Error", durationMs);
    }

    public void ShowWarning(string message, string? title = null, int durationMs = 5000)
    {
        Show(message, ToastType.Warning, title ?? "Notice", durationMs);
    }

    public void ShowInfo(string message, string? title = null, int durationMs = 4500)
    {
        Show(message, ToastType.Info, title ?? "Information", durationMs);
    }

    public void Dismiss(Guid id)
    {
        OnDismiss?.Invoke(id);
    }
}
