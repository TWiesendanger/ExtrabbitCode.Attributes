using System;
using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace ExtrabbitCode.Inventor.Attributes.Services;

public sealed class UserNotificationService : IUserNotificationService
{
    private SnackbarPresenter? _presenter;

    public void SetPresenter(SnackbarPresenter presenter)
    {
        _presenter = presenter;
    }

    public Task ShowInfoAsync(string title, string message)
    {
        return ShowAsync(
            title,
            message,
            ControlAppearance.Secondary,
            new SymbolIcon(SymbolRegular.Info24));
    }

    public Task ShowSuccessAsync(string title, string message)
    {
        return ShowAsync(
            title,
            message,
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.CheckmarkCircle24));
    }

    public Task ShowWarningAsync(string title, string message)
    {
        return ShowAsync(
            title,
            message,
            ControlAppearance.Caution,
            new SymbolIcon(SymbolRegular.Warning24));
    }

    public Task ShowErrorAsync(string title, string message)
    {
        return ShowAsync(
            title,
            message,
            ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.DismissCircle24));
    }

    private async Task ShowAsync(
        string title,
        string message,
        ControlAppearance appearance,
        IconElement icon)
    {
        if (_presenter == null)
        {
            return;
        }

        Snackbar snackbar = new(_presenter)
        {
            Title = title,
            Content = message,
            Appearance = appearance,
            Icon = icon,
            IsCloseButtonEnabled = true,
            Timeout = TimeSpan.FromSeconds(3)
        };

        await snackbar.ShowAsync().ConfigureAwait(false);
    }
}