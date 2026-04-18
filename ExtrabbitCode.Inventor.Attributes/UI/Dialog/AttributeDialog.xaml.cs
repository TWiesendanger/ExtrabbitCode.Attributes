using ExtrabbitCode.Inventor.Attributes.Helper;
using ExtrabbitCode.Inventor.Attributes.UI.ViewModels;
using System;
using System.Windows.Threading;

namespace ExtrabbitCode.Inventor.Attributes.UI.Dialog;

public partial class AttributeDialog
{
    public readonly AttributeWindowViewModel _viewModel;
    private bool _themeRefreshScheduled;

    public AttributeDialog()
    {
        InitializeComponent();
        _viewModel = new AttributeWindowViewModel(Globals.SettingsService, Globals.AttributeService, Globals.UserNotificationService);
        Globals.UserNotificationService.SetPresenter(SnackbarPresenter);
        DataContext = _viewModel;

        Activated += OnActivated;
        Deactivated += OnDeactivated;
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        RefreshTheme();
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        ScheduleThemeRefresh();
    }

    private void ScheduleThemeRefresh()
    {
        if (_themeRefreshScheduled)
        {
            return;
        }

        _themeRefreshScheduled = true;

        Dispatcher.BeginInvoke(() =>
        {
            _themeRefreshScheduled = false;
            RefreshTheme();
        }, DispatcherPriority.ApplicationIdle);
    }

    private void RefreshTheme()
    {
        DialogHelper.SetDialogTheme(this);
        InvalidateVisual();
        UpdateLayout();
    }
}