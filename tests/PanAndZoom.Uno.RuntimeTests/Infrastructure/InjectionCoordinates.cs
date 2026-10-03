// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System.Runtime.InteropServices;
using Windows.UI.Input.Preview.Injection;

namespace PanAndZoom.Uno.RuntimeTests.Infrastructure;

/// <summary>
/// Converts window (XAML root) coordinates to the coordinates expected by <see cref="InputInjector"/>.
/// </summary>
/// <remarks>
/// Uno injects input in window coordinates (logical pixels). The real Windows input injector used
/// by the native WinUI test app (PANANDZOOM_WINUI) works in physical screen pixels, and absolute
/// mouse moves are normalized to the virtual desktop (0..65535).
/// </remarks>
public static class InjectionCoordinates
{
    /// <summary>
    /// Converts a window position to an injected pointer location.
    /// </summary>
    public static InjectedInputPoint ToInjectedPoint(Point windowPoint)
    {
        var screen = ToScreen(windowPoint);
        return new InjectedInputPoint { PositionX = (int)Math.Round(screen.X), PositionY = (int)Math.Round(screen.Y) };
    }

    /// <summary>
    /// Adjusts touch frames to the requirements of the platform injector.
    /// </summary>
    /// <remarks>
    /// The Windows injector rejects touch frames with a non-zero time offset and requires the
    /// pressure and contact parameters to be declared (see the InputInjector touch samples).
    /// </remarks>
    public static IEnumerable<InjectedInputTouchInfo> Prepare(IEnumerable<InjectedInputTouchInfo> frame)
    {
#if PANANDZOOM_WINUI
        foreach (var info in frame)
        {
            var pointerInfo = info.PointerInfo;
            pointerInfo.TimeOffsetInMilliseconds = 0;
            info.PointerInfo = pointerInfo;
            info.TouchParameters = InjectedInputTouchParameters.Pressure | InjectedInputTouchParameters.Contact;
            if (info.Pressure <= 0)
            {
                info.Pressure = 1.0;
            }

            yield return info;
        }
#else
        return frame;
#endif
    }

    /// <summary>
    /// Converts a logical pointer id to the id passed to the injector.
    /// </summary>
    /// <remarks>
    /// Windows touch injection only accepts small pointer ids (a contact slot per id), while Uno
    /// accepts any id. Logical ids are unique per gesture, so a modulo keeps them distinct.
    /// </remarks>
    public static uint ToInjectedPointerId(uint id)
    {
#if PANANDZOOM_WINUI
        return id % 10;
#else
        return id;
#endif
    }

#if PANANDZOOM_WINUI
    /// <summary>
    /// Converts a window position (logical pixels) to physical screen pixels.
    /// </summary>
    public static Point ToScreen(Point windowPoint)
    {
        var scale = GetScale();
        var origin = new NativePoint { X = 0, Y = 0 };
        ClientToScreen(GetWindowHandle(), ref origin);
        return new Point(origin.X + windowPoint.X * scale, origin.Y + windowPoint.Y * scale);
    }

    /// <summary>
    /// Converts physical screen pixels to a window position (logical pixels).
    /// </summary>
    public static Point FromScreen(Point screenPoint)
    {
        var scale = GetScale();
        var origin = new NativePoint { X = 0, Y = 0 };
        ClientToScreen(GetWindowHandle(), ref origin);
        return new Point((screenPoint.X - origin.X) / scale, (screenPoint.Y - origin.Y) / scale);
    }

    /// <summary>
    /// Gets the current cursor position in window coordinates.
    /// </summary>
    public static Point GetCursorWindowPosition()
    {
        GetCursorPos(out var point);
        return FromScreen(new Point(point.X, point.Y));
    }

    /// <summary>
    /// Creates an absolute (virtual desktop normalized) mouse move to a window position.
    /// </summary>
    public static InjectedInputMouseInfo CreateAbsoluteMove(Point windowPoint)
    {
        var screen = ToScreen(windowPoint);
        var left = GetSystemMetrics(SM_XVIRTUALSCREEN);
        var top = GetSystemMetrics(SM_YVIRTUALSCREEN);
        var width = Math.Max(1, GetSystemMetrics(SM_CXVIRTUALSCREEN) - 1);
        var height = Math.Max(1, GetSystemMetrics(SM_CYVIRTUALSCREEN) - 1);

        return new InjectedInputMouseInfo
        {
            DeltaX = (int)Math.Round((Math.Round(screen.X) - left) * 65535.0 / width),
            DeltaY = (int)Math.Round((Math.Round(screen.Y) - top) * 65535.0 / height),
            MouseOptions = InjectedInputMouseOptions.Move | InjectedInputMouseOptions.Absolute | InjectedInputMouseOptions.VirtualDesk | InjectedInputMouseOptions.MoveNoCoalesce,
            TimeOffsetInMilliseconds = 1
        };
    }

    private static double GetScale() => App.MainWindow?.Content?.XamlRoot?.RasterizationScale ?? 1.0;

    private static IntPtr GetWindowHandle() => WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref NativePoint point);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);
#else
    /// <summary>
    /// Uno injects input in window coordinates, so no conversion is needed.
    /// </summary>
    public static Point ToScreen(Point windowPoint) => windowPoint;
#endif
}
