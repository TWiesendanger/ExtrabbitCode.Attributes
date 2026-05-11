using ExtrabbitCode.Inventor.Attributes.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ExtrabbitCode.Inventor.Attributes.Helper.AttributeTree;

internal static class AttributeTreeFilter
{
    internal static void Apply(
        string searchText,
        HashSet<string> expandedNodeKeys,
        List<AttributeTreeNode> allAttributeTree,
        ObservableCollection<AttributeTreeNode> attributeTree,
        AttributeTreeNode? forceExpandNode = null)
    {
        expandedNodeKeys.Clear();
        SnapshotExpandState(attributeTree, expandedNodeKeys);

        AttributeTreeNode? current = forceExpandNode;
        while (current != null)
        {
            expandedNodeKeys.Add(GetNodeKey(current));
            current = current.Parent;
        }

        attributeTree.Clear();

        if (allAttributeTree.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            foreach (AttributeTreeNode rootNode in allAttributeTree)
            {
                attributeTree.Add(CloneTree(rootNode, null));
            }
        }
        else
        {
            string search = searchText.Trim();
            foreach (AttributeTreeNode rootNode in allAttributeTree)
            {
                AttributeTreeNode? filteredRoot = FilterNode(rootNode, search, null);
                if (filteredRoot != null)
                {
                    attributeTree.Add(filteredRoot);
                }
            }
        }

        RestoreExpandState(attributeTree, expandedNodeKeys);
    }

    internal static AttributeTreeNode? FindInFullTree(
        AttributeTreeNode target,
        List<AttributeTreeNode> allAttributeTree)
    {
        if (allAttributeTree.Count == 0)
        {
            return null;
        }

        return FindRecursive(allAttributeTree[0], target);
    }

    internal static AttributeTreeNode? FindOrphan(
        AttributeTreeNode target,
        List<AttributeTreeNode> allAttributeTree)
    {
        foreach (AttributeTreeNode root in allAttributeTree)
        {
            AttributeTreeNode? match = FindRecursive(root, target);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    internal static bool NodesMatch(AttributeTreeNode left, AttributeTreeNode right)
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

    internal static void ExpandNodePath(AttributeTreeNode node)
    {
        AttributeTreeNode? current = node;
        while (current != null)
        {
            current.IsExpanded = true;
            current = current.Parent;
        }
    }

    internal static void SetExpandedAll(IEnumerable<AttributeTreeNode> nodes, bool expanded)
    {
        foreach (AttributeTreeNode node in nodes)
        {
            node.IsExpanded = expanded;
            SetExpandedAll(node.Children, expanded);
        }
    }

    private static AttributeTreeNode? FilterNode(
        AttributeTreeNode node,
        string search,
        AttributeTreeNode? parent)
    {
        AttributeTreeNode clone = CloneShallow(node, parent);

        foreach (AttributeTreeNode child in node.Children)
        {
            AttributeTreeNode? filteredChild = FilterNode(child, search, clone);
            if (filteredChild != null)
            {
                clone.Children.Add(filteredChild);
            }
        }

        bool nodeMatches = NodeMatches(node, search);
        if (nodeMatches || clone.Children.Count > 0)
        {
            clone.IsExpanded = true;
            return clone;
        }

        return null;
    }

    private static bool NodeMatches(AttributeTreeNode node, string search) =>
        Contains(node.Name, search) || Contains(node.Value, search);

    private static bool Contains(string? source, string search) =>
        !string.IsNullOrWhiteSpace(source) &&
        source.Contains(search, StringComparison.OrdinalIgnoreCase);

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
        AttributeTreeNode? parent) =>
        new()
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

    private static AttributeTreeNode? FindRecursive(
        AttributeTreeNode current,
        AttributeTreeNode target)
    {
        if (NodesMatch(current, target))
        {
            return current;
        }

        foreach (AttributeTreeNode child in current.Children)
        {
            AttributeTreeNode? result = FindRecursive(child, target);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static void SnapshotExpandState(
        IEnumerable<AttributeTreeNode> nodes,
        HashSet<string> expandedNodeKeys)
    {
        foreach (AttributeTreeNode node in nodes)
        {
            if (node.IsExpanded)
            {
                expandedNodeKeys.Add(GetNodeKey(node));
            }

            SnapshotExpandState(node.Children, expandedNodeKeys);
        }
    }

    private static void RestoreExpandState(
        IEnumerable<AttributeTreeNode> nodes,
        HashSet<string> expandedNodeKeys)
    {
        foreach (AttributeTreeNode node in nodes)
        {
            node.IsExpanded = expandedNodeKeys.Contains(GetNodeKey(node));
            RestoreExpandState(node.Children, expandedNodeKeys);
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