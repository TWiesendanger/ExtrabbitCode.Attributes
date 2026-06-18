using ExtrabbitCode.Inventor.ModernUi;
using System.Threading.Tasks;
using System.Windows;

namespace ExtrabbitCode.Attributes.Services;

public sealed class UserNotificationService : IUserNotificationService
{
    private Window? _owner;

    public void SetPresenter(Window owner)
    {
        _owner = owner;
    }

    public Task ShowInfoAsync(string title, string message) => Show(title, message, ToastType.Info);

    public Task ShowSuccessAsync(string title, string message) => Show(title, message, ToastType.Success);

    public Task ShowWarningAsync(string title, string message) => Show(title, message, ToastType.Warning);

    public Task ShowErrorAsync(string title, string message) => Show(title, message, ToastType.Error);

    private Task Show(string title, string message, ToastType type)
    {
        Window? owner = _owner;
        if (owner != null)
        {
            // Toasts must be shown on the UI thread; callers may be on a background thread.
            owner.Dispatcher.BeginInvoke(() => ModernToast.Show(owner, message, type, title));
        }

        return Task.CompletedTask;
    }
}
