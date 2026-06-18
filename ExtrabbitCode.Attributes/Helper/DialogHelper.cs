using ExtrabbitCode.Inventor.ModernUi;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ExtrabbitCode.Attributes.Helper;

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
        ModernMessageBox.Show(
            Globals.OwnerWindow,
            Globals.CurrentTheme,
            content,
            title,
            [new ModernDialogButton("Close", ModernDialogResult.Ok, IsDefault: true, IsCancel: true, Accent: true)],
            ModernDialogIcon.Info,
            font: Globals.CurrentFont);
    }

    public static Task<bool> ShowConfirmationAsync(
        string title,
        string content,
        string primaryButtonText = "Yes",
        string closeButtonText = "Cancel")
    {
        ModernDialogButton[] buttons =
        [
            new(primaryButtonText, ModernDialogResult.Yes, IsDefault: true, Accent: true),
            new(closeButtonText, ModernDialogResult.No, IsCancel: true),
        ];

        ModernDialogResult result = ModernMessageBox.Show(
            Globals.OwnerWindow,
            Globals.CurrentTheme,
            content,
            title,
            buttons,
            ModernDialogIcon.Question,
            font: Globals.CurrentFont);

        return Task.FromResult(result == ModernDialogResult.Yes);
    }

    public static Task<bool> ShowTelemetryConsentAsync()
    {
        const string privacyUrl = "https://twiesendanger.github.io/ExtrabbitCode.Attributes/docs/telemetry";

        TextBlock textBlock = new()
        {
            TextWrapping = TextWrapping.Wrap,
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

        ScrollViewer scrollViewer = new()
        {
            Content = textBlock,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 300
        };

        ModernDialogButton[] buttons =
        [
            new("Enable (recommended)", ModernDialogResult.Yes, IsDefault: true, Accent: true),
            new("Disable", ModernDialogResult.No, IsCancel: true),
        ];

        ModernDialogResult result = ModernMessageBox.Show(
            Globals.OwnerWindow,
            Globals.CurrentTheme,
            scrollViewer,
            "Anonymous Usage Data",
            buttons,
            ModernDialogIcon.Question,
            font: Globals.CurrentFont);

        return Task.FromResult(result == ModernDialogResult.Yes);
    }
}
