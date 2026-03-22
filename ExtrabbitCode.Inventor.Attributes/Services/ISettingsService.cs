using ExtrabbitCode.Inventor.Attributes.Models;

namespace ExtrabbitCode.Inventor.Attributes.Services;

public interface ISettingsService
{
    SettingsModel Current { get; }

    SettingsModel GetCopy();

    void Update(SettingsModel settings);
}