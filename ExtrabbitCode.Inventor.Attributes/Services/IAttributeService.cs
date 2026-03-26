using System.Collections.Generic;

namespace ExtrabbitCode.Inventor.Attributes.Services;

/// <summary>
/// Service for managing Inventor attributes.
/// </summary>
public interface IAttributeService
{
    /// <summary>
    /// Gets all attribute sets from the specified document.
    /// </summary>
    /// <param name="document">The Inventor document.</param>
    /// <returns>A collection of attribute sets.</returns>
    AttributeSetsEnumerator? GetAttributeSets(Document? document);

    /// <summary>
    /// Gets a specific attribute set by name from the specified document.
    /// </summary>
    /// <param name="document">The Inventor document.</param>
    /// <param name="attributeSetName">The name of the attribute set.</param>
    /// <returns>The attribute set if found, otherwise null.</returns>
    InventorAttributeSet? GetAttributeSet(Document? document, string attributeSetName);

    /// <summary>
    /// Creates a new attribute set in the specified document.
    /// </summary>
    /// <param name="document">The Inventor document.</param>
    /// <param name="attributeSetName">The name of the attribute set to create.</param>
    /// <returns>The created attribute set.</returns>
    InventorAttributeSet CreateAttributeSet(Document? document, string attributeSetName);

    /// <summary>
    /// Deletes an attribute set from the specified document.
    /// </summary>
    /// <param name="document">The Inventor document.</param>
    /// <param name="attributeSetName">The name of the attribute set to delete.</param>
    /// <returns>True if the attribute set was deleted, otherwise false.</returns>
    bool DeleteAttributeSet(Document? document, string attributeSetName);

    /// <summary>
    /// Gets all attributes from a specific attribute set.
    /// </summary>
    /// <param name="attributeSet">The attribute set.</param>
    /// <returns>A collection of attributes.</returns>
    IEnumerable<InventorAttribute> GetAttributes(InventorAttributeSet? attributeSet);

    /// <summary>
    /// Gets a specific attribute by name from the specified attribute set.
    /// </summary>
    /// <param name="attributeSet">The attribute set.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>The attribute if found, otherwise null.</returns>
    InventorAttribute? GetAttribute(InventorAttributeSet? attributeSet, string attributeName);

    /// <summary>
    /// Adds a new attribute to the specified attribute set.
    /// </summary>
    /// <param name="attributeSet">The attribute set.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value of the attribute.</param>
    /// <returns>The created attribute.</returns>
    InventorAttribute? AddAttribute(InventorAttributeSet? attributeSet, string attributeName, object? value);

    /// <summary>
    /// Updates the value of an existing attribute.
    /// </summary>
    /// <param name="attribute">The attribute to update.</param>
    /// <param name="value">The new value.</param>
    /// <returns>True if the attribute was updated, otherwise false.</returns>
    bool UpdateAttributeValue(InventorAttribute? attribute, object value);

    /// <summary>
    /// Deletes an attribute from its attribute set.
    /// </summary>
    /// <param name="attribute">The attribute to delete.</param>
    /// <returns>True if the attribute was deleted, otherwise false.</returns>
    bool DeleteAttribute(InventorAttribute? attribute);

    /// <summary>
    /// Deletes all attributes from the specified attribute set.
    /// </summary>
    /// <param name="attributeSet">The attribute set.</param>
    /// <returns>The number of attributes deleted.</returns>
    int DeleteAllAttributes(InventorAttributeSet? attributeSet);

    /// <summary>
    /// Checks if an attribute set exists in the specified document.
    /// </summary>
    /// <param name="document">The Inventor document.</param>
    /// <param name="attributeSetName">The name of the attribute set.</param>
    /// <returns>True if the attribute set exists, otherwise false.</returns>
    bool AttributeSetExists(Document? document, string attributeSetName);

    /// <summary>
    /// Checks if an attribute exists in the specified attribute set.
    /// </summary>
    /// <param name="attributeSet">The attribute set.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>True if the attribute exists, otherwise false.</returns>
    bool AttributeExists(InventorAttributeSet? attributeSet, string attributeName);
}