using ExtrabbitCode.Inventor.Attributes.Helper;
using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.UI.Dialog;

namespace ExtrabbitCode.Inventor.Attributes.Services;

public sealed class AddAttributeWorkflowService(
    AttributeService attributeService) : IAddAttributeWorkflowService
{
    public AddAttributeWorkflowResult Execute()
    {
        (bool isValid, string? message) =
            DialogHelper.ValidateSingleSelectionForAddAttribute();

        if (!isValid)
        {
            return new AddAttributeWorkflowResult(
                false,
                null,
                null,
                message);
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
        object typedValue = AttributeValueConverter.ConvertToTypedValue(input.RawValue, input.ValueType);

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
}