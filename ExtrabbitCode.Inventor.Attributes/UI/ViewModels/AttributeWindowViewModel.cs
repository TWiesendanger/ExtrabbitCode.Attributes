using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExtrabbitCode.Inventor.Attributes.Helper;
using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.Services;
using ExtrabbitCode.Inventor.Attributes.Services.AttributeModels;
using ExtrabbitCode.Inventor.Attributes.UI.Dialog;
using System;
using System.Collections.Generic;
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
    private readonly HashSet<string> _expandedNodeKeys = [];

    [ObservableProperty]
    private string searchText = string.Empty;

    private readonly List<AttributeTreeNode> _allAttributeTree = [];
    public ObservableCollection<AttributeTreeNode> AttributeTree { get; } = [];

    [ObservableProperty]
    private AttributeTreeNode? selectedNode;

    /// <summary>
    /// Add attribute command that is used from dockable window and also the ribbon.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Copy command on rmt. Will copy the raw attribute value to clipboard if the node is an attribute node and has a value.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Tries to select the Inventor object associated with the node. Only works for attribute, attribute set and owner nodes. Shows a message if no object is associated or selection fails.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
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

        DeleteAttributesResult result = attributeService.DeleteAllAttributes(GetSelectedObjects(), settingsService.GetCopy().DeleteAutodeskDefaultAttributeSets);

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
    /// This is the command bar `DeleteAll` button. Depending on options this can lead to destroyed documents.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task DeleteAll()
    {
        await DeleteAllAttributesInDocumentAsync(
            "Delete All Attributes",
            "This will delete all attributes in the active document. Do you want to continue?").ConfigureAwait(false);
    }

    /// <summary>
    /// Refresh button dockable window. Builds the attribute tree from scratch. This is also called after adding an attribute to update the tree. Depending on the number of attributes this can be slow, so for adding we try to just update the tree without full refresh.
    /// </summary>
    [RelayCommand]
    public void GetAllAttributes()
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

        _allAttributeTree.Clear();

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
                        AttributeValueType = attribute.ValueType,
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

        _allAttributeTree.Add(documentNode);

        AddOrphanedAttributeSetsToTree(tree);

        ApplyTreeFilter();
    }

    private void AddOrphanedAttributeSetsToTree(AttributeDocumentInfo tree)
    {
        if (tree.OrphanedAttributeSets.Count > 0)
        {
            AttributeTreeNode orphanRoot = new()
            {
                Name = "Orphaned Attribute Sets",
                Value = $"({tree.OrphanedAttributeSets.Count})",
                NodeType = NodeType.OrphanRoot,
                IsExpanded = true,
                IconSource = AttributeTreeIconProvider.GetIcon(NodeType.OrphanRoot)
            };

            foreach (OrphanedAttributeSetInfo orphan in tree.OrphanedAttributeSets)
            {
                AttributeTreeNode orphanSetNode = new()
                {
                    Name = orphan.Name,
                    NodeType = NodeType.OrphanAttributeSet,
                    AttributeSetName = orphan.Name,
                    IconSource = AttributeTreeIconProvider.GetIcon(NodeType.OrphanAttributeSet),
                    Parent = orphanRoot
                };

                foreach (AttributeInfo attribute in orphan.Attributes)
                {
                    orphanSetNode.Children.Add(new AttributeTreeNode
                    {
                        Name = attribute.Name,
                        Value = $"{attribute.ValueType}: {attribute.Value}",
                        NodeType = NodeType.Attribute,
                        RawAttributeValue = attribute.Value,
                        AttributeValueType = attribute.ValueType,
                        AttributeSetName = orphan.Name,
                        AttributeName = attribute.Name,
                        IconSource = AttributeTreeIconProvider.GetIcon(NodeType.Attribute),
                        Parent = orphanSetNode
                    });
                }

                orphanRoot.Children.Add(orphanSetNode);
            }

            _allAttributeTree.Add(orphanRoot);
        }
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
            case NodeType.OrphanAttributeSet:
                DeleteOrphanAttributeSetNode(node);
                break;
        }
    }

    [RelayCommand]
    private async Task PurgeAllOrphans()
    {
        _Document? document = Globals.InvApp.ActiveDocument;
        if (document == null)
        {
            await userNotificationService.ShowErrorAsync(
                "Purge Orphaned Sets",
                "No active Inventor document found.").ConfigureAwait(false);
            return;
        }

        IReadOnlyList<OrphanedAttributeSetInfo> orphans =
            attributeService.GetOrphanedAttributeSets(document);

        if (orphans.Count == 0)
        {
            await userNotificationService.ShowInfoAsync(
                "Purge Orphaned Sets",
                "No orphaned attribute sets were found.").ConfigureAwait(false);
            return;
        }

        bool confirmed = await DialogHelper.ShowConfirmationAsync(
            "Purge Orphaned Sets",
            $"This will permanently purge {orphans.Count} orphaned attribute set(s). Continue?",
            "Purge").ConfigureAwait(true);

        if (!confirmed)
        {
            return;
        }

        int purged = 0;
        int failed = 0;

        foreach (OrphanedAttributeSetInfo orphan in orphans)
        {
            if (attributeService.PurgeOrphanedAttributeSet(document, orphan.Name))
            {
                purged++;
            }
            else
            {
                failed++;
            }
        }

        GetAllAttributes();

        string message = $"Purged {purged} orphaned attribute set(s).";
        if (failed > 0)
        {
            message += $" Failed: {failed}.";
            await userNotificationService.ShowWarningAsync(
                "Purge Orphaned Sets", message).ConfigureAwait(false);
            return;
        }

        if (settingsService.GetCopy().ShowConfirmationMessages)
        {
            await userNotificationService.ShowSuccessAsync(
                "Purge Orphaned Sets", message).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Edit is only possible on attribute nodes. It will open the same dialog as adding, but with prefilled values. After editing the tree is updated with the new values. Depending on the number of attributes this can be slow, so we try to just update the tree without full refresh.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task EditNode(AttributeTreeNode? node)
    {
        if (node is not { NodeType: NodeType.Attribute })
        {
            return;
        }

        AttributeTreeNode? realNode = FindNodeInFullTree(node);
        if (realNode?.OwnerObject == null ||
            string.IsNullOrWhiteSpace(realNode.AttributeSetName) ||
            string.IsNullOrWhiteSpace(realNode.AttributeName))
        {
            return;
        }

        AddAttributeDialog dialog = new();

        dialog.ViewModel.InitializeForEdit(
            realNode.AttributeSetName,
            realNode.AttributeName,
            realNode.AttributeValueType ?? ValueTypeEnum.kStringType,
            realNode.RawAttributeValue);

        DialogHelper.SetDialogTheme(dialog);

        bool? result = dialog.ShowDialog();
        if (result != true)
        {
            return;
        }

        AddAttributeDialogResult input = dialog.Result;

        object typedValue = ConvertToTypedValue(input.RawValue, input.ValueType);

        InventorAttribute? updatedAttribute = attributeService.AddOrUpdateAttribute(
            realNode.OwnerObject,
            input.AttributeSetName,
            input.AttributeName,
            input.ValueType,
            typedValue);

        if (updatedAttribute == null)
        {
            await userNotificationService.ShowErrorAsync(
                "Edit Attribute",
                "The attribute value could not be updated.").ConfigureAwait(false);
            return;
        }

        realNode.Value = $"{input.ValueType}: {input.RawValue}";
        realNode.RawAttributeValue = input.RawValue;
        ExpandNodePath(realNode);
        ApplyTreeFilter();

        if (settingsService.GetCopy().ShowConfirmationMessages)
        {
            await userNotificationService.ShowSuccessAsync(
                "Edit Attribute",
                $"Attribute '{input.AttributeName}' was updated.").ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private static void OpenSettings()
    {
        SettingsDialog settingsDialog = new();
        DialogHelper.SetDialogTheme(settingsDialog);

        settingsDialog.ShowDialog();
    }


    private AttributeTreeNode? FindOrphanNode(AttributeTreeNode target)
    {
        foreach (AttributeTreeNode root in _allAttributeTree)
        {
            AttributeTreeNode? match = FindNodeRecursive(root, target);
            if (match != null)
            {
                return match;
            }
        }
        return null;
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

    private void DeleteOrphanAttributeSetNode(AttributeTreeNode node)
    {
        if (string.IsNullOrWhiteSpace(node.AttributeSetName))
        {
            return;
        }

        AttributeTreeNode? realNode = FindOrphanNode(node);
        if (realNode == null)
        {
            return;
        }

        bool purged = attributeService.PurgeOrphanedAttributeSet(
            Globals.InvApp.ActiveDocument,
            realNode.AttributeSetName!);

        if (!purged)
        {
            DialogHelper.ShowInfoMessage(
                "Purge Attribute Set",
                "The orphaned attribute set could not be purged.");
            return;
        }

        AttributeTreeNode? parent = realNode.Parent;
        parent?.Children.Remove(realNode);

        // Remove OrphanRoot if empty
        if (parent is { NodeType: NodeType.OrphanRoot, Children.Count: 0 })
        {
            _allAttributeTree.Remove(parent);
        }

        ApplyTreeFilter();
    }

    private void DeleteAttributeNode(AttributeTreeNode node)
    {
        AttributeTreeNode? realNode = FindNodeInFullTree(node);
        if (realNode?.OwnerObject == null ||
            string.IsNullOrWhiteSpace(realNode.AttributeSetName) ||
            string.IsNullOrWhiteSpace(realNode.AttributeName))
        {
            return;
        }

        bool deleted = attributeService.DeleteAttribute(
            Globals.InvApp.ActiveDocument,
            realNode.OwnerObject,
            realNode.AttributeSetName,
            realNode.AttributeName);

        if (!deleted)
        {
            DialogHelper.ShowInfoMessage(
                "Delete Attribute",
                "The attribute could not be deleted.");
            return;
        }

        realNode.Parent?.Children.Remove(realNode);

        ApplyTreeFilter();
    }

    private void DeleteAttributeSetNode(AttributeTreeNode node)
    {
        AttributeTreeNode? realNode = FindNodeInFullTree(node);
        if (realNode == null ||
            realNode.OwnerObject == null ||
            string.IsNullOrWhiteSpace(realNode.AttributeSetName))
        {
            return;
        }

        bool deleted = attributeService.DeleteAttributeSet(
            Globals.InvApp.ActiveDocument,
            realNode.OwnerObject,
            realNode.AttributeSetName);

        if (!deleted)
        {
            DialogHelper.ShowInfoMessage(
                "Delete Attribute Set",
                "The attribute set could not be deleted.");
            return;
        }

        if (SelectedNode != null && NodesMatch(SelectedNode, realNode))
        {
            SelectedNode = null;
        }

        AttributeTreeNode? parentNode = realNode.Parent;
        parentNode?.Children.Remove(realNode);

        if (parentNode is { NodeType: NodeType.Owner, Children.Count: 0 })
        {
            if (SelectedNode != null && NodesMatch(SelectedNode, parentNode))
            {
                SelectedNode = null;
            }

            parentNode.Parent?.Children.Remove(parentNode);
        }

        ApplyTreeFilter();
    }

    private void AddAttributeToTree(
        object selectedObject,
        AddAttributeDialogResult input)
    {
        if (_allAttributeTree.Count == 0)
        {
            GetAllAttributes();
            return;
        }

        AttributeTreeNode documentNode = _allAttributeTree[0];

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
            AttributeValueType = input.ValueType,
            NodeType = NodeType.Attribute,
            OwnerObject = selectedObject,
            AttributeSetName = input.AttributeSetName,
            AttributeName = input.AttributeName,
            IconSource = AttributeTreeIconProvider.GetIcon(NodeType.Attribute),
            Parent = setNode
        });

        ApplyTreeFilter();
    }
    partial void OnSearchTextChanged(string value)
    {
        ApplyTreeFilter();
    }

    private void DeleteOwnerNode(AttributeTreeNode node)
    {
        AttributeTreeNode? realNode = FindNodeInFullTree(node);
        if (realNode?.OwnerObject == null)
        {
            return;
        }

        ObjectCollection objectCollection =
            Globals.InvApp.TransientObjects.CreateObjectCollection();

        objectCollection.Add(realNode.OwnerObject);

        DeleteAttributesResult result =
            attributeService.DeleteAllAttributes(
                objectCollection,
                settingsService.GetCopy().DeleteAutodeskDefaultAttributeSets);

        if (!result.HasChanges)
        {
            DialogHelper.ShowInfoMessage(
                "Delete Attributes",
                "No attributes were found on the selected owner object.");
            return;
        }

        if (SelectedNode != null && NodesMatch(SelectedNode, realNode))
        {
            SelectedNode = null;
        }

        realNode.Parent?.Children.Remove(realNode);
        ApplyTreeFilter();
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
    private void ApplyTreeFilter()
    {
        // Snapshot current visible state before wiping the tree
        _expandedNodeKeys.Clear();
        SnapshotExpandState(AttributeTree);

        AttributeTree.Clear();

        if (_allAttributeTree.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            foreach (AttributeTreeNode rootNode in _allAttributeTree)
            {
                AttributeTree.Add(CloneTree(rootNode, null));
            }
        }
        else
        {
            string search = SearchText.Trim();
            foreach (AttributeTreeNode rootNode in _allAttributeTree)
            {
                AttributeTreeNode? filteredRoot = FilterNode(rootNode, search, null);
                if (filteredRoot != null)
                    AttributeTree.Add(filteredRoot);
            }
        }

        RestoreExpandState(AttributeTree);
    }

    private static AttributeTreeNode? FilterNode(
        AttributeTreeNode node,
        string search,
        AttributeTreeNode? parent)
    {
        bool nodeMatches = NodeMatches(node, search);

        AttributeTreeNode clone = CloneShallow(node, parent);

        foreach (AttributeTreeNode child in node.Children)
        {
            AttributeTreeNode? filteredChild = FilterNode(child, search, clone);
            if (filteredChild != null)
            {
                clone.Children.Add(filteredChild);
            }
        }

        if (nodeMatches || clone.Children.Count > 0)
        {
            clone.IsExpanded = true;
            return clone;
        }

        return null;
    }

    private static bool NodeMatches(AttributeTreeNode node, string search)
    {
        return Contains(node.Name, search) ||
               Contains(node.Value, search);
    }

    private static bool Contains(string? source, string search)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               source.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static AttributeTreeNode CloneTree(
        AttributeTreeNode node,
        AttributeTreeNode? parent)
    {
        AttributeTreeNode clone = CloneShallow(node, parent);

        foreach (AttributeTreeNode child in node.Children)
        {
            clone.Children.Add(CloneTree(child, clone));
        }

        return clone;
    }

    private static AttributeTreeNode CloneShallow(
        AttributeTreeNode node,
        AttributeTreeNode? parent)
    {
        return new AttributeTreeNode
        {
            Name = node.Name,
            Value = node.Value,
            RawAttributeValue = node.RawAttributeValue,
            AttributeValueType = node.AttributeValueType,
            NodeType = node.NodeType,
            IsExpanded = node.IsExpanded,
            IconSource = node.IconSource,
            OwnerObject = node.OwnerObject,
            AttributeSetName = node.AttributeSetName,
            AttributeName = node.AttributeName,
            Parent = parent
        };
    }

    private AttributeTreeNode? FindNodeInFullTree(AttributeTreeNode node)
    {
        if (_allAttributeTree.Count == 0)
        {
            return null;
        }

        return FindNodeRecursive(_allAttributeTree[0], node);
    }

    private static AttributeTreeNode? FindNodeRecursive(
        AttributeTreeNode current,
        AttributeTreeNode target)
    {
        if (NodesMatch(current, target))
        {
            return current;
        }

        foreach (AttributeTreeNode child in current.Children)
        {
            AttributeTreeNode? result = FindNodeRecursive(child, target);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static bool NodesMatch(AttributeTreeNode left, AttributeTreeNode right)
    {
        if (left.NodeType != right.NodeType)
        {
            return false;
        }

        return left.NodeType switch
        {
            NodeType.Document =>
                string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase),

            NodeType.Owner =>
                ReferenceEquals(left.OwnerObject, right.OwnerObject),

            NodeType.AttributeSet =>
                ReferenceEquals(left.OwnerObject, right.OwnerObject) &&
                string.Equals(left.AttributeSetName, right.AttributeSetName,
                    StringComparison.OrdinalIgnoreCase),

            NodeType.Attribute =>
                ReferenceEquals(left.OwnerObject, right.OwnerObject) &&
                string.Equals(left.AttributeSetName, right.AttributeSetName,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(left.AttributeName, right.AttributeName,
                    StringComparison.OrdinalIgnoreCase),
            NodeType.OrphanRoot => true,

            NodeType.OrphanAttributeSet =>
                string.Equals(left.AttributeSetName, right.AttributeSetName,
                    StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static object ConvertToTypedValue(string rawValue, ValueTypeEnum valueType)
    {
        return valueType switch
        {
            ValueTypeEnum.kStringType => rawValue,
            ValueTypeEnum.kBooleanType => rawValue,
            ValueTypeEnum.kIntegerType => int.Parse(
                rawValue,
                CultureInfo.InvariantCulture),
            ValueTypeEnum.kDoubleType => double.Parse(
                rawValue,
                CultureInfo.InvariantCulture),
            ValueTypeEnum.kByteArrayType => Convert.FromBase64String(rawValue),
            _ => rawValue
        };
    }

    private static void ExpandNodePath(AttributeTreeNode node)
    {
        AttributeTreeNode? current = node;

        while (current != null)
        {
            current.IsExpanded = true;
            current = current.Parent;
        }
    }

    private void SnapshotExpandState(IEnumerable<AttributeTreeNode> nodes)
    {
        foreach (AttributeTreeNode node in nodes)
        {
            if (node.IsExpanded)
            {
                _expandedNodeKeys.Add(GetNodeKey(node));
            }

            SnapshotExpandState(node.Children);
        }
    }

    private void RestoreExpandState(IEnumerable<AttributeTreeNode> nodes)
    {
        foreach (AttributeTreeNode node in nodes)
        {
            node.IsExpanded = _expandedNodeKeys.Contains(GetNodeKey(node));
            RestoreExpandState(node.Children);
        }
    }

    private static string GetNodeKey(AttributeTreeNode node) =>
        node.NodeType switch
        {
            NodeType.Document => $"Doc|{node.Name}",
            NodeType.Owner => $"Owner|{node.OwnerObject?.GetHashCode()}",
            NodeType.AttributeSet =>
                $"Set|{node.OwnerObject?.GetHashCode()}|{node.AttributeSetName}",
            NodeType.Attribute =>
                $"Attr|{node.OwnerObject?.GetHashCode()}|{node.AttributeSetName}|{node.AttributeName}",
            _ => node.Name
        };
}


//TODO move tree search methods
//TODO add attached behavior for expanding
//TODO maybe try reapplying theme / style after editing tree