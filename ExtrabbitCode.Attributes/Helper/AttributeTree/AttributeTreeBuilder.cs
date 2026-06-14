using ExtrabbitCode.Attributes.Models;
using ExtrabbitCode.Attributes.Services;
using ExtrabbitCode.Attributes.Services.AttributeModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ExtrabbitCode.Attributes.Helper.AttributeTree;

internal static class AttributeTreeBuilder
{
    internal static void Build(
        _Document document,
        AttributeService attributeService,
        List<AttributeTreeNode> allAttributeTree,
        ObservableCollection<AttributeTreeNode> attributeTree)
    {
        AttributeDocumentInfo? tree = attributeService.GetAttributeTree(document);
        if (tree == null)
        {
            return;
        }

        allAttributeTree.Clear();

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

        allAttributeTree.Add(documentNode);

        AddOrphanedAttributeSets(tree, allAttributeTree);
    }

    internal static void AddOrphanedAttributeSets(
        AttributeDocumentInfo tree,
        List<AttributeTreeNode> allAttributeTree)
    {
        if (tree.OrphanedAttributeSets.Count == 0)
        {
            return;
        }

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

        allAttributeTree.Add(orphanRoot);
    }
}