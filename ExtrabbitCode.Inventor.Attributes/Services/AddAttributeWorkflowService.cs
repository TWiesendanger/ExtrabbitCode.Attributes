using ExtrabbitCode.Inventor.Attributes.Helper;
using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.UI.Dialog;
using System;
using System.Globalization;

namespace ExtrabbitCode.Inventor.Attributes.Services;

public sealed class AddAttributeWorkflowService(
    AttributeService attributeService) : IAddAttributeWorkflowService
{
    public AddAttributeWorkflowResult Execute()
    {
        (bool IsValid, string? Message) validation =
            DialogHelper.ValidateSingleSelectionForAddAttribute();

        if (!validation.IsValid)
        {
            return new AddAttributeWorkflowResult(
                false,
                null,
                null,
                validation.Message);
        }

        Document document = Globals.InvApp.ActiveDocument;
        object selectedObject = document.SelectSet[1];

        AddAttributeDialog dialog = new();
        DialogHelper.SetDialogTheme(dialog);

        bool? dialogResult = dialog.ShowDialog();
        if (dialogResult != true)
        {
            return new AddAttributeWorkflowResult(false, null, null);
        }

        AddAttributeDialogResult input = dialog.Result;
        object typedValue = ConvertToTypedValue(input.RawValue, input.ValueType);

        InventorAttribute? attribute = attributeService.AddOrUpdateAttribute(
            selectedObject,
            input.AttributeSetName,
            input.AttributeName,
            input.ValueType,
            typedValue);

        if (attribute == null)
        {
            return new AddAttributeWorkflowResult(
                false,
                selectedObject,
                input,
                "The attribute could not be created.");
        }

        return new AddAttributeWorkflowResult(
            true,
            selectedObject,
            input);
    }

    private static object ConvertToTypedValue(
        string rawValue,
        ValueTypeEnum valueType)
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
}