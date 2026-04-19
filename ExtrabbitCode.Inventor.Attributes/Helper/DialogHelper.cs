using ExtrabbitCode.Inventor.Attributes.Models;
using System;
using System.Threading.Tasks;
using System.Windows;
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
}