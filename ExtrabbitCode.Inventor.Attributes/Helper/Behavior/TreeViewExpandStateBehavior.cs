using System.Windows;
using System.Windows.Controls;

namespace ExtrabbitCode.Inventor.Attributes.Helper.Behavior;

public static class TreeViewExpandStateBehavior
{
    public static readonly DependencyProperty PreserveExpandStateProperty =
        DependencyProperty.RegisterAttached(
            "PreserveExpandState",
            typeof(bool),
            typeof(TreeViewExpandStateBehavior),
            new PropertyMetadata(false, OnPreserveExpandStateChanged));

    public static bool GetPreserveExpandState(DependencyObject obj) =>
        (bool)obj.GetValue(PreserveExpandStateProperty);

    public static void SetPreserveExpandState(DependencyObject obj, bool value) =>
        obj.SetValue(PreserveExpandStateProperty, value);

    private static void OnPreserveExpandStateChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is not TreeView treeView)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            treeView.ItemContainerGenerator.StatusChanged += (_, _) =>
                SyncExpandState(treeView.ItemContainerGenerator, treeView.Items);
        }
    }

    private static void SyncExpandState(
        ItemContainerGenerator generator,
        System.Collections.IEnumerable items)
    {
        foreach (object? item in items)
        {
            if (generator.ContainerFromItem(item) is not TreeViewItem container)
            {
                continue;
            }

            if (item is Models.AttributeTreeNode node)
            {
                // VM -> View (initial restore)
                container.IsExpanded = node.IsExpanded;

                // View -> VM
                container.Expanded += (_, _) => node.IsExpanded = true;
                container.Collapsed += (_, _) => node.IsExpanded = false;

                // Recurse into children once this container generates its items
                container.ItemContainerGenerator.StatusChanged += (_, _) =>
                    SyncExpandState(container.ItemContainerGenerator, container.Items);
            }
        }
    }
}