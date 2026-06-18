using ExtrabbitCode.Attributes.Helper;
using ExtrabbitCode.Attributes.Models;
using ExtrabbitCode.Attributes.UI.Dialog;
using System.Collections.Generic;

namespace ExtrabbitCode.Attributes.Services;

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
            Globals.TelemetryService.TrackEvent("add_attribute_failed", new Dictionary<string, object>
            {
                ["document_type"] = document.DocumentType.ToString(),
                ["selected_object_type"] = selectedObject.GetType().Name,
                ["inventor_version"] = Globals.InvApp.SoftwareVersion.DisplayVersion
            });

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