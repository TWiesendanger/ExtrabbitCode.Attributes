using ExtrabbitCode.Inventor.Attributes.Models;
using System;

namespace ExtrabbitCode.Inventor.Attributes.Services;

public sealed class SettingsService : ISettingsService
{
    public SettingsModel Current { get; private set; } = new();

    public SettingsModel GetCopy()
    {
        return new SettingsModel
        {
            ShowWarningOnSingleAttributeDelete =
                Current.ShowWarningOnSingleAttributeDelete,
            ShowWarningOnDeleteAllAttributes =
                Current.ShowWarningOnDeleteAllAttributes,
            UpdateAttributesOnDocumentSwitch =
                Current.UpdateAttributesOnDocumentSwitch
        };
    }

    public void Update(SettingsModel settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Current = new SettingsModel
        {
            ShowWarningOnSingleAttributeDelete =
                settings.ShowWarningOnSingleAttributeDelete,
            ShowWarningOnDeleteAllAttributes =
                settings.ShowWarningOnDeleteAllAttributes,
            UpdateAttributesOnDocumentSwitch =
                settings.UpdateAttributesOnDocumentSwitch
        };
    }
}