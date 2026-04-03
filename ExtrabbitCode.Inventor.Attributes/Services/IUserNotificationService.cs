using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace ExtrabbitCode.Inventor.Attributes.Services;

public interface IUserNotificationService
{
    void SetPresenter(SnackbarPresenter presenter);

    Task ShowInfoAsync(string title, string message);

    Task ShowSuccessAsync(string title, string message);

    Task ShowWarningAsync(string title, string message);

    Task ShowErrorAsync(string title, string message);
}