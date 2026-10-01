// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI.Xaml.Data;

namespace UnoDemo.Converters;

/// <summary>
/// Returns <c>true</c> when the bound string is not null or empty (the WinUI equivalent of the Avalonia
/// <c>StringConverters.IsNotNullOrEmpty</c> converter).
/// </summary>
public sealed class StringIsNotNullOrEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return !string.IsNullOrEmpty(value as string);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotSupportedException();
    }
}
