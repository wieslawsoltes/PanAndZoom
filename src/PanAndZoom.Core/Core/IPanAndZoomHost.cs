// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;

namespace PanAndZoom.Core;

/// <summary>
/// The UI framework specific side of a pan and zoom control used by <see cref="PanAndZoomEngine"/>.
/// </summary>
/// <remarks>
/// The host supplies layout information (viewport and child element sizes), applies the
/// computed render transform to the child element and exposes the virtual extension points of the control.
/// </remarks>
public interface IPanAndZoomHost
{
    /// <summary>
    /// Gets a value indicating whether the control has a child element attached.
    /// </summary>
    bool HasElement { get; }

    /// <summary>
    /// Gets the size of the viewport (the pan and zoom control bounds).
    /// </summary>
    CoreSize ViewportSize { get; }

    /// <summary>
    /// Gets the layout size of the child element (untransformed).
    /// </summary>
    CoreSize ElementSize { get; }

    /// <summary>
    /// Gets the layout offset where the UI framework positioned the child element inside the control.
    /// </summary>
    /// <remarks>
    /// The offset is expressed in viewport coordinates and is not scaled by the transform matrix.
    /// </remarks>
    CorePoint ElementLayoutOffset { get; }

    /// <summary>
    /// Calculates automatic zoom limits (virtual extension point).
    /// </summary>
    /// <returns>The minimum and maximum zoom values.</returns>
    (double minZoom, double maxZoom) CalculateAutoZoomLimits();

    /// <summary>
    /// Gets the content bounds used by <see cref="ContentBoundsMode.Custom"/> (virtual extension point).
    /// </summary>
    /// <returns>The content bounds in content coordinates.</returns>
    CoreRect GetContentBounds();

    /// <summary>
    /// Validates a proposed transform matrix used by <see cref="ContentBoundsMode.Custom"/> (virtual extension point).
    /// </summary>
    /// <param name="matrix">The proposed matrix.</param>
    /// <returns>True when the matrix is valid.</returns>
    bool ValidateTransform(CoreMatrix matrix);

    /// <summary>
    /// Called when the control is resized in <see cref="ResizeBehaviorMode.Custom"/> mode (virtual extension point).
    /// </summary>
    /// <param name="oldSize">The previous size.</param>
    /// <param name="newSize">The new size.</param>
    void OnResized(CoreSize oldSize, CoreSize newSize);

    /// <summary>
    /// Publishes the current zoom and offset values so the control can raise property change notifications.
    /// </summary>
    /// <param name="zoomX">The zoom ratio for x axis.</param>
    /// <param name="zoomY">The zoom ratio for y axis.</param>
    /// <param name="offsetX">The pan offset for x axis.</param>
    /// <param name="offsetY">The pan offset for y axis.</param>
    void UpdateViewProperties(double zoomX, double zoomY, double offsetX, double offsetY);

    /// <summary>
    /// Applies the render transform to the child element.
    /// </summary>
    /// <param name="renderMatrix">The final render matrix (pan, zoom and rotation) relative to the element top left corner.</param>
    /// <param name="skipTransitions">True when transitions or animations must not be used for this update.</param>
    void ApplyTransform(CoreMatrix renderMatrix, bool skipTransitions);

    /// <summary>
    /// Called when the pointer panning state changes.
    /// </summary>
    /// <param name="isPanning">The new panning state.</param>
    void OnIsPanningChanged(bool isPanning);

    /// <summary>
    /// Called when the zoom indicator visibility changes.
    /// </summary>
    /// <param name="isVisible">The new visibility.</param>
    void OnZoomIndicatorVisibilityChanged(bool isVisible);

    /// <summary>
    /// (Re)starts the zoom indicator auto-hide timer. When it elapses the host calls <see cref="PanAndZoomEngine.HideZoomIndicator"/>.
    /// </summary>
    /// <param name="duration">The timer interval.</param>
    void StartZoomIndicatorTimer(TimeSpan duration);
}
