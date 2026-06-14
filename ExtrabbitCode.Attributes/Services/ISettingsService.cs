using ExtrabbitCode.Attributes.Models;

namespace ExtrabbitCode.Attributes.Services;

public interface ISettingsService
{
    SettingsModel GetCopy();

    void Update(SettingsModel settings);
}