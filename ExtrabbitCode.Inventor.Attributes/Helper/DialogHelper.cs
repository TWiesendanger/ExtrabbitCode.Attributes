using ExtrabbitCode.Inventor.Attributes.Models;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Color = System.Windows.Media.Color;
using UiMessageBox = Wpf.Ui.Controls.MessageBox;

namespace ExtrabbitCode.Inventor.Attributes.Helper;

static class DialogHelper
{
    public static bool CanOpenAddAttributeDialog()
    {
        Document activeDocument = Globals.InvApp.ActiveDocument;
        if (activeDocument.SelectSet.Count == 1)
        {
            return true;
        }

        UiMessageBox messageBox = new()
        {
            Title = "Add Attribute",
            Content = "Please select exactly one object before adding an attribute.",
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
        return false;
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