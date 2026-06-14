using ExtrabbitCode.Attributes.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ExtrabbitCode.Attributes.Helper;

public static class ThemeResourceHelper
{
    private const string SharedThemePath =
        "pack://application:,,,/ExtrabbitCode.Attributes;component/Resources/Styles/Theme.Shared.xaml";

    private const string DarkThemePath =
        "pack://application:,,,/ExtrabbitCode.Attributes;component/Resources/Styles/Theme.Dark.xaml";

    private const string LightThemePath =
        "pack://application:,,,/ExtrabbitCode.Attributes;component/Resources/Styles/Theme.Light.xaml";

    public static void ApplyInventorThemeResources()
    {
        if (System.Windows.Application.Current == null)
        {
            return;
        }

        ResourceDictionary resources = System.Windows.Application.Current.Resources;

        RemoveThemeDictionaries(resources);

        resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(SharedThemePath, UriKind.Absolute)
        });

        string themePath = Globals.ActiveTheme.Name == InventorThemeConstants.LightTheme
            ? LightThemePath
            : DarkThemePath;

        resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(themePath, UriKind.Absolute)
        });
    }

    private static void RemoveThemeDictionaries(ResourceDictionary resources)
    {
        List<ResourceDictionary> dictionariesToRemove = [.. resources.MergedDictionaries
            .Where(static x => x.Source != null &&
                            x.Source.OriginalString.Contains("/Resources/Styles/Theme.", StringComparison.Ordinal))];

        foreach (ResourceDictionary dictionary in dictionariesToRemove)
        {
            resources.MergedDictionaries.Remove(dictionary);
        }
    }
}