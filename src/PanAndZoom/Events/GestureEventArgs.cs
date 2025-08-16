// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using Avalonia.Media;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Gesture operation event arguments.
/// </summary>
public class GestureEventArgs : EventArgs
{
    /// <summary>
    /// Gets the type of gesture.
    /// </summary>
    public string GestureType { get; }

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
    /// Gets the gesture center point x coordinate.
    /// </summary>
    public double CenterX { get; }

    /// <summary>
    /// Gets the gesture center point y coordinate.
    /// </summary>
    public double CenterY { get; }

    /// <summary>
    /// Gets the gesture delta value (for zoom gestures).
    /// </summary>
    public double Delta { get; }

    /// <summary>
    /// Gets the current transformation matrix.
    /// </summary>
    public Matrix Matrix { get; }

    /// <summary>
    /// Gets the previous transformation matrix.
    /// </summary>
    public Matrix PreviousMatrix { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GestureEventArgs"/> class.
    /// </summary>
    /// <param name="gestureType">The type of gesture.</param>
    /// <param name="zoomX">The current zoom ratio for x axis.</param>
    /// <param name="zoomY">The current zoom ratio for y axis.</param>
    /// <param name="offsetX">The current pan offset for x axis.</param>
    /// <param name="offsetY">The current pan offset for y axis.</param>
    /// <param name="centerX">The gesture center point x coordinate.</param>
    /// <param name="centerY">The gesture center point y coordinate.</param>
    /// <param name="delta">The gesture delta value.</param>
    /// <param name="matrix">The current transformation matrix.</param>
    /// <param name="previousMatrix">The previous transformation matrix.</param>
    public GestureEventArgs(string gestureType, double zoomX, double zoomY, double offsetX, double offsetY,
        double centerX, double centerY, double delta, Matrix matrix, Matrix previousMatrix)
    {
        GestureType = gestureType;
        ZoomX = zoomX;
        ZoomY = zoomY;
        OffsetX = offsetX;
        OffsetY = offsetY;
        CenterX = centerX;
        CenterY = centerY;
        Delta = delta;
        Matrix = matrix;
        PreviousMatrix = previousMatrix;
    }
}
