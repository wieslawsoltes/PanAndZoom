// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom;

/// <summary>
/// Describes the behavior of double-click zoom operations.
/// </summary>
public enum DoubleClickZoomMode
{
    /// <summary>
    /// Always zoom in on double-click.
    /// </summary>
    ZoomIn,

    /// <summary>
    /// Always zoom out on double-click.
    /// </summary>
    ZoomOut,

    /// <summary>
    /// Toggle between zooming in and zooming out based on current zoom level.
    /// </summary>
    ZoomInOut,

    /// <summary>
    /// Fit content to viewport on double-click.
    /// </summary>
    ZoomToFit,

    /// <summary>
    /// Disable double-click zoom functionality.
    /// </summary>
    None
}
