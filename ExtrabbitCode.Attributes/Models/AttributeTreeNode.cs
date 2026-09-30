using CommunityToolkit.Mvvm.ComponentModel;
using ExtrabbitCode.Attributes.Helper;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
// ReSharper disable InconsistentNaming

namespace ExtrabbitCode.Attributes.Models;

public partial class AttributeTreeNode : ObservableObject
{
    private const int FormattedValuePreviewMaxLines = 40;

    private const int CompactValueDisplayMaxLength = 80;

    private (StructuredTextKind Kind, string Formatted, string Compact)? _structuredValue;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayValue))]
    private string? value;

    [ObservableProperty]
    private NodeType nodeType = NodeType.Document;

    [ObservableProperty]
    private bool isExpanded;

    [ObservableProperty]
    private bool isHighlighted;

    [ObservableProperty]
    private ImageSource? iconSource;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStructuredValue))]
    [NotifyPropertyChangedFor(nameof(StructuredValueLabel))]
    [NotifyPropertyChangedFor(nameof(FormattedValuePreview))]
    [NotifyPropertyChangedFor(nameof(DisplayValue))]
    private string rawAttributeValue = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStructuredValue))]
    [NotifyPropertyChangedFor(nameof(StructuredValueLabel))]
    [NotifyPropertyChangedFor(nameof(FormattedValuePreview))]
    [NotifyPropertyChangedFor(nameof(DisplayValue))]
    private ValueTypeEnum? attributeValueType;

    public object? OwnerObject { get; init; }

    public string? AttributeSetName { get; init; }

    public string? AttributeName { get; init; }

    public ObservableCollection<AttributeTreeNode> Children { get; } = [];

    public AttributeTreeNode? Parent { get; set; }

    public bool CanEdit => NodeType is NodeType.Attribute;

    public bool CanDelete => NodeType is NodeType.Attribute or NodeType.AttributeSet;

    public bool CanCopyValue => NodeType == NodeType.Attribute;

    /// <summary>
    /// True if this is a string attribute whose value is well-formed JSON or XML.
    /// </summary>
    public bool IsStructuredValue => GetStructuredValue().Kind != StructuredTextKind.None;

    public string StructuredValueLabel => GetStructuredValue().Kind switch
    {
        StructuredTextKind.Json => "JSON",
        StructuredTextKind.Xml => "XML",
        _ => string.Empty
    };

    /// <summary>
    /// The value as indented JSON/XML, or the raw value if it is not structured.
    /// </summary>
    public string FormattedValue => GetStructuredValue().Formatted;

    /// <summary>
    /// The indented JSON/XML value, truncated so it fits into a tooltip.
    /// </summary>
    public string? FormattedValuePreview
    {
        get
        {
            if (!IsStructuredValue)
            {
                return null;
            }

            string[] lines = FormattedValue.Split('\n');
            return lines.Length <= FormattedValuePreviewMaxLines
                ? FormattedValue
                : string.Join('\n', lines.Take(FormattedValuePreviewMaxLines)) + "\n…";
        }
    }

    /// <summary>
    /// The text shown next to the node name. JSON/XML values are shown on a single, shortened
    /// line regardless of how they are stored, so indented values do not blow up the tree.
    /// </summary>
    public string? DisplayValue
    {
        get
        {
            if (!IsStructuredValue)
            {
                return Value;
            }

            string compact = GetStructuredValue().Compact;
            if (compact.Length > CompactValueDisplayMaxLength)
            {
                compact = compact[..CompactValueDisplayMaxLength] + "…";
            }

            return $"{AttributeValueType}: {compact}";
        }
    }

    partial void OnRawAttributeValueChanged(string value) => _structuredValue = null;

    partial void OnAttributeValueTypeChanged(ValueTypeEnum? value) => _structuredValue = null;

    private (StructuredTextKind Kind, string Formatted, string Compact) GetStructuredValue()
    {
        if (_structuredValue is { } cached)
        {
            return cached;
        }

        _structuredValue =
            NodeType == NodeType.Attribute &&
            AttributeValueType == ValueTypeEnum.kStringType &&
            StructuredTextFormatter.TryFormat(RawAttributeValue, indented: true, out string formatted, out StructuredTextKind kind, out _) &&
            StructuredTextFormatter.TryFormat(RawAttributeValue, indented: false, out string compact, out _, out _)
                ? (kind, formatted, compact)
                : (StructuredTextKind.None, RawAttributeValue, RawAttributeValue);

        return _structuredValue.Value;
    }
}
