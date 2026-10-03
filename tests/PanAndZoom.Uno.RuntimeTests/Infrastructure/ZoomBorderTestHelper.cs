// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;

namespace PanAndZoom.Uno.RuntimeTests.Infrastructure;

/// <summary>
/// Helpers to host a <see cref="ZoomBorder"/> in the runtime test window.
/// </summary>
public static class ZoomBorderTestHelper
{
    /// <summary>
    /// Creates a <see cref="ZoomBorder"/> with a fixed size and a fixed size <see cref="Border"/> child.
    /// </summary>
    public static (ZoomBorder ZoomBorder, Border Child) Create(
        double width = 400,
        double height = 400,
        double childWidth = 200,
        double childHeight = 200,
        Action<ZoomBorder>? configure = null)
    {
        var child = new Border
        {
            Width = childWidth,
            Height = childHeight,
            Background = new SolidColorBrush(Colors.CornflowerBlue)
        };

        var zoomBorder = new ZoomBorder
        {
            Width = width,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = child
        };

        configure?.Invoke(zoomBorder);
        return (zoomBorder, child);
    }

    /// <summary>
    /// Creates a <see cref="ZoomBorder"/> (see <see cref="Create"/>) and loads it into the test window.
    /// </summary>
    public static async Task<(ZoomBorder ZoomBorder, Border Child)> CreateAndLoadAsync(
        double width = 400,
        double height = 400,
        double childWidth = 200,
        double childHeight = 200,
        Action<ZoomBorder>? configure = null)
    {
        var (zoomBorder, child) = Create(width, height, childWidth, childHeight, configure);
        await LoadAsync(zoomBorder);
        return (zoomBorder, child);
    }

    /// <summary>
    /// Loads an element into the test window and waits until the layout is idle.
    /// </summary>
    public static async Task LoadAsync(FrameworkElement element)
    {
        UnitTestsUIContentHelper.Content = element;
        await UnitTestsUIContentHelper.WaitForLoaded(element);
        await WaitForIdleAsync();
    }

    /// <summary>
    /// Removes the current content from the test window.
    /// </summary>
    public static async Task UnloadAsync()
    {
        UnitTestsUIContentHelper.Content = null;
        await WaitForIdleAsync();
    }

    /// <summary>
    /// Waits for the dispatcher and layout to be idle.
    /// </summary>
    public static async Task WaitForIdleAsync()
    {
#if PANANDZOOM_WINUI
        // Real Windows input injection is asynchronous (it goes through the OS input queue), so give
        // the injected input time to be dispatched before waiting for the UI thread to be idle.
        await Task.Delay(InputSettleDelayMilliseconds);
        await UnitTestsUIContentHelper.WaitForIdle();
        InputHelper.ReleaseModifierKeys();
#endif
        await UnitTestsUIContentHelper.WaitForIdle();
    }

#if PANANDZOOM_WINUI
    /// <summary>
    /// Time given to the Windows input queue to dispatch injected input.
    /// </summary>
    public const int InputSettleDelayMilliseconds = 150;
#endif

    /// <summary>
    /// Waits until a condition is met (polling on the UI thread).
    /// </summary>
    public static async Task<bool> WaitForAsync(Func<bool> condition, int timeoutMilliseconds = 3000)
    {
        var start = Environment.TickCount64;
        while (!condition())
        {
            if (Environment.TickCount64 - start > timeoutMilliseconds)
            {
                return false;
            }

            await Task.Delay(16);
        }

        return true;
    }

    /// <summary>
    /// Converts a point in <paramref name="element"/> coordinates to window coordinates (used for input injection).
    /// </summary>
    public static Point ToWindow(UIElement element, Point point)
    {
        return element.TransformToVisual(null).TransformPoint(point);
    }

    /// <summary>
    /// Gets the center of an element in window coordinates.
    /// </summary>
    public static Point GetWindowCenter(FrameworkElement element)
    {
        return ToWindow(element, new Point(element.ActualWidth / 2, element.ActualHeight / 2));
    }

    /// <summary>
    /// Gets the render transform matrix currently applied to the child element.
    /// </summary>
    public static Matrix GetChildRenderMatrix(ZoomBorder zoomBorder)
    {
        return zoomBorder.Child?.RenderTransform is MatrixTransform transform ? transform.Matrix : Matrix.Identity;
    }

    /// <summary>
    /// Simulates a key press routed through the control keyboard handler.
    /// </summary>
    /// <remarks>
    /// Keyboard injection is not implemented by Uno on Skia, so keys are routed to the same
    /// handler used by <c>OnKeyDown</c>.
    /// </remarks>
    public static bool PressKey(ZoomBorder zoomBorder, VirtualKey key, ZoomBorderKeyModifiers modifiers = ZoomBorderKeyModifiers.None)
    {
        return zoomBorder.HandleKeyDown(key, modifiers);
    }

    /// <summary>
    /// Simulates a wheel change with keyboard modifiers routed through the control wheel handler.
    /// </summary>
    /// <remarks>
    /// Injected mouse input cannot carry keyboard modifiers, so modifier combinations are routed to
    /// the same handler used by <c>OnPointerWheelChanged</c>. Use <see cref="InputHelper.MouseWheel"/>
    /// for real (unmodified) wheel input.
    /// </remarks>
    public static bool Wheel(ZoomBorder zoomBorder, int mouseWheelDelta, Point positionInChild, ZoomBorderKeyModifiers modifiers = ZoomBorderKeyModifiers.None, bool horizontal = false)
    {
        return zoomBorder.HandlePointerWheel(mouseWheelDelta, horizontal, positionInChild.ToCore(), modifiers);
    }

    private static CorePoint ToCore(this Point point) => new(point.X, point.Y);
}
