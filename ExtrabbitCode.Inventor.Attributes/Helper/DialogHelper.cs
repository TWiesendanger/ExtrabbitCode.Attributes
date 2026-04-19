using ExtrabbitCode.Inventor.Attributes.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Color = System.Windows.Media.Color;
using UiMessageBox = Wpf.Ui.Controls.MessageBox;

namespace ExtrabbitCode.Inventor.Attributes.Helper;

static class DialogHelper
{
    public static (bool IsValid, string? Message) ValidateSingleSelectionForAddAttribute()
    {
        Document? activeDocument = Globals.InvApp.ActiveDocument;
        if (activeDocument == null)
        {
            return (false, "No active Inventor document found.");
        }

        if (activeDocument.SelectSet.Count != 1)
        {
            return (false, "Please select exactly one object before adding an attribute.");
        }

        return (true, null);
    }
    public static void ShowInfoMessage(string title, string content)
    {
        UiMessageBox messageBox = new()
        {
            Title = title,
            Content = content,
            ShowTitle = true,
            CloseButtonText = "Close",
            IsCloseButtonEnabled = true,
            IsPrimaryButtonEnabled = false,
            IsSecondaryButtonEnabled = false,
            Width = 420,
            MinWidth = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        _ = new WindowInteropHelper(messageBox)
        {
            Owner = new IntPtr(Globals.InvApp.MainFrameHWND)
        };

        SetDialogTheme(messageBox);

        _ = messageBox.ShowDialogAsync();
    }

    public static async Task<bool> ShowConfirmationAsync(
        string title,
        string content,
        string primaryButtonText = "Yes",
        string closeButtonText = "Cancel")
    {
        UiMessageBox messageBox = new()
        {
            Title = title,
            Content = content,
            ShowTitle = true,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            IsPrimaryButtonEnabled = true,
            IsCloseButtonEnabled = true,
            IsSecondaryButtonEnabled = false,
            Width = 460,
            MinWidth = 460,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        _ = new WindowInteropHelper(messageBox)
        {
            Owner = new IntPtr(Globals.InvApp.MainFrameHWND)
        };

        SetDialogTheme(messageBox);

        Wpf.Ui.Controls.MessageBoxResult result = await messageBox.ShowDialogAsync().ConfigureAwait(true);

        return result == Wpf.Ui.Controls.MessageBoxResult.Primary;
    }

    public static void SetDialogTheme(Window dialog)
    {
        ApplicationTheme theme = Globals.ActiveTheme.Name == InventorThemeConstants.LightTheme
            ? ApplicationTheme.Light
            : ApplicationTheme.Dark;

        ApplicationThemeManager.Apply(theme);

        ApplicationAccentColorManager.Apply(Color.FromArgb(0xFF, 0x06, 0x96, 0xD7), theme);
        ThemeResourceHelper.ApplyInventorThemeResources();
        ApplicationThemeManager.Apply(dialog);
    }

    public static async Task<bool> ShowTelemetryConsentAsync()
    {
        const string privacyUrl = "https://github.com/TWiesendanger/ExtrabbitCode.Inventor.Attributes/blob/master/TELEMETRY.md";

        System.Windows.Controls.TextBlock textBlock = new()
        {
            TextWrapping = System.Windows.TextWrapping.Wrap,
            MaxWidth = 440
        };
        textBlock.Inlines.Add(new Run(
            "Help improve this add-in by sharing anonymous usage data.\n\n" +
            "No personal information, file names, or model contents are ever collected. " +
            "Data is fully anonymous and cannot be linked back to you.\n\n" +
            "You can change this setting at any time in the Settings dialog.\n\n"));
        Hyperlink link = new(new Run("→ What is tracked? (opens browser)"))
        {
            NavigateUri = new Uri(privacyUrl)
        };
        link.RequestNavigate += (_, e) =>
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
        textBlock.Inlines.Add(link);

        System.Windows.Controls.ScrollViewer scrollViewer = new()
        {
            Content = textBlock,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            MaxHeight = 300
        };

        UiMessageBox messageBox = new()
        {
            Title = "Anonymous Usage Data",
            Content = scrollViewer,
            ShowTitle = true,
            PrimaryButtonText = "Enable (recommended)",
            SecondaryButtonText = "Disable",
            IsPrimaryButtonEnabled = true,
            IsSecondaryButtonEnabled = true,
            IsCloseButtonEnabled = false,
            Width = 500,
            MinWidth = 500,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        _ = new WindowInteropHelper(messageBox)
        {
            Owner = new IntPtr(Globals.InvApp.MainFrameHWND)
        };

        SetDialogTheme(messageBox);

        Wpf.Ui.Controls.MessageBoxResult result =
            await messageBox.ShowDialogAsync().ConfigureAwait(true);

        return result == Wpf.Ui.Controls.MessageBoxResult.Primary;
    }
}