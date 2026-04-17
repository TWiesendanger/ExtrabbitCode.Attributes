using System;
using System.Globalization;
using System.Linq;

namespace ExtrabbitCode.Inventor.Attributes.Helper;

internal static class AttributeValueConverter
{
    public static object ConvertToTypedValue(
        string rawValue,
        ValueTypeEnum valueType)
    {
        return valueType switch
        {
            ValueTypeEnum.kStringType => rawValue,
            ValueTypeEnum.kBooleanType => rawValue,
            ValueTypeEnum.kIntegerType => int.Parse(
                rawValue,
                CultureInfo.InvariantCulture),
            ValueTypeEnum.kDoubleType => double.Parse(
                rawValue,
                CultureInfo.InvariantCulture),
            ValueTypeEnum.kByteArrayType => ParseHexToByteArray(rawValue),
            _ => rawValue
        };
    }

    public static string FormatAttributeValue(
        object? value,
        ValueTypeEnum valueType)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return valueType switch
        {
            ValueTypeEnum.kByteArrayType => FormatByteArrayAsHex(value),
            ValueTypeEnum.kDoubleType => Convert.ToDouble(
                value,
                CultureInfo.InvariantCulture).ToString(
                CultureInfo.InvariantCulture),
            ValueTypeEnum.kIntegerType => Convert.ToInt32(
                value,
                CultureInfo.InvariantCulture).ToString(
                CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string FormatByteArrayAsHex(object value)
    {
        if (value is byte[] bytes)
        {
            return string.Join(
                " ",
                bytes.Select(
                    b => b.ToString("X2", CultureInfo.InvariantCulture)));
        }

        if (value is Array array)
        {
            byte[] bytesFromArray = [.. array.Cast<object>().Select(Convert.ToByte)];

            return string.Join(
                " ",
                bytesFromArray.Select(
                    b => b.ToString("X2", CultureInfo.InvariantCulture)));
        }

        return value.ToString() ?? string.Empty;
    }

    private static byte[] ParseHexToByteArray(string input)
    {
        string normalized = input
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(":", string.Empty, StringComparison.Ordinal);

        if (normalized.Length == 0)
        {
            return [];
        }

        if (normalized.Length % 2 != 0)
        {
            throw new FormatException(
                "Hex value must contain an even number of characters.");
        }

        byte[] result = new byte[normalized.Length / 2];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = byte.Parse(
                normalized.AsSpan(i * 2, 2),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture);
        }

        return result;
    }
}