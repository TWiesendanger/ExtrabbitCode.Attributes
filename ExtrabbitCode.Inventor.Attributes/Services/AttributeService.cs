using ExtrabbitCode.Inventor.Attributes.Helper;
using log4net;
using System;
using System.Collections.Generic;


namespace ExtrabbitCode.Inventor.Attributes.Services;

/// <summary>
/// Service for managing Inventor attributes.
/// </summary>
public sealed class AttributeService : IAttributeService
{
    private static readonly ILog Logger = LogManagerAddin.GetLogger(typeof(AttributeService));

    public AttributeSetsEnumerator? GetAttributeSets(Document? document)
    {
        if (document == null)
        {
            Logger.Warn("Cannot get attribute sets from null document.");
            return null;
        }

        try
        {
            AttributeManager? attributeManager = document.AttributeManager;
            AttributeSetsEnumerator? attributeSets = attributeManager.FindAttributeSets();
            return attributeSets;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting attribute sets from document: {ex.Message}", ex);
            return null;
        }
    }

    public InventorAttributeSet? GetAttributeSet(Document? document, string attributeSetName)
    {
        if (document == null || string.IsNullOrWhiteSpace(attributeSetName))
        {
            Logger.Warn("Cannot get attribute set: document or attribute set name is null.");
            return null;
        }

        try
        {
            InventorAttributeSets? inventorAttributeSets = document.AttributeSets;
            foreach (InventorAttributeSet inventorAttributeSet in inventorAttributeSets)
            {
                if (inventorAttributeSet.Name.Equals(attributeSetName, StringComparison.OrdinalIgnoreCase))
                {
                    return inventorAttributeSet;
                }
            }

            Logger.Debug($"Attribute set '{attributeSetName}' not found in document.");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting attribute set '{attributeSetName}': {ex.Message}", ex);
            return null;
        }
    }

    public InventorAttributeSet CreateAttributeSet(Document? document, string attributeSetName)
    {
        throw new NotImplementedException();
    }

    public bool DeleteAttributeSet(Document? document, string attributeSetName)
    {
        if (document == null || string.IsNullOrWhiteSpace(attributeSetName))
        {
            Logger.Warn("Cannot delete attribute set: document or attribute set name is null.");
            return false;
        }

        try
        {
            InventorAttributeSet? attributeSet = GetAttributeSet(document, attributeSetName);
            if (attributeSet == null)
            {
                Logger.Debug($"Attribute set '{attributeSetName}' does not exist.");
                return false;
            }

            attributeSet.Delete();
            Logger.Info($"Deleted attribute set '{attributeSetName}'.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error deleting attribute set '{attributeSetName}': {ex.Message}", ex);
            return false;
        }
    }

    public IEnumerable<InventorAttribute> GetAttributes(InventorAttributeSet? attributeSet)
    {
        if (attributeSet == null)
        {
            Logger.Warn("Cannot get attributes from null attribute set.");
            return [];
        }

        try
        {
            List<InventorAttribute> attributes = [];
            foreach (InventorAttribute attribute in attributeSet)
            {
                attributes.Add(attribute);
            }
            return attributes;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting attributes from attribute set '{attributeSet.Name}': {ex.Message}", ex);
            return [];
        }
    }

    public InventorAttribute? GetAttribute(InventorAttributeSet? attributeSet, string attributeName)
    {
        if (attributeSet == null || string.IsNullOrWhiteSpace(attributeName))
        {
            Logger.Warn("Cannot get attribute: attribute set or attribute name is null.");
            return null;
        }

        try
        {
            foreach (InventorAttribute attribute in attributeSet)
            {
                if (attribute.Name.Equals(attributeName, StringComparison.OrdinalIgnoreCase))
                {
                    return attribute;
                }
            }

            Logger.Debug($"Attribute '{attributeName}' not found in attribute set '{attributeSet.Name}'.");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting attribute '{attributeName}': {ex.Message}", ex);
            return null;
        }
    }

    public InventorAttribute? AddAttribute(InventorAttributeSet? attributeSet, string attributeName, object? value)
    {
        if (attributeSet == null || string.IsNullOrWhiteSpace(attributeName) || value == null)
        {
            Logger.Warn("Cannot add attribute: attribute set, attribute name, or value is null.");
            return null;
        }

        try
        {
            InventorAttribute? existingAttribute = GetAttribute(attributeSet, attributeName);
            if (existingAttribute != null)
            {
                Logger.Info($"Attribute '{attributeName}' already exists in attribute set '{attributeSet.Name}'.");
                return existingAttribute;
            }

            InventorAttribute? attribute = attributeSet.Add(attributeName, ValueTypeEnum.kStringType, value);
            Logger.Info($"Added attribute '{attributeName}' to attribute set '{attributeSet.Name}'.");
            return attribute;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error adding attribute '{attributeName}': {ex.Message}", ex);
            return null;
        }
    }

    public bool UpdateAttributeValue(InventorAttribute? attribute, object? value)
    {
        if (attribute == null || value == null)
        {
            Logger.Warn("Cannot update attribute: attribute or value is null.");
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
            Logger.Error($"Error updating attribute '{attribute.Name}': {ex.Message}", ex);
            return false;
        }
    }

    public bool DeleteAttribute(InventorAttribute? attribute)
    {
        if (attribute == null)
        {
            Logger.Warn("Cannot delete null attribute.");
            return false;
        }

        try
        {
            string? attributeName = attribute.Name;
            attribute.Delete();
            Logger.Info($"Deleted attribute '{attributeName}'.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error deleting attribute: {ex.Message}", ex);
            return false;
        }
    }

    public int DeleteAllAttributes(InventorAttributeSet? attributeSet)
    {
        if (attributeSet == null)
        {
            Logger.Warn("Cannot delete attributes from null attribute set.");
            return 0;
        }

        try
        {
            List<InventorAttribute> attributes = [.. GetAttributes(attributeSet)];
            int count = 0;

            foreach (InventorAttribute attribute in attributes)
            {
                if (DeleteAttribute(attribute))
                {
                    count++;
                }
            }

            Logger.Info($"Deleted {count} attributes from attribute set '{attributeSet.Name}'.");
            return count;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error deleting all attributes from attribute set '{attributeSet.Name}': {ex.Message}", ex);
            return 0;
        }
    }

    public bool AttributeSetExists(Document? document, string attributeSetName)
    {
        if (document == null || string.IsNullOrWhiteSpace(attributeSetName))
        {
            return false;
        }

        return GetAttributeSet(document, attributeSetName) != null;
    }

    public bool AttributeExists(InventorAttributeSet? attributeSet, string attributeName)
    {
        if (attributeSet == null || string.IsNullOrWhiteSpace(attributeName))
        {
            return false;
        }

        return GetAttribute(attributeSet, attributeName) != null;
    }
}