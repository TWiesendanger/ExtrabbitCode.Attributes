
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExtrabbitCode.Inventor.Attributes.Helper;
using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.Services;
using ExtrabbitCode.Inventor.Attributes.UI.Dialog;
using System.Threading.Tasks;

namespace ExtrabbitCode.Inventor.Attributes.UI.ViewModels;

/// <inheritdoc/>
public partial class AttributeWindowViewModel(SettingsService settingsService, AttributeService attributeService) : ObservableObject
{
    [ObservableProperty]
    private string searchText = string.Empty;

    [RelayCommand]
    private static void AddAttribute()
    {
        if (!DialogHelper.CanOpenAddAttributeDialog())
        {
            return;
        }

        AddAttributeDialog addAttributeDialog = new();
        DialogHelper.SetDialogTheme(addAttributeDialog);

        addAttributeDialog.ShowDialog();
    }

    [RelayCommand]
    private async Task DeleteSelected()
    {

        if (Globals.InvApp.ActiveDocument.SelectSet.Count == 0)
        {
            DialogHelper.ShowInfoMessage(
                "Delete Attributes",
                "Please select at least one object.");
            return;
        }

        if (settingsService.GetCopy().ShowWarningOnSingleAttributeDelete)
        {
            bool confirmed = await DialogHelper.ShowConfirmationAsync(
                "Delete Attributes",
                "This will delete all attributes on the selected object(s). Do you want to continue?",
                "Delete").ConfigureAwait(true);

            if (!confirmed)
            {
                return;
            }
        }

        DeleteAttributesResult result = attributeService.DeleteAllAttributes(GetSelectedObjects());

        if (!result.HasChanges)
        {
            DialogHelper.ShowInfoMessage(
                "Delete Attributes",
                "No attributes were found on the selected object(s).");
            return;
        }

        string message =
            $"Deleted {result.DeletedAttributes} attribute(s) and " +
            $"{result.DeletedAttributeSets} attribute set(s) on " +
            $"{result.AffectedObjects} object(s).";

        if (result.FailedObjects > 0)
        {
            message += $"\nFailed objects: {result.FailedObjects}.";
        }

        if (settingsService.GetCopy().ShowConfirmationMessages)
        {
            DialogHelper.ShowInfoMessage("Delete Attributes", message);
        }
    }

    [RelayCommand]
    private async Task DeleteAll()
    {
        ObjectCollection? objectsWithAttributes =
            attributeService.GetObjectsWithAttributes(Globals.InvApp.ActiveDocument);

        if (objectsWithAttributes == null || objectsWithAttributes.Count == 0)
        {
            DialogHelper.ShowInfoMessage(
                "Delete All Attributes",
                "No attributes were found in the active document.");
            return;
        }

        if (settingsService.GetCopy().ShowWarningOnDeleteAllAttributes)
        {
            bool confirmed = await DialogHelper.ShowConfirmationAsync(
                "Delete All Attributes",
                "This will delete all attributes in the active document. Do you want to continue?",
                "Delete All").ConfigureAwait(true);

            if (!confirmed)
            {
                return;
            }
        }

        DeleteAttributesResult result =
            attributeService.DeleteAllAttributes(objectsWithAttributes);

        if (!result.HasChanges)
        {
            DialogHelper.ShowInfoMessage(
                "Delete All Attributes",
                "No attributes were found in the active document.");
            return;
        }

        string message =
            $"Deleted {result.DeletedAttributes} attribute(s) and " +
            $"{result.DeletedAttributeSets} attribute set(s) on " +
            $"{result.AffectedObjects} object(s).";

        if (result.FailedObjects > 0)
        {
            message += $"\nFailed objects: {result.FailedObjects}.";
        }

        if (settingsService.GetCopy().ShowConfirmationMessages)
        {
            DialogHelper.ShowInfoMessage("Delete All Attributes", message);
        }
    }

    [RelayCommand]
    private static void GetAllAttributes()
    {
    }

    [RelayCommand]
    private static void OpenSettings()
    {
        SettingsDialog settingsDialog = new();
        DialogHelper.SetDialogTheme(settingsDialog);

        settingsDialog.ShowDialog();
    }

    private static ObjectCollection? GetSelectedObjects()
    {

        ObjectCollection selectedObjects =
            Globals.InvApp.TransientObjects.CreateObjectCollection();

        foreach (object selectedObject in Globals.InvApp.ActiveDocument.SelectSet)
        {
            selectedObjects.Add(selectedObject);
        }

        return selectedObjects;
    }
}