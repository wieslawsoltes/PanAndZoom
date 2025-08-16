// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using Avalonia.Media;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Stretch mode changed event arguments.
/// </summary>
public class StretchModeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the current stretch mode.
    /// </summary>
    public StretchMode StretchMode { get; }

    /// <summary>
    /// Gets the previous stretch mode.
    /// </summary>
    public StretchMode PreviousStretchMode { get; }

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
    /// Gets the panel width used for the stretch calculation.
    /// </summary>
    public double PanelWidth { get; }

    /// <summary>
    /// Gets the panel height used for the stretch calculation.
    /// </summary>
    public double PanelHeight { get; }

    /// <summary>
    /// Gets the element width used for the stretch calculation.
    /// </summary>
    public double ElementWidth { get; }

    /// <summary>
    /// Gets the element height used for the stretch calculation.
    /// </summary>
    public double ElementHeight { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StretchModeChangedEventArgs"/> class.
    /// </summary>
    /// <param name="stretchMode">The current stretch mode.</param>
    /// <param name="previousStretchMode">The previous stretch mode.</param>
    /// <param name="matrix">The current transformation matrix.</param>
    /// <param name="previousMatrix">The previous transformation matrix.</param>
    /// <param name="zoomX">The current zoom ratio for x axis.</param>
    /// <param name="zoomY">The current zoom ratio for y axis.</param>
    /// <param name="offsetX">The current pan offset for x axis.</param>
    /// <param name="offsetY">The current pan offset for y axis.</param>
    /// <param name="panelWidth">The panel width used for the stretch calculation.</param>
    /// <param name="panelHeight">The panel height used for the stretch calculation.</param>
    /// <param name="elementWidth">The element width used for the stretch calculation.</param>
    /// <param name="elementHeight">The element height used for the stretch calculation.</param>
    public StretchModeChangedEventArgs(StretchMode stretchMode, StretchMode previousStretchMode,
        Matrix matrix, Matrix previousMatrix, double zoomX, double zoomY, double offsetX, double offsetY,
        double panelWidth, double panelHeight, double elementWidth, double elementHeight)
    {
        StretchMode = stretchMode;
        PreviousStretchMode = previousStretchMode;
        Matrix = matrix;
        PreviousMatrix = previousMatrix;
        ZoomX = zoomX;
        ZoomY = zoomY;
        OffsetX = offsetX;
        OffsetY = offsetY;
        PanelWidth = panelWidth;
        PanelHeight = panelHeight;
        ElementWidth = elementWidth;
        ElementHeight = elementHeight;
    }
}
