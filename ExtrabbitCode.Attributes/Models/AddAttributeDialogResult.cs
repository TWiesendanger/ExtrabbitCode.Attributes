namespace ExtrabbitCode.Attributes.Models;

public sealed record AddAttributeDialogResult(
    string AttributeSetName,
    string AttributeName,
    ValueTypeEnum ValueType,
    string RawValue);