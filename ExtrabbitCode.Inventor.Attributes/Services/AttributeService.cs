using ExtrabbitCode.Inventor.Attributes.Addin;
using ExtrabbitCode.Inventor.Attributes.Helper;
using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.Services.AttributeModels;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using ValueTypeEnum = Inventor.ValueTypeEnum;

namespace ExtrabbitCode.Inventor.Attributes.Services;

/// <summary>
/// Service for managing Inventor attributes on documents and persistent objects.
/// </summary>
public sealed class AttributeService : IAttributeService
{
    private static readonly ILog Logger = LogManagerAddin.GetLogger(
        typeof(AttributeService));

    public AttributeManager? GetAttributeManager(Document? document)
    {
        if (document == null)
        {
            Logger.Warn("Cannot get AttributeManager from null document.");
            return null;
        }

        try
        {
            return document.AttributeManager;
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Error getting AttributeManager: {ex.Message}",
                ex);
            return null;
        }
    }

    public AttributeDocumentInfo? GetAttributeTree(Document? document)
    {
        if (document is null)
        {
            Logger.Warn("Cannot get attribute tree: document is null.");
            return null;
        }

        try
        {
            ObjectCollection? objectsWithAttributes = GetObjectsWithAttributes(document);
            if (objectsWithAttributes == null)
            {
                return new AttributeDocumentInfo
                {
                    DocumentName = document.DisplayName
                };
            }

            AttributeDocumentInfo result = new()
            {
                DocumentName = document.DisplayName
            };

            foreach (object inventorObject in objectsWithAttributes)
            {
                InventorAttributeSets? attributeSets = TryGetAttributeSets(inventorObject);
                if (attributeSets == null || attributeSets.Count == 0)
                {
                    continue;
                }

                AttributeOwnerInfo ownerInfo = new()
                {
                    DisplayName = GetObjectDisplayName(inventorObject),
                    ObjectType = inventorObject.GetType().Name,
                    OwnerObject = inventorObject
                };

                foreach (InventorAttributeSet attributeSet in attributeSets)
                {
                    AttributeSetInfo attributeSetInfo = new()
                    {
                        Name = attributeSet.Name
                    };

                    foreach (InventorAttribute attribute in attributeSet)
                    {
                        attributeSetInfo.Attributes.Add(new AttributeInfo
                        {
                            Name = attribute.Name,
                            ValueType = attribute.ValueType,
                            Value = AttributeValueConverter.FormatAttributeValue(attribute.Value, attribute.ValueType)
                        });
                    }

                    ownerInfo.AttributeSets.Add(attributeSetInfo);
                }

                result.Owners.Add(ownerInfo);
            }

            foreach (OrphanedAttributeSetInfo orphan in GetOrphanedAttributeSets(document))
            {
                result.OrphanedAttributeSets.Add(orphan);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting attribute tree: {ex.Message}", ex);
            return null;
        }
    }

    public AttributeSetsEnumerator? FindAttributeSets(
        Document? document,
        string? attributeSetName = null)
    {
        if (document == null)
        {
            Logger.Warn("Cannot find attribute sets in null document.");
            return null;
        }

        try
        {
            AttributeManager? attributeManager = GetAttributeManager(document);
            if (attributeManager == null)
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(attributeSetName)
                ? attributeManager.FindAttributeSets()
                : attributeManager.FindAttributeSets(attributeSetName);
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Error finding attribute sets: {ex.Message}",
                ex);
            return null;
        }
    }

    public IEnumerable<InventorAttributeSet> GetAttributeSets(object? inventorObject)
    {
        if (inventorObject == null)
        {
            Logger.Warn("Cannot get attribute sets from null inventor object.");
            return [];
        }

        try
        {
            InventorAttributeSets? attributeSets = TryGetAttributeSets(inventorObject);
            if (attributeSets == null)
            {
                Logger.Warn("Inventor object does not expose AttributeSets.");
                return [];
            }

            List<InventorAttributeSet> result = [];
            foreach (InventorAttributeSet attributeSet in attributeSets)
            {
                result.Add(attributeSet);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting attribute sets from inventor object: {ex.Message}", ex);
            return [];
        }
    }

    public InventorAttributeSet? GetAttributeSet(
        Document? document,
        object? inventorObject,
        string attributeSetName)
    {
        if (document == null ||
            inventorObject == null ||
            string.IsNullOrWhiteSpace(attributeSetName))
        {
            Logger.Warn(
                "Cannot get attribute set: document, inventor object, or attribute set name is null.");
            return null;
        }

        try
        {
            foreach (InventorAttributeSet attributeSet in GetAttributeSets(inventorObject))
            {
                if (attributeSet.Name.Equals(
                    attributeSetName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return attributeSet;
                }
            }

            Logger.Debug(
                $"Attribute set '{attributeSetName}' not found on inventor object.");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Error getting attribute set '{attributeSetName}': {ex.Message}",
                ex);
            return null;
        }
    }

    public InventorAttributeSet? GetOrCreateAttributeSet(
        Document? document,
        object? inventorObject,
        string attributeSetName)
    {
        if (document == null ||
            inventorObject == null ||
            string.IsNullOrWhiteSpace(attributeSetName))
        {
            Logger.Warn(
                "Cannot get or create attribute set: document, inventor object, or attribute set name is null.");
            return null;
        }

        try
        {
            TransientObjects transientObjects = Globals.InvApp.TransientObjects;
            ObjectCollection objects = transientObjects.CreateObjectCollection();
            objects.Add(inventorObject);

            AttributeManager? attributeManager = GetAttributeManager(document);
            if (attributeManager == null)
            {
                return null;
            }

            AttributeSetsEnumerator openedSets =
                attributeManager.OpenAttributeSets(objects, attributeSetName);

            if (openedSets.Count < 1)
            {
                Logger.Warn(
                    $"OpenAttributeSets returned no results for '{attributeSetName}'.");
                return null;
            }

            return openedSets[1];
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Error getting or creating attribute set '{attributeSetName}': {ex.Message}",
                ex);
            return null;
        }
    }

    public bool DeleteAttributeSet(
        Document? document,
        object? inventorObject,
        string attributeSetName)
    {
        if (document == null ||
            inventorObject == null ||
            string.IsNullOrWhiteSpace(attributeSetName))
        {
            Logger.Warn(
                "Cannot delete attribute set: document, inventor object, or attribute set name is null.");
            return false;
        }

        try
        {
            InventorAttributeSet? attributeSet = GetAttributeSet(
                document,
                inventorObject,
                attributeSetName);

            if (attributeSet == null)
            {
                Logger.Debug(
                    $"Attribute set '{attributeSetName}' does not exist.");
                return false;
            }

            attributeSet.Delete();
            Logger.Info($"Deleted attribute set '{attributeSetName}'.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Error deleting attribute set '{attributeSetName}': {ex.Message}",
                ex);
            return false;
        }
    }

    public IEnumerable<InventorAttribute> GetAttributes(
        Document? document,
        object? inventorObject,
        string attributeSetName)
    {
        if (document == null ||
            inventorObject == null ||
            string.IsNullOrWhiteSpace(attributeSetName))
        {
            Logger.Warn(
                "Cannot get attributes: document, inventor object, or attribute set name is null.");
            return [];
        }

        try
        {
            InventorAttributeSet? attributeSet = GetAttributeSet(
                document,
                inventorObject,
                attributeSetName);

            if (attributeSet == null)
            {
                return [];
            }

            List<InventorAttribute> attributes = [];
            foreach (InventorAttribute attribute in attributeSet)
            {
                attributes.Add(attribute);
            }

            return attributes;
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Error getting attributes from '{attributeSetName}': {ex.Message}",
                ex);
            return [];
        }
    }

    public InventorAttribute? GetAttribute(
        Document? document,
        object? inventorObject,
        string attributeSetName,
        string attributeName)
    {
        if (document == null ||
            inventorObject == null ||
            string.IsNullOrWhiteSpace(attributeSetName) ||
            string.IsNullOrWhiteSpace(attributeName))
        {
            Logger.Warn(
                "Cannot get attribute: document, inventor object, attribute set name, or attribute name is null.");
            return null;
        }

        try
        {
            InventorAttributeSet? attributeSet = GetAttributeSet(
                document,
                inventorObject,
                attributeSetName);

            if (attributeSet == null)
            {
                Logger.Debug(
                    $"Attribute set '{attributeSetName}' was not found.");
                return null;
            }

            foreach (InventorAttribute attribute in attributeSet)
            {
                if (attribute.Name.Equals(
                    attributeName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return attribute;
                }
            }

            Logger.Debug(
                $"Attribute '{attributeName}' not found in set '{attributeSetName}'.");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Error getting attribute '{attributeName}': {ex.Message}",
                ex);
            return null;
        }
    }

    public InventorAttribute? AddOrUpdateAttribute(
        object? inventorObject,
        string attributeSetName,
        string attributeName,
        ValueTypeEnum valueType,
        object value)
    {
        if (inventorObject == null ||
            string.IsNullOrWhiteSpace(attributeSetName) ||
            string.IsNullOrWhiteSpace(attributeName))
        {
            Logger.Warn(
                "Cannot add or update attribute: document, inventor object, names, or value is null.");
            return null;
        }

        try
        {
            InventorAttributeSet? attributeSet = GetOrCreateAttributeSet(
                Globals.InvApp.ActiveDocument,
                inventorObject,
                attributeSetName);

            if (attributeSet == null)
            {
                Logger.Warn(
                    $"Could not get or create attribute set '{attributeSetName}'.");
                return null;
            }

            foreach (InventorAttribute existingAttribute in attributeSet)
            {
                if (!existingAttribute.Name.Equals(
                        attributeName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                existingAttribute.Value = value;

                Logger.Info(
                    $"Updated attribute '{attributeName}' in set '{attributeSetName}'.");
                return existingAttribute;
            }

            InventorAttribute newAttribute = attributeSet.Add(
                attributeName,
                valueType,
                value);

            Logger.Info(
                $"Added attribute '{attributeName}' to set '{attributeSetName}'.");
            return newAttribute;
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Error adding or updating attribute '{attributeName}': {ex.Message}",
                ex);
            return null;
        }
    }

    public bool UpdateAttributeValue(InventorAttribute? attribute, object? value)
    {
        if (attribute == null || value == null)
        {
            Logger.Warn("Cannot update attribute value: attribute or value is null.");
            return false;
        }

        try
        {
            attribute.Value = value;
            Logger.Info($"Updated attribute '{attribute.Name}' value.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Error updating attribute '{attribute.Name}': {ex.Message}",
                ex);
            return false;
        }
    }

    public bool DeleteAttribute(
        Document? document,
        object? inventorObject,
        string attributeSetName,
        string attributeName)
    {
        if (document == null ||
            inventorObject == null ||
            string.IsNullOrWhiteSpace(attributeSetName) ||
            string.IsNullOrWhiteSpace(attributeName))
        {
            Logger.Warn(
                "Cannot delete attribute: document, inventor object, attribute set name, or attribute name is null.");
            return false;
        }

        try
        {
            InventorAttribute? attribute = GetAttribute(
                document,
                inventorObject,
                attributeSetName,
                attributeName);

            if (attribute == null)
            {
                Logger.Debug(
                    $"Attribute '{attributeName}' not found in set '{attributeSetName}'.");
                return false;
            }

            attribute.Delete();
            Logger.Info(
                $"Deleted attribute '{attributeName}' from set '{attributeSetName}'.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Error deleting attribute '{attributeName}': {ex.Message}",
                ex);
            return false;
        }
    }

    public DeleteAttributesResult DeleteAllAttributes(ObjectCollection? inventorObjectCollection, bool deleteAutodeskDefaultAttributeSets)
    {
        if (inventorObjectCollection == null || inventorObjectCollection.Count == 0)
        {
            Logger.Warn(
                "Cannot delete all attributes: inventor object collection is null or empty.");
            return new DeleteAttributesResult(0, 0, 0, 0);
        }

        int affectedObjects = 0;
        int deletedAttributes = 0;
        int deletedAttributeSets = 0;
        int failedObjects = 0;

        try
        {
            foreach (object inventorObject in inventorObjectCollection)
            {
                try
                {
                    InventorAttributeSets? attributeSets =
                        TryGetAttributeSets(inventorObject);

                    if (attributeSets == null || attributeSets.Count == 0)
                    {
                        continue;
                    }

                    affectedObjects++;

                    List<InventorAttributeSet> attributeSetsToDelete = [];
                    attributeSetsToDelete.AddRange(attributeSets.Cast<InventorAttributeSet>());

                    foreach (InventorAttributeSet attributeSet in attributeSetsToDelete)
                    {
                        if (!deleteAutodeskDefaultAttributeSets &&
                            IsAutodeskDefaultAttributeSet(attributeSet.Name))
                        {
                            continue;
                        }

                        List<InventorAttribute> attributesToDelete = [];
                        attributesToDelete.AddRange(attributeSet.Cast<InventorAttribute>());

                        foreach (InventorAttribute attribute in attributesToDelete)
                        {
                            attribute.Delete();
                            deletedAttributes++;
                        }

                        attributeSet.Delete();
                        deletedAttributeSets++;
                    }
                }
                catch (Exception ex)
                {
                    failedObjects++;

                    Logger.Error(
                        $"Error deleting attributes from selected object: {ex.Message}",
                        ex);
                }
            }

            Logger.Info(
                $"Deleted {deletedAttributes} attribute(s) and {deletedAttributeSets} attribute set(s) from {affectedObjects} object(s). Failed objects: {failedObjects}.");

            return new DeleteAttributesResult(
                AffectedObjects: affectedObjects,
                DeletedAttributes: deletedAttributes,
                DeletedAttributeSets: deletedAttributeSets,
                FailedObjects: failedObjects);
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Error deleting all attributes from object collection: {ex.Message}",
                ex);

            return new DeleteAttributesResult(
                AffectedObjects: affectedObjects,
                DeletedAttributes: deletedAttributes,
                DeletedAttributeSets: deletedAttributeSets,
                FailedObjects: failedObjects);
        }
    }

    public ObjectCollection? GetObjectsWithAttributes(Document? document)
    {
        try
        {
            AttributeManager? attributeManager = GetAttributeManager(Globals.InvApp.ActiveDocument);
            ObjectCollection? objects = attributeManager?.FindObjects();

            ObjectCollection objectCollection =
                Globals.InvApp.TransientObjects.CreateObjectCollection();

            if (objects == null)
            {
                return objectCollection;
            }

            foreach (object inventorObject in objects)
            {
                objectCollection.Add(inventorObject);
            }

            return objectCollection;
        }
        catch (Exception ex)
        {
            Logger.Error(
                $"Error getting objects with attributes: {ex.Message}",
                ex);
            return null;
        }
    }

    public IReadOnlyList<OrphanedAttributeSetInfo> GetOrphanedAttributeSets(Document? document)
    {
        List<OrphanedAttributeSetInfo> result = [];

        AttributeManager? attributeManager = GetAttributeManager(document);
        if (attributeManager == null)
        {
            return result;
        }

        try
        {
            // reportOnly = true -> does not actually purge
            attributeManager.PurgeAttributeSets("*", true, out object orphans);

            if (orphans is null)
            {
                return result;
            }

            dynamic orphanCollection = orphans;
            int count = orphanCollection.Count;

            for (int i = 1; i <= count; i++)
            {
                InventorAttributeSet attributeSet = orphanCollection[i];

                OrphanedAttributeSetInfo info = new() { Name = attributeSet.Name };

                foreach (InventorAttribute attribute in attributeSet)
                {
                    info.Attributes.Add(new AttributeInfo
                    {
                        Name = attribute.Name,
                        ValueType = attribute.ValueType,
                        Value = AttributeValueConverter.FormatAttributeValue(
                            attribute.Value, attribute.ValueType)
                    });
                }

                result.Add(info);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting orphaned attribute sets: {ex.Message}", ex);
        }

        return result;
    }

    public bool PurgeOrphanedAttributeSet(Document? document, string attributeSetName)
    {
        AttributeManager? attributeManager = GetAttributeManager(document);
        if (attributeManager == null || string.IsNullOrWhiteSpace(attributeSetName))
        {
            return false;
        }

        try
        {
            attributeManager.PurgeAttributeSets(attributeSetName, false, out _);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error purging orphaned attribute set '{attributeSetName}': {ex.Message}", ex);
            return false;
        }
    }

    public bool AttributeSetExists(
        Document? document,
        object? inventorObject,
        string attributeSetName)
    {
        if (document == null ||
            inventorObject == null ||
            string.IsNullOrWhiteSpace(attributeSetName))
        {
            return false;
        }

        return GetAttributeSet(document, inventorObject, attributeSetName) != null;
    }

    public bool AttributeExists(
        Document? document,
        object? inventorObject,
        string attributeSetName,
        string attributeName)
    {
        if (document == null ||
            inventorObject == null ||
            string.IsNullOrWhiteSpace(attributeSetName) ||
            string.IsNullOrWhiteSpace(attributeName))
        {
            return false;
        }

        return GetAttribute(
            document,
            inventorObject,
            attributeSetName,
            attributeName) != null;
    }

    private static InventorAttributeSets? TryGetAttributeSets(object inventorObject)
    {
        try
        {
            dynamic dynObject = inventorObject;
            return dynObject.AttributeSets as InventorAttributeSets;
        }
        catch
        {
            return null;
        }
    }

    private static string GetObjectDisplayName(object inventorObject)
    {
        try
        {
            if (inventorObject is Document document)
            {
                return document.DisplayName;
            }

            ObjectTypeEnum objectType = GetInventorObjectType(inventorObject);
            return objectType.ToString();
        }
        catch
        {
            return "Unknown Inventor Object";
        }
    }

    private static ObjectTypeEnum GetInventorObjectType(object inventorObject)
    {
        dynamic dynObject = inventorObject;
        return (ObjectTypeEnum)dynObject.Type;
    }

    private static bool IsAutodeskDefaultAttributeSet(string attributeSetName)
    {
        if (string.IsNullOrWhiteSpace(attributeSetName))
        {
            return false;
        }

        // iLogic sets
        if (attributeSetName.Equals("iLogicRuleListSet", StringComparison.OrdinalIgnoreCase) ||
            attributeSetName.Equals("iLogicDocumentRuleOptions", StringComparison.OrdinalIgnoreCase) ||
            attributeSetName.Equals("iLogicDocumentLanguageAttSet", StringComparison.OrdinalIgnoreCase) ||
            attributeSetName.StartsWith("iLogicRule_", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Inventor Publications / Shared Views (PUBx prefix)
        if (attributeSetName.StartsWith("PUBx", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Other known internal Autodesk attribute sets
        if (attributeSetName.Equals("_ADSK_PartDataValues", StringComparison.OrdinalIgnoreCase) ||
            attributeSetName.Equals("_ADSK_AssemblyDataValues", StringComparison.OrdinalIgnoreCase) ||
            attributeSetName.StartsWith("_ADSK_", StringComparison.OrdinalIgnoreCase) ||
            attributeSetName.StartsWith("Adsk_", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}