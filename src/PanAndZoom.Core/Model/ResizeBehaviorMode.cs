// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace PanAndZoom;

/// <summary>
/// Describes how the view is adjusted when the control is resized.
/// </summary>
public enum ResizeBehaviorMode
{
    /// <summary>
    /// No special handling. View stays as-is during resize.
    /// </summary>
    None,

    /// <summary>
    /// Maintain the center point of the viewport during resize.
    /// </summary>
    MaintainCenter,

    /// <summary>
    /// Maintain the top-left position during resize.
    /// </summary>
    MaintainTopLeft,

    /// <summary>
    /// Maintain zoom level and adjust position proportionally.
    /// </summary>
    MaintainZoom,

    /// <summary>
    /// Reapply the current stretch mode on resize.
    /// </summary>
    ReapplyStretch,

    /// <summary>
    /// Use custom resize logic via virtual method.
    /// </summary>
    Custom
}
