// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace PanAndZoom.Uno.RuntimeTests.Infrastructure;

/// <summary>
/// Pointer input helpers that take <see cref="ZoomBorder"/> coordinates (like the Avalonia tests, which raise
/// events with positions relative to the control) and inject real input at the matching window position.
/// </summary>
public static class PointerTestHelpers
{
    /// <summary>
    /// Moves the mouse to a point in <paramref name="zoomBorder"/> coordinates and presses a button.
    /// </summary>
    public static void MouseDownAt(ZoomBorder zoomBorder, Point point, ButtonName button)
    {
        InputHelper.MouseMoveTo(ZoomBorderTestHelper.ToWindow(zoomBorder, point));
        InputHelper.MouseDown(button);
    }

    /// <summary>
    /// Moves the mouse to a point in <paramref name="zoomBorder"/> coordinates (pressed buttons stay pressed).
    /// </summary>
    public static void MouseMoveTo(ZoomBorder zoomBorder, Point point, int steps = 5)
    {
        InputHelper.MouseMoveTo(ZoomBorderTestHelper.ToWindow(zoomBorder, point), steps);
    }

    /// <summary>
    /// Scrolls the mouse wheel at a point in <paramref name="zoomBorder"/> coordinates.
    /// </summary>
    /// <param name="zoomBorder">The control.</param>
    /// <param name="point">The position in control coordinates.</param>
    /// <param name="delta">The WinUI wheel delta (120 per notch, equals Avalonia <c>Delta = 1</c>).</param>
    /// <param name="modifiers">The keyboard modifiers reported with the wheel input.</param>
    /// <param name="horizontal">True for a horizontal (tilt) wheel.</param>
    public static void MouseWheelAt(ZoomBorder zoomBorder, Point point, int delta, VirtualKeyModifiers modifiers = VirtualKeyModifiers.None, bool horizontal = false)
    {
        var position = ZoomBorderTestHelper.ToWindow(zoomBorder, point);
        if (modifiers == VirtualKeyModifiers.None)
        {
            InputHelper.MouseWheel(position, delta, horizontal);
        }
        else
        {
            InputHelper.MouseWheel(position, delta, modifiers, horizontal);
        }
    }

    /// <summary>
    /// Double clicks the left mouse button at a point in <paramref name="zoomBorder"/> coordinates.
    /// </summary>
    public static void MouseDoubleClickAt(ZoomBorder zoomBorder, Point point)
    {
        InputHelper.MouseDoubleClick(ZoomBorderTestHelper.ToWindow(zoomBorder, point));
    }

    /// <summary>
    /// Pinches with two fingers around a point in <paramref name="zoomBorder"/> coordinates.
    /// </summary>
    public static void PinchAt(ZoomBorder zoomBorder, Point center, double startDistance, double endDistance, int steps = 10)
    {
        InputHelper.Pinch(ZoomBorderTestHelper.ToWindow(zoomBorder, center), startDistance, endDistance, steps);
    }

    /// <summary>
    /// Drags one finger between two points in <paramref name="zoomBorder"/> coordinates.
    /// </summary>
    public static void TouchDragAt(ZoomBorder zoomBorder, Point from, Point to, int steps = 10)
    {
        InputHelper.TouchDrag(ZoomBorderTestHelper.ToWindow(zoomBorder, from), ZoomBorderTestHelper.ToWindow(zoomBorder, to), steps);
    }

    /// <summary>
    /// Gets the number of pointers currently captured by <paramref name="element"/>.
    /// </summary>
    public static int GetPointerCaptureCount(UIElement element)
    {
        return element.PointerCaptures?.Count ?? 0;
    }
}

/// <summary>
/// Hosts a <see cref="ZoomBorder"/> in a parent panel and records whether routed input events that bubble out of
/// the control were handled (the WinUI equivalent of asserting <c>RoutedEventArgs.Handled</c> in the Avalonia tests).
/// </summary>
public sealed class RoutedEventRecorder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoutedEventRecorder"/> class and wraps the control in a host.
    /// </summary>
    public RoutedEventRecorder(ZoomBorder zoomBorder)
    {
        ZoomBorder = zoomBorder;
        Host = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        Host.Children.Add(zoomBorder);

        Host.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler((_, e) =>
        {
            WheelCount++;
            WheelHandled = e.Handled;
        }), true);

        Host.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler((_, e) =>
        {
            CaptureLostCount++;
            CaptureLostHandled = e.Handled;
        }), true);

        Host.AddHandler(UIElement.ManipulationDeltaEvent, new ManipulationDeltaEventHandler((_, e) =>
        {
            ManipulationDeltaCount++;
            ManipulationDeltaHandled |= e.Handled;
        }), true);

        Host.AddHandler(UIElement.DoubleTappedEvent, new DoubleTappedEventHandler((_, e) =>
        {
            DoubleTappedCount++;
            DoubleTappedHandled = e.Handled;
        }), true);
    }

    /// <summary>
    /// Gets the recorded control.
    /// </summary>
    public ZoomBorder ZoomBorder { get; }

    /// <summary>
    /// Gets the host panel (load this into the test window).
    /// </summary>
    public Grid Host { get; }

    /// <summary>Gets the number of wheel events that reached the host.</summary>
    public int WheelCount { get; private set; }

    /// <summary>Gets a value indicating whether the last wheel event was handled by the control.</summary>
    public bool WheelHandled { get; private set; }

    /// <summary>Gets the number of capture lost events that reached the host.</summary>
    public int CaptureLostCount { get; private set; }

    /// <summary>Gets a value indicating whether the last capture lost event was handled by the control.</summary>
    public bool CaptureLostHandled { get; private set; }

    /// <summary>Gets the number of manipulation delta events that reached the host.</summary>
    public int ManipulationDeltaCount { get; private set; }

    /// <summary>Gets a value indicating whether any manipulation delta event was handled by the control.</summary>
    public bool ManipulationDeltaHandled { get; private set; }

    /// <summary>Gets the number of double tapped events that reached the host.</summary>
    public int DoubleTappedCount { get; private set; }

    /// <summary>Gets a value indicating whether the last double tapped event was handled by the control.</summary>
    public bool DoubleTappedHandled { get; private set; }

    /// <summary>
    /// Clears the recorded state.
    /// </summary>
    public void Reset()
    {
        WheelCount = 0;
        WheelHandled = false;
        CaptureLostCount = 0;
        CaptureLostHandled = false;
        ManipulationDeltaCount = 0;
        ManipulationDeltaHandled = false;
        DoubleTappedCount = 0;
        DoubleTappedHandled = false;
    }

    /// <summary>
    /// Creates a control (see <see cref="ZoomBorderTestHelper.Create"/>), wraps it in a recorder and loads the host.
    /// </summary>
    public static async Task<RoutedEventRecorder> CreateAndLoadAsync(
        double width = 400,
        double height = 400,
        double childWidth = 200,
        double childHeight = 200,
        Action<ZoomBorder>? configure = null)
    {
        var (zoomBorder, _) = ZoomBorderTestHelper.Create(width, height, childWidth, childHeight, configure);
        var recorder = new RoutedEventRecorder(zoomBorder);
        await ZoomBorderTestHelper.LoadAsync(recorder.Host);
        return recorder;
    }
}
