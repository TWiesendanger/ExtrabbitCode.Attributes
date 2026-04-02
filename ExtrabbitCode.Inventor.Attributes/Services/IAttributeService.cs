using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.Services.AttributeModels;
using System.Collections.Generic;
using ValueTypeEnum = Inventor.ValueTypeEnum;

namespace ExtrabbitCode.Inventor.Attributes.Services;

public interface IAttributeService
{
    AttributeManager? GetAttributeManager(Document? document);

    AttributeDocumentInfo? GetAttributeTree(Document? document);

    AttributeSetsEnumerator? FindAttributeSets(
        Document? document,
        string? attributeSetName = null);

    IEnumerable<InventorAttributeSet> GetAttributeSets(object? inventorObject);

    InventorAttributeSet? GetAttributeSet(
        Document? document,
        object? inventorObject,
        string attributeSetName);

    InventorAttributeSet? GetOrCreateAttributeSet(
        Document? document,
        object? inventorObject,
        string attributeSetName);

    bool DeleteAttributeSet(
        Document? document,
        object? inventorObject,
        string attributeSetName);

    IEnumerable<InventorAttribute> GetAttributes(
        Document? document,
        object? inventorObject,
        string attributeSetName);

    InventorAttribute? GetAttribute(
        Document? document,
        object? inventorObject,
        string attributeSetName,
        string attributeName);

    InventorAttribute? AddOrUpdateAttribute(object? inventorObject,
        string attributeSetName,
        string attributeName,
        ValueTypeEnum valueType,
        object value);

    bool UpdateAttributeValue(InventorAttribute? attribute, object? value);

    bool DeleteAttribute(
        Document? document,
        object? inventorObject,
        string attributeSetName,
        string attributeName);

    DeleteAttributesResult DeleteAllAttributes(ObjectCollection? inventorObjectCollection);

    bool AttributeSetExists(
        Document? document,
        object? inventorObject,
        string attributeSetName);

    bool AttributeExists(
        Document? document,
        object? inventorObject,
        string attributeSetName,
        string attributeName);

    ObjectCollection? GetObjectsWithAttributes(Document? document);
}