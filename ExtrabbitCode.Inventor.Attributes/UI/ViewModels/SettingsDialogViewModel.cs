using CommunityToolkit.Mvvm.ComponentModel;
using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.Services;

namespace ExtrabbitCode.Inventor.Attributes.UI.ViewModels;

public partial class SettingsDialogViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private bool showWarningOnSingleAttributeDelete;

    [ObservableProperty]
    private bool showWarningOnDeleteAllAttributes;

    [ObservableProperty]
    private bool updateAttributesOnDocumentSwitch;

    public SettingsDialogViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;

        SettingsModel settings = _settingsService.GetCopy();

        ShowWarningOnSingleAttributeDelete =
            settings.ShowWarningOnSingleAttributeDelete;
        ShowWarningOnDeleteAllAttributes =
            settings.ShowWarningOnDeleteAllAttributes;
        UpdateAttributesOnDocumentSwitch =
            settings.UpdateAttributesOnDocumentSwitch;
    }

    public void Save()
    {
        SettingsModel settings = new()
        {
            ShowWarningOnSingleAttributeDelete =
                ShowWarningOnSingleAttributeDelete,
            ShowWarningOnDeleteAllAttributes =
                ShowWarningOnDeleteAllAttributes,
            UpdateAttributesOnDocumentSwitch =
                UpdateAttributesOnDocumentSwitch
        };

        _settingsService.Update(settings);
    }
}