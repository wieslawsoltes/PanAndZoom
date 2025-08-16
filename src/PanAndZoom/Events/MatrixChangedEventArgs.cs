// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using Avalonia.Media;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Matrix changed event arguments.
/// </summary>
public class MatrixChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the current transformation matrix.
    /// </summary>
    public Matrix Matrix { get; }

    /// <summary>
    /// Gets the previous transformation matrix.
    /// </summary>
    public Matrix PreviousMatrix { get; }

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
    /// Gets the previous zoom ratio for x axis.
    /// </summary>
    public double PreviousZoomX { get; }

    /// <summary>
    /// Gets the previous zoom ratio for y axis.
    /// </summary>
    public double PreviousZoomY { get; }

    /// <summary>
    /// Gets the previous pan offset for x axis.
    /// </summary>
    public double PreviousOffsetX { get; }

    /// <summary>
    /// Gets the previous pan offset for y axis.
    /// </summary>
    public double PreviousOffsetY { get; }

    /// <summary>
    /// Gets the operation that caused the matrix change.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MatrixChangedEventArgs"/> class.
    /// </summary>
    /// <param name="matrix">The current transformation matrix.</param>
    /// <param name="previousMatrix">The previous transformation matrix.</param>
    /// <param name="zoomX">The current zoom ratio for x axis.</param>
    /// <param name="zoomY">The current zoom ratio for y axis.</param>
    /// <param name="offsetX">The current pan offset for x axis.</param>
    /// <param name="offsetY">The current pan offset for y axis.</param>
    /// <param name="previousZoomX">The previous zoom ratio for x axis.</param>
    /// <param name="previousZoomY">The previous zoom ratio for y axis.</param>
    /// <param name="previousOffsetX">The previous pan offset for x axis.</param>
    /// <param name="previousOffsetY">The previous pan offset for y axis.</param>
    /// <param name="operation">The operation that caused the matrix change.</param>
    public MatrixChangedEventArgs(Matrix matrix, Matrix previousMatrix, 
        double zoomX, double zoomY, double offsetX, double offsetY,
        double previousZoomX, double previousZoomY, double previousOffsetX, double previousOffsetY,
        string operation)
    {
        Matrix = matrix;
        PreviousMatrix = previousMatrix;
        ZoomX = zoomX;
        ZoomY = zoomY;
        OffsetX = offsetX;
        OffsetY = offsetY;
        PreviousZoomX = previousZoomX;
        PreviousZoomY = previousZoomY;
        PreviousOffsetX = previousOffsetX;
        PreviousOffsetY = previousOffsetY;
        Operation = operation;
    }
}
