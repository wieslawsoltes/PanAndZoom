// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace UnoDemo;

/// <summary>
/// Keyboard helpers shared by the demo pages.
/// </summary>
public static class DemoKeyboard
{
    /// <summary>
    /// Gets a value indicating whether a Control key is currently pressed.
    /// </summary>
    public static bool IsControlPressed => IsKeyDown(VirtualKey.Control);

    /// <summary>
    /// Adds the demo shortcuts advertised by the "Basic Demo" and "Keyboard &amp; Reset" pages to a
    /// <see cref="ZoomBorder"/>: F - Fill, U - Uniform, R - Reset, T - Toggle stretch mode.
    /// </summary>
    /// <remarks>
    /// The shortcuts are handled by the demo (not by the control), the control itself handles the arrow,
    /// +/-, Ctrl+0 and Home keys. The <see cref="ZoomBorder"/> must have focus (click on it first).
    /// </remarks>
    /// <param name="zoomBorder">The zoom border.</param>
    public static void AttachStretchShortcuts(ZoomBorder zoomBorder)
    {
        zoomBorder.KeyDown += ZoomBorder_KeyDown;
    }

    private static void ZoomBorder_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled || sender is not ZoomBorder zoomBorder || IsControlPressed)
        {
            return;
        }

        switch (e.Key)
        {
            case VirtualKey.F:
                zoomBorder.Fill();
                e.Handled = true;
                break;
            case VirtualKey.U:
                zoomBorder.Uniform();
                e.Handled = true;
                break;
            case VirtualKey.R:
                zoomBorder.ResetMatrix();
                e.Handled = true;
                break;
            case VirtualKey.T:
                zoomBorder.ToggleStretchMode();
                e.Handled = true;
                break;
        }
    }

    private static bool IsKeyDown(VirtualKey key)
    {
        try
        {
            return (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) != 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
