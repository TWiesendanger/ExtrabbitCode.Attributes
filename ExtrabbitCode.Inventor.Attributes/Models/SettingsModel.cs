namespace ExtrabbitCode.Inventor.Attributes.Models;

public class SettingsModel
{
    public bool ShowConfirmationMessages { get; set; } = true;
    public bool ShowWarningOnSingleAttributeDelete { get; set; } = true;

    public bool ShowWarningOnDeleteAllAttributes { get; set; } = true;

    public bool UpdateAttributesOnDocumentSwitch { get; set; } = true;
}