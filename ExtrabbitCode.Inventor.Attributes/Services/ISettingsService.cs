using ExtrabbitCode.Inventor.Attributes.Models;

namespace ExtrabbitCode.Inventor.Attributes.Services;

public interface ISettingsService
{
    SettingsModel GetCopy();

    void Update(SettingsModel settings);
}