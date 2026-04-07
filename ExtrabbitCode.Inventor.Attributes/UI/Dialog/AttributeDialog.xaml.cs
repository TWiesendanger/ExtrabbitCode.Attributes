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

        _viewModel.AttributeTree.CollectionChanged += (_, _) => UpdateExpansion();
        Loaded += (_, _) => UpdateExpansion();
    }

    private void UpdateExpansion()
    {
        if (DataContext is not AttributeWindowViewModel viewModel)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(viewModel.SearchText))
        {
            ExpandRootNode();
            return;
        }

        //ExpandAllVisibleNodes();
    }

    private void ExpandAllVisibleNodes()
    {
        Dispatcher.InvokeAsync(() =>
        {
            AttributeTreeView.UpdateLayout();

            for (int i = 0; i < AttributeTreeView.Items.Count; i++)
            {
                if (AttributeTreeView.ItemContainerGenerator.ContainerFromIndex(i)
                    is TreeViewItem rootItem)
                {
                    ExpandTreeViewItemRecursive(rootItem);
                }
            }
        }, DispatcherPriority.Loaded);
    }

    private static void ExpandTreeViewItemRecursive(TreeViewItem item)
    {
        item.IsExpanded = true;
        item.UpdateLayout();

        for (int i = 0; i < item.Items.Count; i++)
        {
            if (item.ItemContainerGenerator.ContainerFromIndex(i) is TreeViewItem childItem)
            {
                ExpandTreeViewItemRecursive(childItem);
            }
        }
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