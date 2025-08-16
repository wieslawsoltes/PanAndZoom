// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using Avalonia.Media;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Zoom operation event arguments.
/// </summary>
public class ZoomEventArgs : EventArgs
{
    /// <summary>
    /// Gets the current zoom ratio for x axis.
    /// </summary>
    public double ZoomX { get; }

    /// <summary>
    /// Gets the current zoom ratio for y axis.
    /// </summary>
    public double ZoomY { get; }

    /// <summary>
    /// Gets the previous zoom ratio for x axis.
    /// </summary>
    public double PreviousZoomX { get; }

    /// <summary>
    /// Gets the previous zoom ratio for y axis.
    /// </summary>
    public double PreviousZoomY { get; }

    /// <summary>
    /// Gets the zoom delta ratio.
    /// </summary>
    public double ZoomDelta { get; }

    /// <summary>
    /// Gets the zoom center point x coordinate.
    /// </summary>
    public double CenterX { get; }

    /// <summary>
    /// Gets the zoom center point y coordinate.
    /// </summary>
    public double CenterY { get; }

    /// <summary>
    /// Gets the current pan offset for x axis.
    /// </summary>
    public double OffsetX { get; }

    /// <summary>
    /// Gets the current pan offset for y axis.
    /// </summary>
    public double OffsetY { get; }

    /// <summary>
    /// Gets the current transformation matrix.
    /// </summary>
    public Matrix Matrix { get; }

    /// <summary>
    /// Gets the previous transformation matrix.
    /// </summary>
    public Matrix PreviousMatrix { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoomEventArgs"/> class.
    /// </summary>
    /// <param name="zoomX">The current zoom ratio for x axis.</param>
    /// <param name="zoomY">The current zoom ratio for y axis.</param>
    /// <param name="previousZoomX">The previous zoom ratio for x axis.</param>
    /// <param name="previousZoomY">The previous zoom ratio for y axis.</param>
    /// <param name="zoomDelta">The zoom delta ratio.</param>
    /// <param name="centerX">The zoom center point x coordinate.</param>
    /// <param name="centerY">The zoom center point y coordinate.</param>
    /// <param name="offsetX">The current pan offset for x axis.</param>
    /// <param name="offsetY">The current pan offset for y axis.</param>
    /// <param name="matrix">The current transformation matrix.</param>
    /// <param name="previousMatrix">The previous transformation matrix.</param>
    public ZoomEventArgs(double zoomX, double zoomY, double previousZoomX, double previousZoomY,
        double zoomDelta, double centerX, double centerY, double offsetX, double offsetY,
        Matrix matrix, Matrix previousMatrix)
    {
        ZoomX = zoomX;
        ZoomY = zoomY;
        PreviousZoomX = previousZoomX;
        PreviousZoomY = previousZoomY;
        ZoomDelta = zoomDelta;
        CenterX = centerX;
        CenterY = centerY;
        OffsetX = offsetX;
        OffsetY = offsetY;
        Matrix = matrix;
        PreviousMatrix = previousMatrix;
    }
}
