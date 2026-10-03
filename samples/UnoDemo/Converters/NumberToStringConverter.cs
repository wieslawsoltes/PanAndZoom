// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace UnoDemo.Converters;

/// <summary>
/// Two-way converter between a numeric (<see cref="double"/> or <see cref="int"/>) source property and
/// <see cref="TextBox.Text"/>. Invalid input is ignored (the source keeps its previous value), which matches
/// the Avalonia demo where a <c>TextBox</c> is bound directly to numeric properties.
/// </summary>
public sealed class NumberToStringConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets a value indicating whether the source property is an <see cref="int"/>.
    /// </summary>
    public bool IsInteger { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value switch
        {
            double d => d.ToString(CultureInfo.CurrentCulture),
            int i => i.ToString(CultureInfo.CurrentCulture),
            IFormattable f => f.ToString(null, CultureInfo.CurrentCulture),
            _ => value?.ToString() ?? string.Empty
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        var text = value as string;

        if (IsInteger)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var i)
                ? i
                : DependencyProperty.UnsetValue;
        }

        return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var d)
            ? d
            : DependencyProperty.UnsetValue;
    }
}
