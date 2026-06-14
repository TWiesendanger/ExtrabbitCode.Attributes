using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExtrabbitCode.Attributes.Helper;
using ExtrabbitCode.Attributes.Helper.AttributeTree;
using ExtrabbitCode.Attributes.Models;
using ExtrabbitCode.Attributes.Services;
using ExtrabbitCode.Attributes.UI.Dialog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

// ReSharper disable InconsistentNaming

namespace ExtrabbitCode.Attributes.UI.ViewModels;

public partial class AttributeWindowViewModel(SettingsService settingsService,
    AttributeService attributeService,
    UserNotificationService userNotificationService) : ObservableObject
{
    private readonly HashSet<string> _expandedNodeKeys = [];
    private readonly List<AttributeTreeNode> _allAttributeTree = [];
    private AttributeTreeNode? _pendingExpandNode;

    public ObservableCollection<AttributeTreeNode> AttributeTree { get; } = [];

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private AttributeTreeNode? selectedNode;

    public void RefreshAttributes() => GetAllAttributesCommand.Execute(null);

    public void ApplyExternalAttributeAdded(AddAttributeWorkflowResult result)
    {
        if (result is { OwnerObject: not null, Input: not null })
        {
            _pendingExpandNode = AttributeTreeMutations.AddAttributeToTree(
                result.OwnerObject,
                result.Input,
                _allAttributeTree,
                GetAllAttributes);
        }
        ApplyFilterWithHighlight();
    }

    [RelayCommand]
    private async Task AddAttribute()
    {
        Globals.TelemetryService.TrackEvent("attribute_add_started");

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
            _pendingExpandNode = AttributeTreeMutations.AddAttributeToTree(
                workflowResult.OwnerObject,
                workflowResult.Input,
                _allAttributeTree,
                GetAllAttributes);
        }

        Globals.TelemetryService.TrackEvent("attribute_add_succeeded",
            new Dictionary<string, object>
            {
                ["value_type"] = workflowResult.Input?.ValueType.ToString() ?? "unknown"
            });

        if (settingsService.GetCopy().ShowConfirmationMessages &&
            workflowResult.Input != null)
        {
            await userNotificationService.ShowSuccessAsync(
                    "Add Attribute",
                    $"Attribute '{workflowResult.Input.AttributeName}' was added to set '{workflowResult.Input.AttributeSetName}'.")
                .ConfigureAwait(false);
        }

        ApplyFilterWithHighlight();
    }

    [RelayCommand]
    private async Task CopyNodeValue(AttributeTreeNode? node)
    {
        if (node is not { CanCopyValue: true } || string.IsNullOrWhiteSpace(node.Value))
        {
            return;
        }

        Clipboard.SetText(node.RawAttributeValue);
        Globals.TelemetryService.TrackEvent("attribute_value_copied",
            new Dictionary<string, object>
            {
                ["value_type"] = node.AttributeValueType?.ToString() ?? "unknown"
            });

        if (settingsService.GetCopy().ShowConfirmationMessages)
        {
            await userNotificationService.ShowSuccessAsync(
                "Copied",
                $"'{node.RawAttributeValue}' copied to clipboard.").ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task SelectNodeObject(AttributeTreeNode? node)
    {
        if (node is null or { NodeType: NodeType.Document })
        {
            return;
        }

        Globals.TelemetryService.TrackEvent("attribute_select_object_started",
            new Dictionary<string, object>
            {
                ["node_type"] = node.NodeType.ToString()
            });

        object? targetObject = node.NodeType switch
        {
            NodeType.Owner or NodeType.AttributeSet or NodeType.Attribute => node.OwnerObject,
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
            Globals.TelemetryService.TrackEvent("attribute_select_object_succeeded",
                new Dictionary<string, object>
                {
                    ["node_type"] = node.NodeType.ToString()
                });
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
        Globals.TelemetryService.TrackEvent("attribute_delete_selected_started");

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

        ObjectCollection selectedObjects =
            Globals.InvApp.TransientObjects.CreateObjectCollection();
        foreach (object obj in Globals.InvApp.ActiveDocument.SelectSet)
        {
            selectedObjects.Add(obj);
        }

        DeleteAttributesResult result = attributeService.DeleteAllAttributes(
            selectedObjects,
            settingsService.GetCopy().DeleteAutodeskDefaultAttributeSets);

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

        AttributeTreeNode? mutableSelectedNode = SelectedNode;
        AttributeTreeMutations.RemoveOwnerNodes(
            selectedObjects,
            _allAttributeTree,
            ref mutableSelectedNode);
        SelectedNode = mutableSelectedNode;
        ApplyFilter();

        if (result.FailedObjects > 0)
        {
            message += $"\nFailed objects: {result.FailedObjects}.";
            await userNotificationService.ShowErrorAsync(
                "Delete Attributes", message).ConfigureAwait(false);
            return;
        }

        Globals.TelemetryService.TrackEvent("attribute_delete_selected_succeeded",
            new Dictionary<string, object>
            {
                ["deleted_attributes"] = result.DeletedAttributes,
                ["deleted_sets"] = result.DeletedAttributeSets,
                ["affected_objects"] = result.AffectedObjects
            });

        if (settingsService.GetCopy().ShowConfirmationMessages)
        {
            await userNotificationService.ShowSuccessAsync(
                "Delete Attributes", message).ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task DeleteAll()
    {
        Globals.TelemetryService.TrackEvent("attribute_delete_all_started");

        await AttributeTreeMutations.DeleteAllAttributesInDocumentAsync(
            "Delete All Attributes",
            "This will delete all attributes in the active document. Do you want to continue?",
            attributeService,
            userNotificationService,
            settingsService,
            GetAllAttributes).ConfigureAwait(false);

        Globals.TelemetryService.TrackEvent("attribute_delete_all_succeeded");
    }

    [RelayCommand]
    public void GetAllAttributes()
    {
        Globals.TelemetryService.TrackEvent("attribute_refresh_started");

        _Document document = Globals.InvApp.ActiveDocument;
        if (document == null)
        {
            DialogHelper.ShowInfoMessage(
                "Refresh Attributes",
                "No active Inventor document found.");
            return;
        }

        AttributeTreeBuilder.Build(
            document,
            attributeService,
            _allAttributeTree,
            AttributeTree);

        ApplyFilter();

        Globals.TelemetryService.TrackEvent("attribute_refresh_succeeded",
            new Dictionary<string, object>
            {
                ["node_count"] = _allAttributeTree.Count
            });
    }

    [RelayCommand]
    private async Task DeleteNode(AttributeTreeNode? node)
    {
        if (node == null)
        {
            return;
        }

        Globals.TelemetryService.TrackEvent("attribute_delete_node",
            new Dictionary<string, object>
            {
                ["node_type"] = node.NodeType.ToString()
            });

        AttributeTreeNode? mutableSelectedNode = SelectedNode;

        switch (node.NodeType)
        {
            case NodeType.Attribute:
                AttributeTreeMutations.DeleteAttribute(
                    node, _allAttributeTree, attributeService);
                break;
            case NodeType.AttributeSet:
                AttributeTreeMutations.DeleteAttributeSet(
                    node, _allAttributeTree, attributeService, ref mutableSelectedNode);
                break;
            case NodeType.Owner:
                AttributeTreeMutations.DeleteOwner(
                    node, _allAttributeTree, attributeService, settingsService, ref mutableSelectedNode);
                break;
            case NodeType.Document:
                    await AttributeTreeMutations.DeleteAllAttributesInDocumentAsync(
                        "Delete All Attributes",
                        $"This will delete all attributes in document '{node.Name}'. Do you want to continue?",
                        attributeService,
                        userNotificationService,
                        settingsService,
                        GetAllAttributes).ConfigureAwait(false);
                    Globals.TelemetryService.TrackEvent("attribute_delete_node_succeeded",
                        new Dictionary<string, object>
                        {
                            ["node_type"] = node.NodeType.ToString()
                        });
                    return;
            case NodeType.OrphanRoot:
                await AttributeTreeMutations.PurgeAllOrphansAsync(
                    attributeService,
                    userNotificationService,
                    settingsService,
                    GetAllAttributes).ConfigureAwait(false);
                Globals.TelemetryService.TrackEvent("attribute_delete_node_succeeded",
                    new Dictionary<string, object>
                    {
                        ["node_type"] = node.NodeType.ToString()
                    });
                return;
            case NodeType.OrphanAttributeSet:
                AttributeTreeMutations.DeleteOrphanAttributeSet(
                    node, _allAttributeTree, attributeService);
                break;
        }

        SelectedNode = mutableSelectedNode;
        Globals.TelemetryService.TrackEvent("attribute_delete_node_succeeded",
            new Dictionary<string, object>
            {
                ["node_type"] = node.NodeType.ToString()
            });
        ApplyFilter();
    }

    [RelayCommand]
    private async Task PurgeAllOrphans()
    {
        Globals.TelemetryService.TrackEvent("attribute_purge_orphans_started");

        await AttributeTreeMutations.PurgeAllOrphansAsync(
            attributeService,
            userNotificationService,
            settingsService,
            GetAllAttributes).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task EditNode(AttributeTreeNode? node)
    {
        if (node is not { NodeType: NodeType.Attribute })
        {
            return;
        }

        Globals.TelemetryService.TrackEvent("attribute_edit_started");

        AttributeTreeNode? realNode = AttributeTreeFilter.FindInFullTree(node, _allAttributeTree);
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
        object typedValue = AttributeValueConverter.ConvertToTypedValue(
            input.RawValue, input.ValueType);

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
        AttributeTreeFilter.ExpandNodePath(realNode);
        ApplyFilter();

        Globals.TelemetryService.TrackEvent("attribute_edit_succeeded",
            new Dictionary<string, object>
            {
                ["value_type"] = input.ValueType.ToString()
            });

        if (settingsService.GetCopy().ShowConfirmationMessages)
        {
            await userNotificationService.ShowSuccessAsync(
                "Edit Attribute",
                $"Attribute '{input.AttributeName}' was updated.").ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private void ExpandAllNodes()
    {
        Globals.TelemetryService.TrackEvent("attribute_tree_expand_all");
        AttributeTreeFilter.SetExpandedAll(AttributeTree, true);
        ApplyFilter();
    }

    [RelayCommand]
    private void CollapseAllNodes()
    {
        Globals.TelemetryService.TrackEvent("attribute_tree_collapse_all");
        AttributeTreeFilter.SetExpandedAll(AttributeTree, false);
        ApplyFilter();
    }

    [RelayCommand]
    private static void OpenSettings()
    {
        Globals.TelemetryService.TrackEvent("settings_opened");

        SettingsDialog settingsDialog = new();
        DialogHelper.SetDialogTheme(settingsDialog);
        settingsDialog.ShowDialog();

        Globals.TelemetryService.TrackEvent("settings_closed");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        AttributeTreeFilter.Apply(
            SearchText,
            _expandedNodeKeys,
            _allAttributeTree,
            AttributeTree,
            _pendingExpandNode);
        _pendingExpandNode = null;
    }

    private void ApplyFilterWithHighlight()
    {
        AttributeTreeNode? sourceNode = _pendingExpandNode;
        ApplyFilter();
        if (sourceNode == null) return;
        AttributeTreeNode? visibleNode = FindVisibleNode(sourceNode);
        if (visibleNode != null)
            _ = HighlightNodeAsync(visibleNode);
    }

    private AttributeTreeNode? FindVisibleNode(AttributeTreeNode sourceNode)
    {
        List<AttributeTreeNode> visibleList = [.. AttributeTree];
        return AttributeTreeFilter.FindInFullTree(sourceNode, visibleList);
    }

    private static async Task HighlightNodeAsync(AttributeTreeNode node)
    {
        node.IsHighlighted = true;
        await Task.Delay(5000).ConfigureAwait(true);
        node.IsHighlighted = false;
    }
}