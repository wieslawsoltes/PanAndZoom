/*
 * PanAndZoom A PanAndZoom control for Avalonia.
 * Copyright (C) 2023  Wiesław Šoltés
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Zoom changed event arguments.
/// </summary>
public class ZoomChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the zoom ratio for x axis.
    /// </summary>
    public double ZoomX { get; }

    /// <summary>
    /// Gets the zoom ratio for y axis.
    /// </summary>
    public double ZoomY { get; }

    /// <summary>
    /// Gets the pan offset for x axis.
    /// </summary>
    public double OffsetX { get; }

    /// <summary>
    /// Gets the pan offset for y axis.
    /// </summary>
    public double OffsetY { get;  }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoomChangedEventArgs"/> class.
    /// </summary>
    /// <param name="zoomX">The zoom ratio for y axis</param>
    /// <param name="zoomY">The zoom ratio for y axis</param>
    /// <param name="offsetX">The pan offset for x axis</param>
    /// <param name="offsetY">The pan offset for y axis</param>
    public ZoomChangedEventArgs(double zoomX, double zoomY, double offsetX, double offsetY)
    {
        ZoomX = zoomX;
        ZoomY = zoomY;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }
}
