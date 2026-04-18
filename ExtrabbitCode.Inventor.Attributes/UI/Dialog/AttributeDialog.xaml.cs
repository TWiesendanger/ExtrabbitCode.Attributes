using ExtrabbitCode.Inventor.Attributes.Helper;
using ExtrabbitCode.Inventor.Attributes.UI.ViewModels;
using System;
using System.Windows;
using System.Windows.Threading;

namespace ExtrabbitCode.Inventor.Attributes.UI.Dialog;

public partial class AttributeDialog
{
    private readonly AttributeWindowViewModel _viewModel;
    private bool _themeRefreshScheduled;

    public AttributeDialog()
    {
        InitializeComponent();
        _viewModel = new AttributeWindowViewModel(Globals.SettingsService, Globals.AttributeService, Globals.UserNotificationService);
        Globals.UserNotificationService.SetPresenter(SnackbarPresenter);
        DataContext = _viewModel;

        Activated += OnActivated;
        Deactivated += OnDeactivated;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (StandardAddInServer.InvAppEvents != null)
        {
            StandardAddInServer.InvAppEvents.OnActivateDocument += OnInventorDocumentActivated;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (StandardAddInServer.InvAppEvents != null)
        {
            StandardAddInServer.InvAppEvents.OnActivateDocument -= OnInventorDocumentActivated;
        }
    }

    private void OnInventorDocumentActivated(
        _Document documentObject,
        EventTimingEnum beforeOrAfter,
        NameValueMap context,
        out HandlingCodeEnum handlingCode)
    {
        handlingCode = HandlingCodeEnum.kEventNotHandled;

        if (beforeOrAfter != EventTimingEnum.kAfter)
        {
            return;
        }

        if (!Globals.SettingsService.GetCopy().UpdateAttributesOnDocumentSwitch)
        {
            return;
        }

        // Marshal onto UI thread — Inventor events can arrive off-thread
        Dispatcher.BeginInvoke(() => _viewModel.RefreshAttributes());
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