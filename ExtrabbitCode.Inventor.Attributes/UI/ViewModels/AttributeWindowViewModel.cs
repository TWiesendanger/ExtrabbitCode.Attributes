
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExtrabbitCode.Inventor.Attributes.Helper;
using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.Services;
using ExtrabbitCode.Inventor.Attributes.Services.AttributeModels;
using ExtrabbitCode.Inventor.Attributes.UI.Dialog;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace ExtrabbitCode.Inventor.Attributes.UI.ViewModels;

/// <inheritdoc/>
public partial class AttributeWindowViewModel(SettingsService settingsService, AttributeService attributeService) : ObservableObject
{
    [ObservableProperty]
    private string searchText = string.Empty;

    public ObservableCollection<AttributeTreeNode> AttributeTree { get; } = [];

    [ObservableProperty]
    private AttributeTreeNode? selectedNode;

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
    private void GetAllAttributes()
    {
        Document? document = Globals.InvApp?.ActiveDocument;
        if (document == null)
        {
            DialogHelper.ShowInfoMessage(
                "Refresh Attributes",
                "No active Inventor document found.");
            return;
        }

        AttributeDocumentInfo? tree = attributeService.GetAttributeTree(document);
        if (tree == null)
        {
            return;
        }

        AttributeTree.Clear();

        AttributeTreeNode documentNode = new()
        {
            Name = tree.DocumentName,
            NodeType = NodeType.Document,
            IsExpanded = true,
            IconSource = AttributeTreeIconProvider.GetDocumentIcon(document)
        };

        foreach (AttributeOwnerInfo owner in tree.Owners)
        {
            AttributeTreeNode ownerNode = new()
            {
                Name = owner.DisplayName,
                Value = owner.ObjectType,
                NodeType = NodeType.Owner,
                IconSource = AttributeTreeIconProvider.GetIcon(NodeType.Owner),
                OwnerObject = owner.OwnerObject
            };

            foreach (AttributeSetInfo attributeSet in owner.AttributeSets)
            {
                AttributeTreeNode setNode = new()
                {
                    Name = attributeSet.Name,
                    NodeType = NodeType.AttributeSet,
                    OwnerObject = owner.OwnerObject,
                    AttributeSetName = attributeSet.Name,
                    IconSource = AttributeTreeIconProvider.GetIcon(NodeType.AttributeSet)
                };

                foreach (AttributeInfo attribute in attributeSet.Attributes)
                {
                    setNode.Children.Add(new AttributeTreeNode
                    {
                        Name = attribute.Name,
                        Value = $"{attribute.ValueType}: {attribute.Value}",
                        NodeType = NodeType.Attribute,
                        OwnerObject = owner.OwnerObject,
                        AttributeSetName = attributeSet.Name,
                        AttributeName = attribute.Name,
                        IconSource = AttributeTreeIconProvider.GetIcon(NodeType.Attribute),
                        Parent = setNode
                    });
                }

                setNode.Parent = ownerNode;
                ownerNode.Children.Add(setNode);
            }

            ownerNode.Parent = documentNode;
            documentNode.Children.Add(ownerNode);
        }

        AttributeTree.Add(documentNode);
    }

    [RelayCommand]
    private void DeleteNode(AttributeTreeNode? node)
    {
        if (node == null)
        {
            return;
        }

        switch (node.NodeType)
        {
            case NodeType.Attribute:
                DeleteAttributeNode(node);
                break;

            case NodeType.AttributeSet:
                DeleteAttributeSetNode(node);
                break;
        }
    }

    [RelayCommand]
    private void EditNode(AttributeTreeNode? node)
    {
        if (node == null)
        {
            return;
        }

        switch (node.NodeType)
        {
            case NodeType.Attribute:
                //EditAttributeNode(node);
                break;

            case NodeType.AttributeSet:
                //EditAttributeSetNode(node);
                break;
        }
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

    private void DeleteAttributeNode(AttributeTreeNode node)
    {
        if (node.OwnerObject == null ||
            string.IsNullOrWhiteSpace(node.AttributeSetName) ||
            string.IsNullOrWhiteSpace(node.AttributeName))
        {
            return;
        }

        bool deleted = attributeService.DeleteAttribute(
            Globals.InvApp.ActiveDocument,
            node.OwnerObject,
            node.AttributeSetName,
            node.AttributeName);

        if (!deleted)
        {
            DialogHelper.ShowInfoMessage(
                "Delete Attribute",
                "The attribute could not be deleted.");
            return;
        }

        node.Parent?.Children.Remove(node);
    }

    private void DeleteAttributeSetNode(AttributeTreeNode node)
    {
        if (node.OwnerObject == null ||
            string.IsNullOrWhiteSpace(node.AttributeSetName))
        {
            return;
        }

        bool deleted = attributeService.DeleteAttributeSet(
            Globals.InvApp.ActiveDocument,
            node.OwnerObject,
            node.AttributeSetName);

        if (!deleted)
        {
            DialogHelper.ShowInfoMessage(
                "Delete Attribute Set",
                "The attribute set could not be deleted.");
            return;
        }

        node.Parent?.Children.Remove(node);
    }
}