// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace UnoDemo.Converters;

/// <summary>
/// Formats the bound value with a composite format string passed as the converter parameter
/// (WinUI bindings have no <c>StringFormat</c>, unlike Avalonia bindings).
/// </summary>
/// <remarks>
/// The parameter may be a composite format (<c>Zoom: {0:F2}x</c>) or a plain format specifier (<c>F3</c>).
/// </remarks>
public sealed class StringFormatConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (parameter is not string format || string.IsNullOrEmpty(format))
        {
            return value?.ToString() ?? string.Empty;
        }

        if (!format.Contains('{'))
        {
            return value is IFormattable formattable
                ? formattable.ToString(format, CultureInfo.CurrentCulture)
                : value?.ToString() ?? string.Empty;
        }

        return string.Format(CultureInfo.CurrentCulture, format, value);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotSupportedException();
    }
}
