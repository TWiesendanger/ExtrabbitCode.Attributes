using System.Collections.Generic;

namespace ExtrabbitCode.Inventor.Attributes.Services;

public interface IAttributeLibraryService
{
    IReadOnlyList<string> GetAttributeSetNames();
    void AddAttributeSetName(string name);
    bool RemoveAttributeSetName(string name);
    void Save();
}