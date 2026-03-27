namespace ExtrabbitCode.Inventor.Attributes.Helper;

public static class StoragePaths
{
    public static string AppDirectory =>
        System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "ExtrabbitCode.Inventor.Attributes");

    public static string SettingsFile =>
        System.IO.Path.Combine(AppDirectory, "settings.json");

    public static string AttributeLibraryFile =>
        System.IO.Path.Combine(AppDirectory, "attribute-library.json");
}