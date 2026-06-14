using ExtrabbitCode.Attributes.Models;
using System;
using System.IO;
using System.Text.Json;
using File = System.IO.File;
using Path = System.IO.Path;

namespace ExtrabbitCode.Attributes.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private SettingsModel _settings;

    public SettingsService(string filePath)
    {
        _filePath = filePath;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        _settings = Load();
    }

    public SettingsModel GetCopy()
    {
        return new SettingsModel
        {
            ShowConfirmationMessages = _settings.ShowConfirmationMessages,
            ShowWarningOnSingleAttributeDelete = _settings.ShowWarningOnSingleAttributeDelete,
            ShowWarningOnDeleteAllAttributes = _settings.ShowWarningOnDeleteAllAttributes,
            UpdateAttributesOnDocumentSwitch = _settings.UpdateAttributesOnDocumentSwitch,
            DeleteAutodeskDefaultAttributeSets = _settings.DeleteAutodeskDefaultAttributeSets,
            TelemetryEnabled = _settings.TelemetryEnabled,
            TelemetryConsentAsked = _settings.TelemetryConsentAsked,
            TelemetryId = _settings.TelemetryId
        };
    }

    public void Update(SettingsModel settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = new SettingsModel
        {
            ShowConfirmationMessages = settings.ShowConfirmationMessages,
            ShowWarningOnSingleAttributeDelete =
                settings.ShowWarningOnSingleAttributeDelete,
            ShowWarningOnDeleteAllAttributes =
                settings.ShowWarningOnDeleteAllAttributes,
            UpdateAttributesOnDocumentSwitch =
                settings.UpdateAttributesOnDocumentSwitch,
            DeleteAutodeskDefaultAttributeSets =
                settings.DeleteAutodeskDefaultAttributeSets,
            TelemetryEnabled = settings.TelemetryEnabled,
            TelemetryConsentAsked = settings.TelemetryConsentAsked,
            TelemetryId = settings.TelemetryId
        };

        Save();
    }

    public void Save()
    {
        EnsureStorageExists();

        string json = JsonSerializer.Serialize(
            _settings,
            _jsonSerializerOptions);

        File.WriteAllText(_filePath, json);
    }

    private SettingsModel Load()
    {
        EnsureStorageExists();

        try
        {
            string json = File.ReadAllText(_filePath);
            SettingsModel settings = JsonSerializer.Deserialize<SettingsModel>(json) ?? new SettingsModel();

            string canonical = JsonSerializer.Serialize(settings, _jsonSerializerOptions);
            if (canonical != json)
            {
                File.WriteAllText(_filePath, canonical);
            }

            return settings;
        }
        catch
        {
            SettingsModel defaultSettings = new();
            string json = JsonSerializer.Serialize(defaultSettings, _jsonSerializerOptions);
            File.WriteAllText(_filePath, json);
            return defaultSettings;
        }
    }

    private void EnsureStorageExists()
    {
        string? directoryPath = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrWhiteSpace(directoryPath) &&
            !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        if (File.Exists(_filePath))
        {
            return;
        }

        SettingsModel defaultSettings = new();
        string json = JsonSerializer.Serialize(
            defaultSettings,
            _jsonSerializerOptions);

        File.WriteAllText(_filePath, json);
    }
}