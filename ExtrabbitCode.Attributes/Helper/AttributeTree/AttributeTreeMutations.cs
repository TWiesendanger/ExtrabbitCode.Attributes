using ExtrabbitCode.Attributes.Models;
using ExtrabbitCode.Attributes.Services;
using ExtrabbitCode.Attributes.Services.AttributeModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExtrabbitCode.Attributes.Helper.AttributeTree;

internal static class AttributeTreeMutations
{
    internal static AttributeTreeNode? AddAttributeToTree(
        object selectedObject,
        AddAttributeDialogResult input,
        List<AttributeTreeNode> allAttributeTree,
        System.Action refreshAll)
    {
        if (allAttributeTree.Count == 0)
        {
            refreshAll();
            return FindAttributeNode(allAttributeTree, selectedObject, input.AttributeSetName, input.AttributeName);
        }

        AttributeTreeNode documentNode = allAttributeTree[0];

        AttributeTreeNode? ownerNode = documentNode.Children
            .FirstOrDefault(x => ReferenceEquals(x.OwnerObject, selectedObject));

        if (ownerNode == null)
        {
            refreshAll();
            return FindAttributeNode(allAttributeTree, selectedObject, input.AttributeSetName, input.AttributeName);
        }

        AttributeTreeNode? setNode = ownerNode.Children
            .FirstOrDefault(x =>
                x.NodeType == NodeType.AttributeSet &&
                string.Equals(x.AttributeSetName, input.AttributeSetName,
                    System.StringComparison.OrdinalIgnoreCase));

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
                string.Equals(x.AttributeName, input.AttributeName,
                    System.StringComparison.OrdinalIgnoreCase));

        string valueText = $"{input.ValueType}: {input.RawValue}";

        if (existingAttributeNode != null)
        {
            existingAttributeNode.Value = valueText;
            existingAttributeNode.RawAttributeValue = input.RawValue;
            return existingAttributeNode;
        }

        AttributeTreeNode newNode = new()
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
        };
        setNode.Children.Add(newNode);
        return newNode;
    }

    private static AttributeTreeNode? FindAttributeNode(
        List<AttributeTreeNode> allAttributeTree,
        object ownerObject,
        string attributeSetName,
        string attributeName)
    {
        if (allAttributeTree.Count == 0)
        {
            return null;
        }

        AttributeTreeNode? ownerNode = allAttributeTree[0].Children
            .FirstOrDefault(x => ReferenceEquals(x.OwnerObject, ownerObject));

        AttributeTreeNode? setNode = ownerNode?.Children
            .FirstOrDefault(x =>
                x.NodeType == NodeType.AttributeSet &&
                string.Equals(x.AttributeSetName, attributeSetName,
                    System.StringComparison.OrdinalIgnoreCase));
        if (setNode == null)
        {
            return null;
        }

        return setNode.Children.FirstOrDefault(x =>
            x.NodeType == NodeType.Attribute &&
            string.Equals(x.AttributeName, attributeName,
                System.StringComparison.OrdinalIgnoreCase));
    }

    internal static void DeleteAttribute(
        AttributeTreeNode node,
        List<AttributeTreeNode> allAttributeTree,
        AttributeService attributeService)
    {
        AttributeTreeNode? realNode = AttributeTreeFilter.FindInFullTree(node, allAttributeTree);
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
    }

    internal static void DeleteAttributeSet(
        AttributeTreeNode node,
        List<AttributeTreeNode> allAttributeTree,
        AttributeService attributeService,
        ref AttributeTreeNode? selectedNode)
    {
        AttributeTreeNode? realNode = AttributeTreeFilter.FindInFullTree(node, allAttributeTree);
        if (realNode?.OwnerObject == null ||
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

        if (selectedNode != null && AttributeTreeFilter.NodesMatch(selectedNode, realNode))
        {
            selectedNode = null;
        }

        AttributeTreeNode? parentNode = realNode.Parent;
        parentNode?.Children.Remove(realNode);

        if (parentNode is { NodeType: NodeType.Owner, Children.Count: 0 })
        {
            if (selectedNode != null && AttributeTreeFilter.NodesMatch(selectedNode, parentNode))
            {
                selectedNode = null;
            }

            parentNode.Parent?.Children.Remove(parentNode);
        }
    }

    internal static void DeleteOwner(
        AttributeTreeNode node,
        List<AttributeTreeNode> allAttributeTree,
        AttributeService attributeService,
        SettingsService settingsService,
        ref AttributeTreeNode? selectedNode)
    {
        AttributeTreeNode? realNode = AttributeTreeFilter.FindInFullTree(node, allAttributeTree);
        if (realNode?.OwnerObject == null)
        {
            return;
        }

        ObjectCollection objectCollection =
            Globals.InvApp.TransientObjects.CreateObjectCollection();
        objectCollection.Add(realNode.OwnerObject);

        DeleteAttributesResult result = attributeService.DeleteAllAttributes(
            objectCollection,
            settingsService.GetCopy().DeleteAutodeskDefaultAttributeSets);

        if (!result.HasChanges)
        {
            DialogHelper.ShowInfoMessage(
                "Delete Attributes",
                "No attributes were found on the selected owner object.");
            return;
        }

        if (selectedNode != null && AttributeTreeFilter.NodesMatch(selectedNode, realNode))
        {
            selectedNode = null;
        }

        realNode.Parent?.Children.Remove(realNode);
    }

    internal static void RemoveOwnerNodes(
        ObjectCollection deletedObjects,
        List<AttributeTreeNode> allAttributeTree,
        ref AttributeTreeNode? selectedNode)
    {
        if (allAttributeTree.Count == 0)
        {
            return;
        }

        AttributeTreeNode documentNode = allAttributeTree[0];

        for (int i = documentNode.Children.Count - 1; i >= 0; i--)
        {
            AttributeTreeNode ownerNode = documentNode.Children[i];
            bool matched = false;
            foreach (object obj in deletedObjects)
            {
                if (ReferenceEquals(ownerNode.OwnerObject, obj))
                {
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                continue;
            }

            if (selectedNode != null && AttributeTreeFilter.NodesMatch(selectedNode, ownerNode))
            {
                selectedNode = null;
            }

            documentNode.Children.RemoveAt(i);
        }
    }

    internal static void DeleteOrphanAttributeSet(
        AttributeTreeNode node,
        List<AttributeTreeNode> allAttributeTree,
        AttributeService attributeService)
    {
        if (string.IsNullOrWhiteSpace(node.AttributeSetName))
        {
            return;
        }

        AttributeTreeNode? realNode = AttributeTreeFilter.FindOrphan(node, allAttributeTree);
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

        if (parent is { NodeType: NodeType.OrphanRoot, Children.Count: 0 })
        {
            allAttributeTree.Remove(parent);
        }
    }

    internal static async Task PurgeAllOrphansAsync(
        AttributeService attributeService,
        UserNotificationService userNotificationService,
        SettingsService settingsService,
        System.Action refreshAll)
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

        int purged = attributeService.PurgeAllOrphanedAttributeSets(document);

        refreshAll();

        if (purged == 0)
        {
            await userNotificationService.ShowWarningAsync(
                "Purge Orphaned Sets",
                "No orphaned attribute sets could be purged.").ConfigureAwait(false);
            return;
        }

        if (settingsService.GetCopy().ShowConfirmationMessages)
        {
            await userNotificationService.ShowSuccessAsync(
                "Purge Orphaned Sets",
                $"Purged {purged} orphaned attribute set(s).").ConfigureAwait(false);
        }
    }

    internal static async Task DeleteAllAttributesInDocumentAsync(
        string title,
        string confirmationMessage,
        AttributeService attributeService,
        UserNotificationService userNotificationService,
        SettingsService settingsService,
        System.Action refreshAll)
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

        DeleteAttributesResult result = attributeService.DeleteAllAttributes(
            objectsWithAttributes,
            settingsService.GetCopy().DeleteAutodeskDefaultAttributeSets);

        if (!result.HasChanges)
        {
            await userNotificationService.ShowInfoAsync(
                title,
                "No attributes were found in the active document.").ConfigureAwait(false);
            return;
        }

        refreshAll();

        string message =
            $"Deleted {result.DeletedAttributes} attribute(s) and " +
            $"{result.DeletedAttributeSets} attribute set(s) on " +
            $"{result.AffectedObjects} object(s).";

        if (result.FailedObjects > 0)
        {
            message += $" Failed objects: {result.FailedObjects}.";
            if (settingsService.GetCopy().ShowConfirmationMessages)
            {
                await userNotificationService.ShowWarningAsync(title, message).ConfigureAwait(false);
            }

            return;
        }

        if (settingsService.GetCopy().ShowConfirmationMessages)
        {
            await userNotificationService.ShowSuccessAsync(title, message).ConfigureAwait(false);
        }
    }
}