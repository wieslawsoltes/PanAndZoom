// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI.Xaml.Input;

namespace PanAndZoom.Uno.RuntimeTests.Infrastructure;

/// <summary>
/// Small helpers shared by the ported Avalonia API tests.
/// </summary>
public static class ApiTestHelpers
{
    /// <summary>
    /// Injects a real mouse wheel event at a point in <paramref name="zoomBorder"/> coordinates and reports
    /// whether the control marked it as handled (observed on the parent with handledEventsToo).
    /// </summary>
    /// <param name="zoomBorder">The loaded zoom border.</param>
    /// <param name="pointInZoomBorder">The wheel position relative to the zoom border.</param>
    /// <param name="delta">The WinUI wheel delta (120 per notch).</param>
    /// <returns>True when the wheel event reached the parent marked as handled.</returns>
    public static async Task<bool> MouseWheelAsync(ZoomBorder zoomBorder, Point pointInZoomBorder, int delta)
    {
        var parent = VisualTreeHelper.GetParent(zoomBorder) as UIElement
            ?? throw new InvalidOperationException("The zoom border must be loaded.");

        bool? handled = null;
        PointerEventHandler handler = (_, e) => handled = e.Handled;
        parent.AddHandler(UIElement.PointerWheelChangedEvent, handler, handledEventsToo: true);
        try
        {
            InputHelper.MouseWheel(ZoomBorderTestHelper.ToWindow(zoomBorder, pointInZoomBorder), delta);
            await ZoomBorderTestHelper.WaitForIdleAsync();
        }
        finally
        {
            parent.RemoveHandler(UIElement.PointerWheelChangedEvent, handler);
        }

        return handled == true;
    }

    /// <summary>
    /// Returns the exception thrown by <paramref name="action"/>, or null (xUnit <c>Record.Exception</c>).
    /// </summary>
    public static Exception? RecordException(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
