// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace PanAndZoom;

/// <summary>
/// Defines positions for the zoom indicator.
/// </summary>
public enum ZoomIndicatorPosition
{
    /// <summary>
    /// Top left position.
    /// </summary>
    TopLeft,

    /// <summary>
    /// Top right position.
    /// </summary>
    TopRight,

    /// <summary>
    /// Bottom left position.
    /// </summary>
    BottomLeft,

    /// <summary>
    /// Bottom right position.
    /// </summary>
    BottomRight,

    /// <summary>
    /// Custom position using virtual method.
    /// </summary>
    Custom
}
