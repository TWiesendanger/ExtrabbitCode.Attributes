using ExtrabbitCode.Attributes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using File = System.IO.File;
using Path = System.IO.Path;

namespace ExtrabbitCode.Attributes.Services;

public sealed class AttributeLibraryService : IAttributeLibraryService
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly AttributeLibrary _library;

    public AttributeLibraryService(string filePath)
    {
        _filePath = filePath;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        _library = Load();
    }

    public IReadOnlyList<string> GetAttributeSetNames()
    {
        return [.. _library.AttributeSetNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)];
    }

    public void AddAttributeSetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        string trimmedName = name.Trim();

        if (_library.AttributeSetNames.Any(x =>
                x.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _library.AttributeSetNames.Add(trimmedName);
        Save();
    }

    public bool RemoveAttributeSetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        string? existingName = _library.AttributeSetNames.FirstOrDefault(x =>
            x.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

        if (existingName == null)
        {
            return false;
        }

        bool removed = _library.AttributeSetNames.Remove(existingName);
        if (removed)
        {
            Save();
        }

        return removed;
    }

    public void Save()
    {
        string? directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(
            _library,
            _jsonSerializerOptions);

        File.WriteAllText(_filePath, json);
    }

    private AttributeLibrary Load()
    {
        if (!File.Exists(_filePath))
        {
            return new AttributeLibrary();
        }

        string json = File.ReadAllText(_filePath);

        return JsonSerializer.Deserialize<AttributeLibrary>(json) ??
               new AttributeLibrary();
    }
}