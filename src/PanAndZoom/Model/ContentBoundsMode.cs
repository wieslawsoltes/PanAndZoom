// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Describes how content bounds are restricted during pan and zoom operations.
/// </summary>
public enum ContentBoundsMode
{
    /// <summary>
    /// No bounds checking. Content can be panned completely out of view.
    /// </summary>
    Unrestricted,

    /// <summary>
    /// Keep at least some content visible at all times.
    /// </summary>
    KeepContentVisible,

    /// <summary>
    /// Never show empty space. Content always fills the viewport.
    /// </summary>
    FillViewport,

    /// <summary>
    /// Keep content centered within the viewport when possible.
    /// </summary>
    KeepCentered,

    /// <summary>
    /// Restrict the viewport and calculate scrollbar extents from the rectangle returned by
    /// <see cref="ZoomBorder.GetContentBounds"/>. Subclasses can additionally veto the final
    /// transform by overriding <see cref="ZoomBorder.ValidateTransform"/>.
    /// </summary>
    Custom
}
