using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MauiStartup.Converters;

public class StringToNullableIntConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is int year)
            return year.ToString(culture);

        return string.Empty;
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        string? text = value?.ToString();

        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (int.TryParse(
            text,
            NumberStyles.Integer,
            culture,
            out int year))
        {
            return year;
        }

        return null;
    }
}
