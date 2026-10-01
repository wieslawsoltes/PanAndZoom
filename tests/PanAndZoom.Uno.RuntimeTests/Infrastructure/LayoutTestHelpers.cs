// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom.Uno.RuntimeTests.Infrastructure;

/// <summary>
/// Layout helpers for runtime tests.
/// </summary>
public static class LayoutTestHelpers
{
    /// <summary>
    /// Gets the layout position of <paramref name="element"/> relative to <paramref name="ancestor"/>
    /// (the WinUI equivalent of Avalonia <c>Bounds.Position</c>): the accumulated
    /// <see cref="UIElement.ActualOffset"/> values, excluding render transforms.
    /// </summary>
    public static Point GetLayoutOffset(UIElement element, UIElement ancestor)
    {
        double x = 0;
        double y = 0;
        DependencyObject? current = element;

        while (current is UIElement uiElement && !ReferenceEquals(current, ancestor))
        {
            var offset = uiElement.ActualOffset;
            x += offset.X;
            y += offset.Y;
            current = VisualTreeHelper.GetParent(current);
        }

        if (!ReferenceEquals(current, ancestor))
        {
            throw new InvalidOperationException("The element is not a descendant of the ancestor.");
        }

        return new Point(x, y);
    }
}
