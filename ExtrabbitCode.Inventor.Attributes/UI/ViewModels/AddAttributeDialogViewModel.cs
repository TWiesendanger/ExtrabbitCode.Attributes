using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExtrabbitCode.Inventor.Attributes.Helper;
using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

// ReSharper disable InconsistentNaming

namespace ExtrabbitCode.Inventor.Attributes.UI.ViewModels;

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
        AttributeValue = attributeValueInitial;

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
        OnPropertyChanged(nameof(CanSubmit));
    }

    partial void OnSelectedValueTypeChanged(ValueTypeEnum value)
    {
        AttributeValue = string.Empty;
        SelectedBooleanValue = null;
        OnPropertyChanged(nameof(IsBooleanType));
        OnPropertyChanged(nameof(IsTextValueType));
        OnPropertyChanged(nameof(CanSubmit));
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
                try
                {
                    _ = Convert.FromBase64String(text);
                    return ValidationResult.Success;
                }
                catch
                {
                    return new ValidationResult(
                        "Value must be a valid Base64 string.");
                }
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
            AttributeValue);

        Globals.TelemetryService.TrackEvent("add_attribute_dialog_succeeded",
            new System.Collections.Generic.Dictionary<string, object>
            {
                ["dialog_mode"] = DialogMode.ToString(),
                ["value_type"] = SelectedValueType.ToString()
            });
    }
}