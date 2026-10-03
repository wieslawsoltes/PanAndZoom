// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom;

/// <summary>
/// Describes the behavior of mouse wheel operations.
/// </summary>
public enum WheelBehaviorMode
{
    /// <summary>
    /// Zoom in and out with the mouse wheel.
    /// </summary>
    Zoom,

    /// <summary>
    /// Pan vertically with the mouse wheel.
    /// </summary>
    PanVertical,

    /// <summary>
    /// Pan horizontally with the mouse wheel.
    /// </summary>
    PanHorizontal,

    /// <summary>
    /// Disable mouse wheel functionality.
    /// </summary>
    None
}
