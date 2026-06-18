using System.Threading.Tasks;
using System.Windows;

namespace ExtrabbitCode.Attributes.Services;

public interface IUserNotificationService
{
    /// <summary>Sets the window that hosts the toasts (typically the dockable attribute panel).</summary>
    void SetPresenter(Window owner);

    Task ShowInfoAsync(string title, string message);

    Task ShowSuccessAsync(string title, string message);

    Task ShowWarningAsync(string title, string message);

    Task ShowErrorAsync(string title, string message);
}
