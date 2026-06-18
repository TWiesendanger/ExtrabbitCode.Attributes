using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Reflection;
using File = System.IO.File;
using Path = System.IO.Path;
// ReSharper disable InconsistentNaming

namespace ExtrabbitCode.Attributes.UI.ViewModels;

public partial class InfoDialogViewModel : ObservableObject
{
    private const string GitHubUrl = "https://github.com/TWiesendanger/ExtrabbitCode.Attributes";
    private const string AutodeskStoreUrl = "https://marketplace.autodesk.com/publisher-profile?id=200812101855337";
    private const string DocumentationUrl = "https://attributes.extrabbitcode.com/";

    [ObservableProperty]
    private string programVersion = string.Empty;

    [ObservableProperty]
    private string versionHistory = string.Empty;

    public InfoDialogViewModel()
    {
        LoadProgramVersion();
        LoadVersionHistory();
    }

    [RelayCommand]
    private static void OpenGitHub()
    {
        Globals.TelemetryService.TrackEvent("info_github_opened");
        OpenUrl(GitHubUrl);
    }

    [RelayCommand]
    private static void OpenAutodeskStore()
    {
        Globals.TelemetryService.TrackEvent("info_autodesk_store_opened");
        OpenUrl(AutodeskStoreUrl);
    }

    [RelayCommand]
    private static void OpenDocumentation()
    {
        Globals.TelemetryService.TrackEvent("info_documentation_opened");
        OpenUrl(DocumentationUrl);
    }

    private void LoadProgramVersion()
    {
        ProgramVersion =
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ??
            "Unknown";
    }

    private void LoadVersionHistory()
    {
        string changeLogPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
            string.Empty,
            "Resources",
            "versionhistory.txt");

        VersionHistory = File.Exists(changeLogPath)
            ? File.ReadAllText(changeLogPath)
            : "Changelog file not found.";
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}