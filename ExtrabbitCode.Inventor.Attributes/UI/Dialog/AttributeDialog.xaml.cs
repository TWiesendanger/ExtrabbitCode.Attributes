using ExtrabbitCode.Inventor.Attributes.UI.ViewModels;

namespace ExtrabbitCode.Inventor.Attributes.UI.Dialog;

public partial class AttributeDialog
{
    private readonly AttributeWindowViewModel _viewModel;

    public AttributeDialog()
    {
        InitializeComponent();
        _viewModel = new AttributeWindowViewModel(Globals.SettingsService, Globals.AttributeService, Globals.UserNotificationService);
        Globals.UserNotificationService.SetPresenter(SnackbarPresenter);
        DataContext = _viewModel;
    }
}