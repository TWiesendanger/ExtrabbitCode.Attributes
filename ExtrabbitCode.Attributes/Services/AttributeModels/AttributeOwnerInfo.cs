using System.Collections.Generic;

namespace ExtrabbitCode.Attributes.Services.AttributeModels;

public sealed class AttributeOwnerInfo
{
    public string DisplayName { get; init; } = string.Empty;

    public string ObjectType { get; init; } = string.Empty;
    public object? OwnerObject { get; init; }

    public List<AttributeSetInfo> AttributeSets { get; init; } = [];
}