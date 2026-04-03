using ExtrabbitCode.Inventor.Attributes.UI.ViewModels;
using System.Windows.Controls;
using System.Windows.Threading;

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

        _viewModel.AttributeTree.CollectionChanged += (_, _) => ExpandRootNode();
        Loaded += (_, _) => ExpandRootNode();
    }

    private void ExpandRootNode()
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (AttributeTreeView.Items.Count == 0)
            {
                return;
            }

            AttributeTreeView.UpdateLayout();

            if (AttributeTreeView.ItemContainerGenerator.ContainerFromIndex(0)
                is TreeViewItem rootItem)
            {
                rootItem.IsExpanded = true;
            }
        }, DispatcherPriority.Loaded);
    }
}