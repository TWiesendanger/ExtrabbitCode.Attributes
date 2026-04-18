using System.Collections.Generic;

namespace ExtrabbitCode.Inventor.Attributes.Services.AttributeModels;

public sealed class OrphanedAttributeSetInfo
{
    public required string Name { get; init; }
    public List<AttributeInfo> Attributes { get; } = [];
}