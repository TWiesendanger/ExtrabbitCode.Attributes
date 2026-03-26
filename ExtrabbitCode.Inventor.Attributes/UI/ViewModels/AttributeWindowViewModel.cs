
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExtrabbitCode.Inventor.Attributes.Services;

namespace ExtrabbitCode.Inventor.Attributes.UI.ViewModels;

public partial class AttributeWindowViewModel : ObservableObject
{
    private readonly SettingsService _settingsService = Globals.SettingsService;
    private readonly AttributeService _attributeService = Globals.AttributeService;

    [RelayCommand]
    private void GetAllAttributes()
    {
        AttributeSetsEnumerator? attributeSets = _attributeService.GetAttributeSets(Globals.InvApp.ActiveDocument);
    }

    [RelayCommand]
    private static void DeleteAttributes()
    {
        // TODO: Delete selected/all attributes
    }
}