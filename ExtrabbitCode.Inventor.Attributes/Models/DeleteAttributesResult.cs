namespace ExtrabbitCode.Inventor.Attributes.Models;

public sealed record DeleteAttributesResult(
    int AffectedObjects,
    int DeletedAttributes,
    int DeletedAttributeSets,
    int FailedObjects)
{
    public bool IsSuccess => FailedObjects == 0;

    public bool HasChanges => DeletedAttributes > 0 || DeletedAttributeSets > 0;
}