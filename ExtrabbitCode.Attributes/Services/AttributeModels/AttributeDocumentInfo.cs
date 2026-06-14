using System.Collections.Generic;

namespace ExtrabbitCode.Attributes.Services.AttributeModels;

public sealed class AttributeDocumentInfo
{
    public string DocumentName { get; init; } = string.Empty;

    public List<AttributeOwnerInfo> Owners { get; init; } = [];

    public List<OrphanedAttributeSetInfo> OrphanedAttributeSets { get; } = [];
}