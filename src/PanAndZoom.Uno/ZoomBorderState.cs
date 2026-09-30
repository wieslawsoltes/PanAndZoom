// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;

namespace PanAndZoom;

/// <summary>
/// Represents a saved view state.
/// </summary>
public struct ViewState
{
    /// <summary>
    /// Gets or sets the transformation matrix.
    /// </summary>
    public Matrix Matrix { get; set; }

    /// <summary>
    /// Gets or sets the stretch mode.
    /// </summary>
    public StretchMode Stretch { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this state was saved.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Represents a named saved view.
/// </summary>
public struct SavedView
{
    /// <summary>
    /// Gets or sets the view name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the transformation matrix.
    /// </summary>
    public Matrix Matrix { get; set; }

    /// <summary>
    /// Gets or sets the stretch mode.
    /// </summary>
    public StretchMode Stretch { get; set; }

    /// <summary>
    /// Gets or sets optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this view was saved.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Represents the complete state of a ZoomBorder control for serialization.
/// </summary>
public class ZoomBorderState
{
    /// <summary>
    /// Gets or sets the transformation matrix.
    /// </summary>
    public Matrix Matrix { get; set; } = Matrix.Identity;

    /// <summary>
    /// Gets or sets the stretch mode.
    /// </summary>
    public StretchMode Stretch { get; set; }

    /// <summary>
    /// Gets or sets the zoom speed.
    /// </summary>
    public double ZoomSpeed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether pan is enabled.
    /// </summary>
    public bool EnablePan { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether zoom is enabled.
    /// </summary>
    public bool EnableZoom { get; set; }

    /// <summary>
    /// Gets or sets the rotation angle in degrees.
    /// </summary>
    public double Rotation { get; set; }

    /// <summary>
    /// Gets or sets the minimum zoom X value.
    /// </summary>
    public double MinZoomX { get; set; }

    /// <summary>
    /// Gets or sets the maximum zoom X value.
    /// </summary>
    public double MaxZoomX { get; set; }

    /// <summary>
    /// Gets or sets the minimum zoom Y value.
    /// </summary>
    public double MinZoomY { get; set; }

    /// <summary>
    /// Gets or sets the maximum zoom Y value.
    /// </summary>
    public double MaxZoomY { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether constrains are enabled.
    /// </summary>
    public bool EnableConstrains { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether animations are enabled.
    /// </summary>
    public bool EnableAnimations { get; set; }

    /// <summary>
    /// Gets or sets the animation duration.
    /// </summary>
    public TimeSpan AnimationDuration { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this state was captured.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
