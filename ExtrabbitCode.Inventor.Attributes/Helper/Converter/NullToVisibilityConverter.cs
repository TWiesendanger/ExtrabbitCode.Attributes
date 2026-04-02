using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ExtrabbitCode.Inventor.Attributes.Helper.Converter;

public sealed class NullToVisibilityConverter : IValueConverter
{
    public bool CollapseWhenNull { get; set; } = true;

    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        bool hasValue = value switch
        {
            null => false,
            string text => !string.IsNullOrWhiteSpace(text),
            _ => true
        };

        if (hasValue)
        {
            return Visibility.Visible;
        }

        return CollapseWhenNull
            ? Visibility.Collapsed
            : Visibility.Hidden;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}