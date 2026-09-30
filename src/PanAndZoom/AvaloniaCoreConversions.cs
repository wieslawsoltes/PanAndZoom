// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Avalonia.Input;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Conversions between Avalonia primitives and the UI framework independent core primitives.
/// </summary>
internal static class AvaloniaCoreConversions
{
    public static CoreMatrix ToCore(this Matrix matrix) =>
        new(matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.M31, matrix.M32);

    public static Matrix ToAvalonia(this CoreMatrix matrix) =>
        new(matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.M31, matrix.M32);

    public static CorePoint ToCore(this Point point) => new(point.X, point.Y);

    public static Point ToAvalonia(this CorePoint point) => new(point.X, point.Y);

    public static CoreVector ToCore(this Vector vector) => new(vector.X, vector.Y);

    public static Vector ToAvalonia(this CoreVector vector) => new(vector.X, vector.Y);

    public static CoreSize ToCore(this Size size) => new(size.Width, size.Height);

    public static Size ToAvalonia(this CoreSize size) => new(size.Width, size.Height);

    public static CoreRect ToCore(this Rect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

    public static Rect ToAvalonia(this CoreRect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

    public static CoreThickness ToCore(this Thickness thickness) =>
        new(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);

    public static Thickness ToAvalonia(this CoreThickness thickness) =>
        new(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);

    public static ZoomBorderKeyModifiers ToCore(this KeyModifiers modifiers)
    {
        var result = ZoomBorderKeyModifiers.None;
        if ((modifiers & KeyModifiers.Alt) != 0) result |= ZoomBorderKeyModifiers.Alt;
        if ((modifiers & KeyModifiers.Control) != 0) result |= ZoomBorderKeyModifiers.Control;
        if ((modifiers & KeyModifiers.Shift) != 0) result |= ZoomBorderKeyModifiers.Shift;
        if ((modifiers & KeyModifiers.Meta) != 0) result |= ZoomBorderKeyModifiers.Meta;
        return result;
    }

    public static ZoomBorderKey ToCore(this Key key) => key switch
    {
        Key.Left => ZoomBorderKey.Left,
        Key.Right => ZoomBorderKey.Right,
        Key.Up => ZoomBorderKey.Up,
        Key.Down => ZoomBorderKey.Down,
        Key.Add or Key.OemPlus => ZoomBorderKey.Add,
        Key.Subtract or Key.OemMinus => ZoomBorderKey.Subtract,
        Key.D0 => ZoomBorderKey.D0,
        Key.Home => ZoomBorderKey.Home,
        _ => ZoomBorderKey.None
    };

    public static PanEventArgs ToAvalonia(this CorePanEventArgs e) =>
        new(e.ZoomX, e.ZoomY, e.OffsetX, e.OffsetY, e.PreviousOffsetX, e.PreviousOffsetY, e.DeltaX, e.DeltaY,
            e.Matrix.ToAvalonia(), e.PreviousMatrix.ToAvalonia());

    public static ZoomEventArgs ToAvalonia(this CoreZoomEventArgs e) =>
        new(e.ZoomX, e.ZoomY, e.PreviousZoomX, e.PreviousZoomY, e.ZoomDelta, e.CenterX, e.CenterY, e.OffsetX, e.OffsetY,
            e.Matrix.ToAvalonia(), e.PreviousMatrix.ToAvalonia());

    public static MatrixChangedEventArgs ToAvalonia(this CoreMatrixChangedEventArgs e) =>
        new(e.Matrix.ToAvalonia(), e.PreviousMatrix.ToAvalonia(), e.ZoomX, e.ZoomY, e.OffsetX, e.OffsetY,
            e.PreviousZoomX, e.PreviousZoomY, e.PreviousOffsetX, e.PreviousOffsetY, e.Operation);

    public static StretchModeChangedEventArgs ToAvalonia(this CoreStretchModeChangedEventArgs e) =>
        new(e.StretchMode, e.PreviousStretchMode, e.Matrix.ToAvalonia(), e.PreviousMatrix.ToAvalonia(),
            e.ZoomX, e.ZoomY, e.OffsetX, e.OffsetY, e.PanelWidth, e.PanelHeight, e.ElementWidth, e.ElementHeight);

    public static GestureEventArgs ToAvalonia(this CoreGestureEventArgs e) =>
        new(e.GestureType, e.ZoomX, e.ZoomY, e.OffsetX, e.OffsetY, e.CenterX, e.CenterY, e.Delta,
            e.Matrix.ToAvalonia(), e.PreviousMatrix.ToAvalonia());
}
