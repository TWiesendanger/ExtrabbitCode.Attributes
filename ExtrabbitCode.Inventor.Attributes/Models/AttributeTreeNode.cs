using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media;
// ReSharper disable InconsistentNaming

namespace ExtrabbitCode.Inventor.Attributes.Models;

public partial class AttributeTreeNode : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string? value;

    [ObservableProperty]
    private NodeType nodeType = NodeType.Document;

    [ObservableProperty]
    private bool isExpanded;

    [ObservableProperty]
    private ImageSource? iconSource;

    public object? OwnerObject { get; init; }

    public string? AttributeSetName { get; init; }

    public string? AttributeName { get; init; }

    public string RawAttributeValue { get; set; } = string.Empty;

    public ValueTypeEnum? AttributeValueType { get; set; }

    public ObservableCollection<AttributeTreeNode> Children { get; } = [];

    public AttributeTreeNode? Parent { get; set; }

    public bool CanEdit => NodeType is NodeType.Attribute;

    public bool CanDelete => NodeType is NodeType.Attribute or NodeType.AttributeSet;

    public bool CanCopyValue => NodeType == NodeType.Attribute;
}