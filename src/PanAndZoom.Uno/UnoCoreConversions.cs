// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI.Xaml;
using Windows.System;

namespace PanAndZoom;

/// <summary>
/// Conversions between WinUI (Uno Platform) primitives and the UI framework independent core primitives.
/// </summary>
internal static class UnoCoreConversions
{
    public static CoreMatrix ToCore(this Matrix matrix) =>
        new(matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.OffsetX, matrix.OffsetY);

    public static Matrix ToUno(this CoreMatrix matrix) =>
        new(matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.M31, matrix.M32);

    public static CorePoint ToCore(this Point point) => new(point.X, point.Y);

    public static Point ToUno(this CorePoint point) => new(point.X, point.Y);

    public static CoreVector ToCoreVector(this Point vector) => new(vector.X, vector.Y);

    public static Point ToUno(this CoreVector vector) => new(vector.X, vector.Y);

    public static CoreSize ToCore(this Size size) => new(size.Width, size.Height);

    public static Size ToUno(this CoreSize size) => new(Sanitize(size.Width), Sanitize(size.Height));

    public static CoreRect ToCore(this Rect rect) =>
        rect.IsEmpty ? default : new(rect.X, rect.Y, rect.Width, rect.Height);

    /// <summary>
    /// Converts to a WinUI rectangle. WinUI rectangles cannot have a negative size, so
    /// negative sizes are normalized by moving the origin.
    /// </summary>
    public static Rect ToUno(this CoreRect rect)
    {
        var x = rect.Width < 0 ? rect.X + rect.Width : rect.X;
        var y = rect.Height < 0 ? rect.Y + rect.Height : rect.Y;
        return new Rect(x, y, System.Math.Abs(rect.Width), System.Math.Abs(rect.Height));
    }

    public static CoreThickness ToCore(this Thickness thickness) =>
        new(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);

    public static Thickness ToUno(this CoreThickness thickness) =>
        new(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);

    public static ZoomBorderKeyModifiers ToCore(this VirtualKeyModifiers modifiers)
    {
        var result = ZoomBorderKeyModifiers.None;
        if ((modifiers & VirtualKeyModifiers.Menu) != 0) result |= ZoomBorderKeyModifiers.Alt;
        if ((modifiers & VirtualKeyModifiers.Control) != 0) result |= ZoomBorderKeyModifiers.Control;
        if ((modifiers & VirtualKeyModifiers.Shift) != 0) result |= ZoomBorderKeyModifiers.Shift;
        if ((modifiers & VirtualKeyModifiers.Windows) != 0) result |= ZoomBorderKeyModifiers.Meta;
        return result;
    }

    public static ZoomBorderKey ToCore(this VirtualKey key) => key switch
    {
        VirtualKey.Left => ZoomBorderKey.Left,
        VirtualKey.Right => ZoomBorderKey.Right,
        VirtualKey.Up => ZoomBorderKey.Up,
        VirtualKey.Down => ZoomBorderKey.Down,
        VirtualKey.Add or (VirtualKey)187 => ZoomBorderKey.Add, // 187 = VK_OEM_PLUS
        VirtualKey.Subtract or (VirtualKey)189 => ZoomBorderKey.Subtract, // 189 = VK_OEM_MINUS
        VirtualKey.Number0 or VirtualKey.NumberPad0 => ZoomBorderKey.D0,
        VirtualKey.Home => ZoomBorderKey.Home,
        _ => ZoomBorderKey.None
    };

    public static PanEventArgs ToUno(this CorePanEventArgs e) =>
        new(e.ZoomX, e.ZoomY, e.OffsetX, e.OffsetY, e.PreviousOffsetX, e.PreviousOffsetY, e.DeltaX, e.DeltaY,
            e.Matrix.ToUno(), e.PreviousMatrix.ToUno());

    public static ZoomEventArgs ToUno(this CoreZoomEventArgs e) =>
        new(e.ZoomX, e.ZoomY, e.PreviousZoomX, e.PreviousZoomY, e.ZoomDelta, e.CenterX, e.CenterY, e.OffsetX, e.OffsetY,
            e.Matrix.ToUno(), e.PreviousMatrix.ToUno());

    public static MatrixChangedEventArgs ToUno(this CoreMatrixChangedEventArgs e) =>
        new(e.Matrix.ToUno(), e.PreviousMatrix.ToUno(), e.ZoomX, e.ZoomY, e.OffsetX, e.OffsetY,
            e.PreviousZoomX, e.PreviousZoomY, e.PreviousOffsetX, e.PreviousOffsetY, e.Operation);

    public static StretchModeChangedEventArgs ToUno(this CoreStretchModeChangedEventArgs e) =>
        new(e.StretchMode, e.PreviousStretchMode, e.Matrix.ToUno(), e.PreviousMatrix.ToUno(),
            e.ZoomX, e.ZoomY, e.OffsetX, e.OffsetY, e.PanelWidth, e.PanelHeight, e.ElementWidth, e.ElementHeight);

    public static GestureEventArgs ToUno(this CoreGestureEventArgs e) =>
        new(e.GestureType, e.ZoomX, e.ZoomY, e.OffsetX, e.OffsetY, e.CenterX, e.CenterY, e.Delta,
            e.Matrix.ToUno(), e.PreviousMatrix.ToUno());

    private static double Sanitize(double value) => double.IsNaN(value) || value < 0 ? 0 : value;
}
