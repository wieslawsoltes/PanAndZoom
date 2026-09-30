// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;

namespace PanAndZoom.Core;

/// <summary>
/// UI framework independent keys handled by the pan and zoom keyboard navigation.
/// </summary>
public enum ZoomBorderKey
{
    /// <summary>
    /// A key that is not handled by the pan and zoom engine.
    /// </summary>
    None,

    /// <summary>
    /// The left arrow key.
    /// </summary>
    Left,

    /// <summary>
    /// The right arrow key.
    /// </summary>
    Right,

    /// <summary>
    /// The up arrow key.
    /// </summary>
    Up,

    /// <summary>
    /// The down arrow key.
    /// </summary>
    Down,

    /// <summary>
    /// The add (+ or =) key.
    /// </summary>
    Add,

    /// <summary>
    /// The subtract (-) key.
    /// </summary>
    Subtract,

    /// <summary>
    /// The zero (0) key.
    /// </summary>
    D0,

    /// <summary>
    /// The home key.
    /// </summary>
    Home
}

/// <summary>
/// UI framework independent keyboard modifiers.
/// </summary>
[Flags]
public enum ZoomBorderKeyModifiers
{
    /// <summary>
    /// No modifiers.
    /// </summary>
    None = 0,

    /// <summary>
    /// The alt key.
    /// </summary>
    Alt = 1,

    /// <summary>
    /// The control key.
    /// </summary>
    Control = 2,

    /// <summary>
    /// The shift key.
    /// </summary>
    Shift = 4,

    /// <summary>
    /// The meta (Windows or Command) key.
    /// </summary>
    Meta = 8
}

/// <summary>
/// UI framework independent pointer buttons.
/// </summary>
[Flags]
public enum ZoomBorderPointerButtons
{
    /// <summary>
    /// No buttons are pressed.
    /// </summary>
    None = 0,

    /// <summary>
    /// The left button (or touch contact / pen tip).
    /// </summary>
    Left = 1,

    /// <summary>
    /// The right button.
    /// </summary>
    Right = 2,

    /// <summary>
    /// The middle button.
    /// </summary>
    Middle = 4
}
