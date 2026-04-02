namespace ExtrabbitCode.Inventor.Attributes.Services.AttributeModels;

public sealed class AttributeInfo
{
    public string Name { get; init; } = string.Empty;

    public ValueTypeEnum ValueType { get; init; } = ValueTypeEnum.kStringType;

    public string Value { get; init; } = string.Empty;
}