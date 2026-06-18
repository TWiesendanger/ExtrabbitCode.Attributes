using ExtrabbitCode.Attributes.Helper;
using ExtrabbitCode.Attributes.Models;
using ExtrabbitCode.Attributes.UI.ViewModels;
using ExtrabbitCode.Inventor.ModernUi;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ExtrabbitCode.Attributes.UI.Dialog;

public partial class AttributeDialog
{
    private readonly AttributeWindowViewModel _viewModel;
    private bool _themeRefreshScheduled;

    public AttributeDialog()
    {
        InitializeComponent();
        ModernUi.Apply(this, Globals.CurrentTheme, font: Globals.CurrentFont, applyWindowChrome: false);
        _viewModel = new AttributeWindowViewModel(Globals.SettingsService, Globals.AttributeService, Globals.UserNotificationService);
        Globals.UserNotificationService.SetPresenter(this);
        Globals.OwnerWindow = this;
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

        Globals.OnAttributeAdded = result =>
            Dispatcher.BeginInvoke(() => _viewModel.ApplyExternalAttributeAdded(result));

        Dispatcher.BeginInvoke(async () => await AskTelemetryConsentIfNeededAsync().ConfigureAwait(false));
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (StandardAddInServer.InvAppEvents != null)
        {
            StandardAddInServer.InvAppEvents.OnActivateDocument -= OnInventorDocumentActivated;
        }

        Globals.OnAttributeAdded = null;
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
        ModernUi.SetTheme(this, Globals.CurrentTheme);
        InvalidateVisual();
        UpdateLayout();
    }

    private static async Task AskTelemetryConsentIfNeededAsync()
    {
        SettingsModel settings = Globals.SettingsService.GetCopy();
        if (settings.TelemetryConsentAsked)
        {
            return;
        }

        bool enabled = await DialogHelper.ShowTelemetryConsentAsync().ConfigureAwait(true);

        settings.TelemetryConsentAsked = true;
        settings.TelemetryEnabled = enabled;
        Globals.SettingsService.Update(settings);
        Globals.TelemetryService.Enabled = enabled;

        Globals.TelemetryService.TrackEvent("telemetry_consent_given",
            new System.Collections.Generic.Dictionary<string, object>
            {
                ["enabled"] = enabled
            });
    }
}