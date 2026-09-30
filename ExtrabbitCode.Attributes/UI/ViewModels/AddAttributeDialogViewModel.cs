using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExtrabbitCode.Attributes.Helper;
using ExtrabbitCode.Attributes.Models;
using ExtrabbitCode.Attributes.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

// ReSharper disable InconsistentNaming

namespace ExtrabbitCode.Attributes.UI.ViewModels;

public partial class AddAttributeDialogViewModel : ObservableValidator
{
    [ObservableProperty]
    private AttributeDialogMode dialogMode = AttributeDialogMode.Add;

    public bool IsEditMode => DialogMode == AttributeDialogMode.EditValue;

    public bool CanEditAttributeSetName => !IsEditMode;

    public bool CanEditAttributeName => !IsEditMode;

    public bool CanEditValueType => !IsEditMode;

    public string DialogTitle =>
        IsEditMode ? "Edit Attribute Value" : "Add Attribute";

    public AddAttributeDialogResult? Result { get; private set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Attribute set name is required.")]
    [RegularExpression(
        "^[A-Za-z]+$",
        ErrorMessage = "Only letters are allowed. No spaces, digits, or special characters.")]
    private string attributeSetName = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Attribute name is required.")]
    [RegularExpression(
        "^[A-Za-z]+$",
        ErrorMessage = "Only letters are allowed. No spaces, digits, or special characters.")]
    private string attributeName = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(
        typeof(AddAttributeDialogViewModel),
        nameof(ValidateAttributeValue))]
    private string attributeValue = string.Empty;

    [ObservableProperty]
    private ValueTypeEnum selectedValueType =
        ValueTypeEnum.kStringType;

    public ObservableCollection<string> AttributeSetNameLibrary { get; } = [];

    public ObservableCollection<bool> BooleanValues { get; } =
    [
        true,
        false
    ];

    [ObservableProperty]
    private bool? selectedBooleanValue;

    private readonly AttributeLibraryService _attributeLibraryService;

    public ObservableCollection<ValueTypeEnum> ValueTypes { get; } =
    [
        ValueTypeEnum.kStringType,
        ValueTypeEnum.kBooleanType,
        ValueTypeEnum.kDoubleType,
        ValueTypeEnum.kIntegerType,
        ValueTypeEnum.kByteArrayType,
    ];

    public bool IsBooleanType => SelectedValueType == ValueTypeEnum.kBooleanType;

    public bool IsTextValueType => SelectedValueType != ValueTypeEnum.kBooleanType;

    public bool IsStringType => SelectedValueType == ValueTypeEnum.kStringType;

    public bool IsSingleLineTextType => IsTextValueType && !IsStringType;

    /// <summary>
    /// The value as stored in the document before editing. Null in add mode.
    /// </summary>
    private string? _originalAttributeValue;

    [ObservableProperty]
    private StructuredTextKind structuredValueKind;

    [ObservableProperty]
    private string structuredValueStatus = string.Empty;

    [ObservableProperty]
    private bool isStructuredValueValid;

    public bool IsStructuredValueCandidate => StructuredValueKind != StructuredTextKind.None;

    public AddAttributeDialogViewModel(AttributeLibraryService attributeLibraryService)
    {
        _attributeLibraryService = attributeLibraryService;
        LoadAttributeSetNameLibrary();
    }

    private void LoadAttributeSetNameLibrary()
    {
        AttributeSetNameLibrary.Clear();
        
        foreach (string name in _attributeLibraryService.GetAttributeSetNames())
        {
            AttributeSetNameLibrary.Add(name);
        }
    }

    public void InitializeForEdit(
        string attributeSetNameInitial,
        string attributeNameInitial,
        ValueTypeEnum valueType,
        string attributeValueInitial)
    {
        DialogMode = AttributeDialogMode.EditValue;

        AttributeSetName = attributeSetNameInitial;
        AttributeName = attributeNameInitial;
        SelectedValueType = valueType;
        _originalAttributeValue = attributeValueInitial;

        // Show JSON/XML values indented so they are readable. GetValueToStore() restores
        // the original layout on save, so opening and saving never changes the stored value.
        AttributeValue =
            valueType == ValueTypeEnum.kStringType &&
            StructuredTextFormatter.TryFormat(attributeValueInitial, indented: true, out string formatted, out _, out _)
                ? formatted
                : attributeValueInitial;

        if (valueType == ValueTypeEnum.kBooleanType &&
            bool.TryParse(attributeValueInitial, out bool boolValue))
        {
            SelectedBooleanValue = boolValue;
        }

        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(CanEditAttributeSetName));
        OnPropertyChanged(nameof(CanEditAttributeName));
        OnPropertyChanged(nameof(CanEditValueType));
        OnPropertyChanged(nameof(CanSubmit));
    }
    partial void OnDialogModeChanged(AttributeDialogMode value)
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(CanEditAttributeSetName));
        OnPropertyChanged(nameof(CanEditAttributeName));
        OnPropertyChanged(nameof(CanEditValueType));
        OnPropertyChanged(nameof(DialogTitle));
    }

    partial void OnSelectedBooleanValueChanged(bool? value)
    {
        AttributeValue = value?.ToString() ?? string.Empty;
        OnPropertyChanged(nameof(CanSubmit));
    }

    partial void OnAttributeSetNameChanged(string value)
    {
        ValidateProperty(value, nameof(AttributeSetName));
        OnPropertyChanged(nameof(CanSubmit));
    }

    partial void OnAttributeNameChanged(string value)
    {
        ValidateProperty(value, nameof(AttributeName));
        OnPropertyChanged(nameof(CanSubmit));
    }

    partial void OnAttributeValueChanged(string value)
    {
        ValidateProperty(value, nameof(AttributeValue));
        UpdateStructuredValueStatus();
        OnPropertyChanged(nameof(CanSubmit));
    }

    partial void OnSelectedValueTypeChanged(ValueTypeEnum value)
    {
        AttributeValue = string.Empty;
        SelectedBooleanValue = null;
        OnPropertyChanged(nameof(IsBooleanType));
        OnPropertyChanged(nameof(IsTextValueType));
        OnPropertyChanged(nameof(IsStringType));
        OnPropertyChanged(nameof(IsSingleLineTextType));
        UpdateStructuredValueStatus();
        OnPropertyChanged(nameof(CanSubmit));
    }

    partial void OnStructuredValueKindChanged(StructuredTextKind value) =>
        OnPropertyChanged(nameof(IsStructuredValueCandidate));

    private void UpdateStructuredValueStatus()
    {
        if (!IsStringType)
        {
            StructuredValueKind = StructuredTextKind.None;
            IsStructuredValueValid = false;
            StructuredValueStatus = string.Empty;
            return;
        }

        bool isValid = StructuredTextFormatter.TryFormat(
            AttributeValue, indented: false, out _, out StructuredTextKind kind, out string? error);

        StructuredValueKind = kind;
        IsStructuredValueValid = isValid;
        StructuredValueStatus = kind switch
        {
            StructuredTextKind.None => string.Empty,
            _ when isValid => $"Valid {kind.ToString().ToUpperInvariant()}",
            _ => $"Not valid {kind.ToString().ToUpperInvariant()} (will be saved as plain text): {error}"
        };
    }

    [RelayCommand]
    private void FormatValue()
    {
        if (StructuredTextFormatter.TryFormat(AttributeValue, indented: true, out string formatted, out _, out _))
        {
            AttributeValue = formatted;
        }
    }

    [RelayCommand]
    private void CompactValue()
    {
        if (StructuredTextFormatter.TryFormat(AttributeValue, indented: false, out string formatted, out _, out _))
        {
            AttributeValue = formatted;
        }
    }

    /// <summary>
    /// The value that should be written to the attribute. When editing a JSON/XML value that
    /// was stored on a single line, the (indented) edited value is compacted again, and if its
    /// content did not change the original value is returned unchanged.
    /// </summary>
    public string GetValueToStore()
    {
        if (!IsStringType ||
            _originalAttributeValue is null ||
            !StructuredTextFormatter.TryFormat(AttributeValue, indented: false, out string compactValue, out _, out _) ||
            !StructuredTextFormatter.TryFormat(_originalAttributeValue, indented: false, out string compactOriginal, out _, out _))
        {
            return AttributeValue;
        }

        if (string.Equals(compactValue, compactOriginal, StringComparison.Ordinal))
        {
            return _originalAttributeValue;
        }

        bool originalWasSingleLine = !_originalAttributeValue.Contains('\n', StringComparison.Ordinal);
        return originalWasSingleLine ? compactValue : AttributeValue;
    }

    public bool CanSubmit =>
        !HasErrors &&
        !string.IsNullOrWhiteSpace(AttributeSetName) &&
        !string.IsNullOrWhiteSpace(AttributeName) &&
        !string.IsNullOrWhiteSpace(AttributeValue);

    public bool ValidateAllInput()
    {
        ValidateAllProperties();
        OnPropertyChanged(nameof(CanSubmit));
        return !HasErrors;
    }

    public static ValidationResult? ValidateAttributeValue(
        object? value,
        ValidationContext context)
    {
        if (context.ObjectInstance is not AddAttributeDialogViewModel viewModel)
        {
            return new ValidationResult("Invalid validation context.");
        }

        string? text = value as string;

        if (string.IsNullOrWhiteSpace(text))
        {
            return new ValidationResult("Attribute value is required.");
        }

        switch (viewModel.SelectedValueType)
        {
            case ValueTypeEnum.kStringType:
                return ValidationResult.Success;

            case ValueTypeEnum.kIntegerType:
                return int.TryParse(text, out _)
                    ? ValidationResult.Success
                    : new ValidationResult("Value must be a valid integer.");

            case ValueTypeEnum.kDoubleType:
                return double.TryParse(
                    text,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out _)
                    ? ValidationResult.Success
                    : new ValidationResult("Value must be a valid double.");

            case ValueTypeEnum.kByteArrayType:
                string normalizedHex = text
                    .Replace(" ", string.Empty, StringComparison.Ordinal)
                    .Replace("-", string.Empty, StringComparison.Ordinal)
                    .Replace(":", string.Empty, StringComparison.Ordinal);
                if (normalizedHex.Length % 2 != 0)
                    return new ValidationResult("Hex string must have an even number of digits (e.g. FF 0A 1B).");
                foreach (char c in normalizedHex)
                {
                    if (!Uri.IsHexDigit(c))
                        return new ValidationResult("Value must be a valid hex string (e.g. FF 0A 1B). Only 0–9 and A–F are allowed.");
                }
                return ValidationResult.Success;
            case ValueTypeEnum.kBooleanType:
                return bool.TryParse(text, out _)
                    ? ValidationResult.Success
                    : new ValidationResult("Value must be either True or False.");
            default:
                return new ValidationResult("Unsupported attribute value type.");
        }
    }

    [RelayCommand]
    private void AddAttribute()
    {
        Globals.TelemetryService.TrackEvent("add_attribute_dialog_submitted",
            new System.Collections.Generic.Dictionary<string, object>
            {
                ["dialog_mode"] = DialogMode.ToString(),
                ["value_type"] = SelectedValueType.ToString()
            });

        if (!ValidateAllInput())
        {
            return;
        }

        if ((Globals.InvApp.ActiveDocument.SelectSet.Count != 1) & !IsEditMode)
        {
            DialogHelper.ShowInfoMessage(
                "Add Attribute",
                "Please select exactly one object before adding an attribute.");
            return;
        }

        Result = new AddAttributeDialogResult(
            AttributeSetName,
            AttributeName,
            SelectedValueType,
            GetValueToStore());

        Globals.TelemetryService.TrackEvent("add_attribute_dialog_succeeded",
            new System.Collections.Generic.Dictionary<string, object>
            {
                ["dialog_mode"] = DialogMode.ToString(),
                ["value_type"] = SelectedValueType.ToString()
            });
    }
}