// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using Avalonia.Media;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Pan operation event arguments.
/// </summary>
public class PanEventArgs : EventArgs
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
    /// Gets the current pan offset for x axis.
    /// </summary>
    public double OffsetX { get; }

    /// <summary>
    /// Gets the current pan offset for y axis.
    /// </summary>
    public double OffsetY { get; }

    /// <summary>
    /// Gets the previous pan offset for x axis.
    /// </summary>
    public double PreviousOffsetX { get; }

    /// <summary>
    /// Gets the previous pan offset for y axis.
    /// </summary>
    public double PreviousOffsetY { get; }

    /// <summary>
    /// Gets the pan delta for x axis.
    /// </summary>
    public double DeltaX { get; }

    /// <summary>
    /// Gets the pan delta for y axis.
    /// </summary>
    public double DeltaY { get; }

    /// <summary>
    /// Gets the current transformation matrix.
    /// </summary>
    public Matrix Matrix { get; }

    /// <summary>
    /// Gets the previous transformation matrix.
    /// </summary>
    public Matrix PreviousMatrix { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PanEventArgs"/> class.
    /// </summary>
    /// <param name="zoomX">The current zoom ratio for x axis.</param>
    /// <param name="zoomY">The current zoom ratio for y axis.</param>
    /// <param name="offsetX">The current pan offset for x axis.</param>
    /// <param name="offsetY">The current pan offset for y axis.</param>
    /// <param name="previousOffsetX">The previous pan offset for x axis.</param>
    /// <param name="previousOffsetY">The previous pan offset for y axis.</param>
    /// <param name="deltaX">The pan delta for x axis.</param>
    /// <param name="deltaY">The pan delta for y axis.</param>
    /// <param name="matrix">The current transformation matrix.</param>
    /// <param name="previousMatrix">The previous transformation matrix.</param>
    public PanEventArgs(double zoomX, double zoomY, double offsetX, double offsetY, 
        double previousOffsetX, double previousOffsetY, double deltaX, double deltaY,
        Matrix matrix, Matrix previousMatrix)
    {
        ZoomX = zoomX;
        ZoomY = zoomY;
        OffsetX = offsetX;
        OffsetY = offsetY;
        PreviousOffsetX = previousOffsetX;
        PreviousOffsetY = previousOffsetY;
        DeltaX = deltaX;
        DeltaY = deltaY;
        Matrix = matrix;
        PreviousMatrix = previousMatrix;
    }
}
