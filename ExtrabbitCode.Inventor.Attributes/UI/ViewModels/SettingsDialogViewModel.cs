using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.Services;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
// ReSharper disable InconsistentNaming

namespace ExtrabbitCode.Inventor.Attributes.UI.ViewModels;

public partial class SettingsDialogViewModel : ObservableValidator
{
    private readonly SettingsService _settingsService;

    private readonly AttributeLibraryService _attributeLibraryService;

    public bool CanAddAttributeSetName => !HasErrors && !string.IsNullOrWhiteSpace(NewAttributeSetName);

    [ObservableProperty]
    private bool telemetryEnabled;

    [ObservableProperty]
    private bool showConfirmationMessages;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Attribute set name is required.")]
    [RegularExpression(
        "^[A-Za-z]+$",
        ErrorMessage = "Only letters are allowed. No spaces, digits, or special characters.")]
    private string newAttributeSetName = string.Empty;

    [ObservableProperty]
    private string? selectedAttributeSetName;

    public ObservableCollection<string> AttributeSetNames { get; } = [];

    [ObservableProperty]
    private bool showWarningOnSingleAttributeDelete;

    [ObservableProperty]
    private bool showWarningOnDeleteAllAttributes;

    [ObservableProperty]
    private bool updateAttributesOnDocumentSwitch;

    [ObservableProperty]
    private bool deleteAutodeskDefaultAttributeSets;

    public SettingsDialogViewModel(SettingsService settingsService, AttributeLibraryService attributeLibraryService)
    {
        _settingsService = settingsService;
        _attributeLibraryService = attributeLibraryService;

        LoadAttributeSetNames();

        SettingsModel settings = _settingsService.GetCopy();

        ShowWarningOnSingleAttributeDelete = settings.ShowWarningOnSingleAttributeDelete;
        ShowWarningOnDeleteAllAttributes = settings.ShowWarningOnDeleteAllAttributes;
        UpdateAttributesOnDocumentSwitch = settings.UpdateAttributesOnDocumentSwitch;
        ShowConfirmationMessages = settings.ShowConfirmationMessages;
        DeleteAutodeskDefaultAttributeSets = settings.DeleteAutodeskDefaultAttributeSets;
        TelemetryEnabled = settings.TelemetryEnabled;
    }

    [RelayCommand]
    private void AddAttributeSetName()
    {
        Globals.TelemetryService.TrackEvent("settings_attribute_set_name_add_started");

        ValidateProperty(NewAttributeSetName, nameof(NewAttributeSetName));

        if (HasErrors || string.IsNullOrWhiteSpace(NewAttributeSetName))
        {
            return;
        }

        _attributeLibraryService.AddAttributeSetName(NewAttributeSetName);
        LoadAttributeSetNames();
        NewAttributeSetName = string.Empty;

        Globals.TelemetryService.TrackEvent("settings_attribute_set_name_add_succeeded");
    }

    [RelayCommand]
    private void RemoveSelectedAttributeSetName()
    {
        Globals.TelemetryService.TrackEvent("settings_attribute_set_name_remove_started");

        if (string.IsNullOrWhiteSpace(SelectedAttributeSetName))
        {
            return;
        }

        _attributeLibraryService.RemoveAttributeSetName(SelectedAttributeSetName);
        LoadAttributeSetNames();
        SelectedAttributeSetName = null;

        Globals.TelemetryService.TrackEvent("settings_attribute_set_name_remove_succeeded");
    }

    partial void OnNewAttributeSetNameChanged(string value)
    {
        ValidateProperty(value, nameof(NewAttributeSetName));
        OnPropertyChanged(nameof(CanAddAttributeSetName));
    }

    private void LoadAttributeSetNames()
    {
        AttributeSetNames.Clear();

        foreach (string name in _attributeLibraryService.GetAttributeSetNames())
        {
            AttributeSetNames.Add(name);
        }
    }

    public void Save()
    {
        SettingsModel settings = new()
        {
            ShowConfirmationMessages = ShowConfirmationMessages,
            ShowWarningOnSingleAttributeDelete =
                ShowWarningOnSingleAttributeDelete,
            ShowWarningOnDeleteAllAttributes =
                ShowWarningOnDeleteAllAttributes,
            UpdateAttributesOnDocumentSwitch =
                UpdateAttributesOnDocumentSwitch,
            DeleteAutodeskDefaultAttributeSets =
                DeleteAutodeskDefaultAttributeSets,
            TelemetryEnabled = TelemetryEnabled
        };

        _settingsService.Update(settings);

        Globals.TelemetryService.TrackEvent("settings_saved",
            new System.Collections.Generic.Dictionary<string, object>
            {
                ["show_confirmation_messages"] = ShowConfirmationMessages,
                ["show_warning_on_single_delete"] = ShowWarningOnSingleAttributeDelete,
                ["show_warning_on_delete_all"] = ShowWarningOnDeleteAllAttributes,
                ["update_on_document_switch"] = UpdateAttributesOnDocumentSwitch,
                ["delete_autodesk_default_sets"] = DeleteAutodeskDefaultAttributeSets,
                ["telemetry_enabled"] = TelemetryEnabled
            });
    }
}