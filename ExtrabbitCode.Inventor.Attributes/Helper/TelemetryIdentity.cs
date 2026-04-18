using System;
using System.IO;

namespace ExtrabbitCode.Inventor.Attributes.Helper;

public static class TelemetryIdentity
{
    public static string GetOrCreate(string storageFolder)
    {
        string file = System.IO.Path.Combine(storageFolder, "telemetry-id.txt");
        if (System.IO.File.Exists(file))
        {
            return System.IO.File.ReadAllText(file).Trim();
        }

        string id = Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(storageFolder);
        System.IO.File.WriteAllText(file, id);
        return id;
    }
}