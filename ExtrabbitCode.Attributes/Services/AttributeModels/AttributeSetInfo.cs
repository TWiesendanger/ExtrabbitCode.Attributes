using System.Collections.Generic;

namespace ExtrabbitCode.Attributes.Services.AttributeModels;

public sealed class AttributeSetInfo
{
    public string Name { get; init; } = string.Empty;

    public List<AttributeInfo> Attributes { get; init; } = [];
}