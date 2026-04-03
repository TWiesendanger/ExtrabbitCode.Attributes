using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExtrabbitCode.Inventor.Attributes.Helper;
using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.Services;
using ExtrabbitCode.Inventor.Attributes.Services.AttributeModels;
using ExtrabbitCode.Inventor.Attributes.UI.Dialog;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

// ReSharper disable InconsistentNaming

namespace ExtrabbitCode.Inventor.Attributes.UI.ViewModels;

/// <inheritdoc/>
public partial class AttributeWindowViewModel(SettingsService settingsService,
    AttributeService attributeService,
    UserNotificationService userNotificationService) : ObservableObject
{
    [ObservableProperty]
    private string searchText = string.Empty;

    public ObservableCollection<AttributeTreeNode> AttributeTree { get; } = [];

    [ObservableProperty]
    private AttributeTreeNode? selectedNode;

    [RelayCommand]
    private async Task AddAttribute()
    {
        AddAttributeWorkflowResult workflowResult =
            Globals.AddAttributeWorkflowService.Execute();

        if (!workflowResult.IsSuccess)
        {
            if (!string.IsNullOrWhiteSpace(workflowResult.ErrorMessage))
            {
                await userNotificationService.ShowErrorAsync(
                    "Add Attribute",
                    workflowResult.ErrorMessage).ConfigureAwait(false);
            }

            return;
        }

        if (workflowResult is { OwnerObject: not null, Input: not null })
        {
            AddAttributeToTree(workflowResult.OwnerObject, workflowResult.Input);
        }

        if (settingsService.GetCopy().ShowConfirmationMessages &&
            workflowResult.Input != null)
        {
            await userNotificationService.ShowSuccessAsync(
                    "Add Attribute",
                    $"Attribute '{workflowResult.Input.AttributeName}' was added to set '{workflowResult.Input.AttributeSetName}'.")
                .ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task CopyNodeValue(AttributeTreeNode? node)
    {
        if (node is not { CanCopyValue: true } || string.IsNullOrWhiteSpace(node.Value))
        {
            return;
        }

        Clipboard.SetText(node.RawAttributeValue);

        if (settingsService.GetCopy().ShowConfirmationMessages)
        {
            await userNotificationService.ShowSuccessAsync(
                "Copied",
                $"Copied value of '{node.Name}' to clipboard.").ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task SelectNodeObject(AttributeTreeNode? node)
    {
        if (node == null)
        {
            return;
        }

        if (node.NodeType == NodeType.Document)
        {
            return;
        }

        object? targetObject = node.NodeType switch
        {
            NodeType.Owner => node.OwnerObject,
            NodeType.AttributeSet => node.OwnerObject,
            NodeType.Attribute => node.OwnerObject,
            _ => null
        };

        if (targetObject == null)
        {
            await userNotificationService.ShowInfoAsync(
                "Select Object",
                "No selectable Inventor object is associated with this node.").ConfigureAwait(false);
            return;
        }

        try
        {
            SelectSet selectSet = Globals.InvApp.ActiveDocument.SelectSet;
            selectSet.Clear();
            selectSet.Select(targetObject);
        }
        catch
        {
            await userNotificationService.ShowErrorAsync(
                "Select Object",
                "The associated Inventor object cannot be selected.").ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task DeleteSelected()
    {

        if (Globals.InvApp.ActiveDocument.SelectSet.Count == 0)
        {
            await userNotificationService.ShowErrorAsync(
                "Delete Attributes",
                "Please select at least one object.").ConfigureAwait(false);
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

        DeleteAttributesResult result = attributeService.DeleteAllAttributes(GetSelectedObjects(),settingsService.GetCopy().DeleteAutodeskDefaultAttributeSets);

        if (!result.HasChanges)
        {
            await userNotificationService.ShowInfoAsync(
                "Delete Attributes",
                "No attributes were found on the selected object(s).").ConfigureAwait(false);
            return;
        }

        string message =
            $"Deleted {result.DeletedAttributes} attribute(s) and " +
            $"{result.DeletedAttributeSets} attribute set(s) on " +
            $"{result.AffectedObjects} object(s).";

        if (result.FailedObjects > 0)
        {
            message += $"\nFailed objects: {result.FailedObjects}.";
            await userNotificationService.ShowErrorAsync(
                "Delete Attributes",
                message).ConfigureAwait(false);
            return;
        }

        if (settingsService.GetCopy().ShowConfirmationMessages)
        {
            await userNotificationService.ShowSuccessAsync(
                "Delete Attributes",
                message).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// This is the command bar `DeleteAll` button.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task DeleteAll()
    {
        await DeleteAllAttributesInDocumentAsync(
            "Delete All Attributes",
            "This will delete all attributes in the active document. Do you want to continue?").ConfigureAwait(false);
    }

    [RelayCommand]
    private void GetAllAttributes()
    {
        _Document document = Globals.InvApp.ActiveDocument;
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
                        RawAttributeValue = attribute.Value,
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

    /// <summary>
    /// This the context command on rmt on a node. Deletes the node depending on the type it is.
    /// </summary>
    /// <param name="node"></param>
    [RelayCommand]
    private async Task DeleteNode(AttributeTreeNode? node)
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
            case NodeType.Owner:
                DeleteOwnerNode(node);
                break;
            case NodeType.Document:
                await DeleteDocumentNodeAsync(node).ConfigureAwait(false);
                break;
        }
    }

    [RelayCommand]
    public static void EditNode(AttributeTreeNode? node)
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

        AttributeTreeNode? parentNode = node.Parent;

        if (SelectedNode == node)
        {
            SelectedNode = null;
        }

        parentNode?.Children.Remove(node);
    }

    private void AddAttributeToTree(
        object selectedObject,
        AddAttributeDialogResult input)
    {
        if (AttributeTree.Count == 0)
        {
            GetAllAttributes();
            return;
        }

        AttributeTreeNode documentNode = AttributeTree[0];

        AttributeTreeNode? ownerNode = documentNode.Children
            .FirstOrDefault(x => ReferenceEquals(x.OwnerObject, selectedObject));

        if (ownerNode == null)
        {
            GetAllAttributes();
            return;
        }

        AttributeTreeNode? setNode = ownerNode.Children
            .FirstOrDefault(x =>
                x.NodeType == NodeType.AttributeSet &&
                string.Equals(
                    x.AttributeSetName,
                    input.AttributeSetName,
                    StringComparison.OrdinalIgnoreCase));

        if (setNode == null)
        {
            setNode = new AttributeTreeNode
            {
                Name = input.AttributeSetName,
                NodeType = NodeType.AttributeSet,
                OwnerObject = selectedObject,
                AttributeSetName = input.AttributeSetName,
                IconSource = AttributeTreeIconProvider.GetIcon(NodeType.AttributeSet),
                Parent = ownerNode
            };

            ownerNode.Children.Add(setNode);
        }

        AttributeTreeNode? existingAttributeNode = setNode.Children
            .FirstOrDefault(x =>
                x.NodeType == NodeType.Attribute &&
                string.Equals(
                    x.AttributeName,
                    input.AttributeName,
                    StringComparison.OrdinalIgnoreCase));

        string valueText = $"{input.ValueType}: {input.RawValue}";

        if (existingAttributeNode != null)
        {
            existingAttributeNode.Value = valueText;
            return;
        }

        setNode.Children.Add(new AttributeTreeNode
        {
            Name = input.AttributeName,
            Value = valueText,
            RawAttributeValue = input.RawValue,
            NodeType = NodeType.Attribute,
            OwnerObject = selectedObject,
            AttributeSetName = input.AttributeSetName,
            AttributeName = input.AttributeName,
            IconSource = AttributeTreeIconProvider.GetIcon(NodeType.Attribute),
            Parent = setNode
        });
    }

    private static object ConvertToTypedValue(string rawValue, ValueTypeEnum valueType)
    {
        return valueType switch
        {
            ValueTypeEnum.kStringType => rawValue,
            ValueTypeEnum.kBooleanType => rawValue,
            ValueTypeEnum.kIntegerType => int.Parse(rawValue, CultureInfo.InvariantCulture),
            ValueTypeEnum.kDoubleType => double.Parse(rawValue, CultureInfo.InvariantCulture),
            ValueTypeEnum.kByteArrayType => Convert.FromBase64String(rawValue),
            _ => rawValue
        };
    }
    private void DeleteOwnerNode(AttributeTreeNode node)
    {
        if (node.OwnerObject == null)
        {
            return;
        }

        ObjectCollection objectCollection =
            Globals.InvApp.TransientObjects.CreateObjectCollection();

        objectCollection.Add(node.OwnerObject);

        DeleteAttributesResult result =
            attributeService.DeleteAllAttributes(objectCollection, settingsService.GetCopy().DeleteAutodeskDefaultAttributeSets);

        if (!result.HasChanges)
        {
            node.Parent?.Children.Remove(node);
            DialogHelper.ShowInfoMessage(
                "Delete Attributes",
                "No attributes were found on the selected owner object.");
            return;
        }

        if (SelectedNode == node)
        {
            SelectedNode = null;
        }

        node.Parent?.Children.Remove(node);
    }

    private async Task DeleteDocumentNodeAsync(AttributeTreeNode node)
    {
        await DeleteAllAttributesInDocumentAsync(
            "Delete All Attributes",
            $"This will delete all attributes in document '{node.Name}'. Do you want to continue?").ConfigureAwait(false);
    }

    private async Task DeleteAllAttributesInDocumentAsync(
        string title,
        string confirmationMessage)
    {
        _Document? document = Globals.InvApp.ActiveDocument;
        if (document == null)
        {
            await userNotificationService.ShowErrorAsync(
                title,
                "No active Inventor document found.").ConfigureAwait(false);
            return;
        }

        ObjectCollection? objectsWithAttributes =
            attributeService.GetObjectsWithAttributes(document);

        if (objectsWithAttributes == null || objectsWithAttributes.Count == 0)
        {
            await userNotificationService.ShowInfoAsync(
                title,
                "No attributes were found in the active document.").ConfigureAwait(false);
            return;
        }

        if (settingsService.GetCopy().ShowWarningOnDeleteAllAttributes)
        {
            bool confirmed = await DialogHelper.ShowConfirmationAsync(
                title,
                confirmationMessage,
                "Delete All").ConfigureAwait(false);

            if (!confirmed)
            {
                return;
            }
        }

        DeleteAttributesResult result =
            attributeService.DeleteAllAttributes(objectsWithAttributes, settingsService.GetCopy().DeleteAutodeskDefaultAttributeSets);

        if (!result.HasChanges)
        {
            await userNotificationService.ShowInfoAsync(
                title,
                "No attributes were found in the active document.").ConfigureAwait(false);
            return;
        }

        GetAllAttributes();

        string message =
            $"Deleted {result.DeletedAttributes} attribute(s) and " +
            $"{result.DeletedAttributeSets} attribute set(s) on " +
            $"{result.AffectedObjects} object(s).";

        if (result.FailedObjects > 0)
        {
            message += $" Failed objects: {result.FailedObjects}.";

            if (settingsService.GetCopy().ShowConfirmationMessages)
            {
                await userNotificationService.ShowWarningAsync(
                    title,
                    message).ConfigureAwait(false);
            }

            return;
        }

        if (settingsService.GetCopy().ShowConfirmationMessages)
        {
            await userNotificationService.ShowSuccessAsync(
                title,
                message).ConfigureAwait(false);
        }
    }
}