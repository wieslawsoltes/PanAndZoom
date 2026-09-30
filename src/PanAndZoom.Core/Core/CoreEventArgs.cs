// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;

namespace PanAndZoom.Core;

/// <summary>
/// UI framework independent pan event data raised by <see cref="PanAndZoomEngine"/>.
/// </summary>
/// <param name="ZoomX">The current zoom ratio for x axis.</param>
/// <param name="ZoomY">The current zoom ratio for y axis.</param>
/// <param name="OffsetX">The current pan offset for x axis.</param>
/// <param name="OffsetY">The current pan offset for y axis.</param>
/// <param name="PreviousOffsetX">The previous pan offset for x axis.</param>
/// <param name="PreviousOffsetY">The previous pan offset for y axis.</param>
/// <param name="DeltaX">The pan delta for x axis.</param>
/// <param name="DeltaY">The pan delta for y axis.</param>
/// <param name="Matrix">The current transformation matrix.</param>
/// <param name="PreviousMatrix">The previous transformation matrix.</param>
public sealed record CorePanEventArgs(
    double ZoomX,
    double ZoomY,
    double OffsetX,
    double OffsetY,
    double PreviousOffsetX,
    double PreviousOffsetY,
    double DeltaX,
    double DeltaY,
    CoreMatrix Matrix,
    CoreMatrix PreviousMatrix);

/// <summary>
/// UI framework independent zoom event data raised by <see cref="PanAndZoomEngine"/>.
/// </summary>
/// <param name="ZoomX">The current zoom ratio for x axis.</param>
/// <param name="ZoomY">The current zoom ratio for y axis.</param>
/// <param name="PreviousZoomX">The previous zoom ratio for x axis.</param>
/// <param name="PreviousZoomY">The previous zoom ratio for y axis.</param>
/// <param name="ZoomDelta">The zoom delta or ratio.</param>
/// <param name="CenterX">The zoom center x coordinate.</param>
/// <param name="CenterY">The zoom center y coordinate.</param>
/// <param name="OffsetX">The current pan offset for x axis.</param>
/// <param name="OffsetY">The current pan offset for y axis.</param>
/// <param name="Matrix">The current transformation matrix.</param>
/// <param name="PreviousMatrix">The previous transformation matrix.</param>
public sealed record CoreZoomEventArgs(
    double ZoomX,
    double ZoomY,
    double PreviousZoomX,
    double PreviousZoomY,
    double ZoomDelta,
    double CenterX,
    double CenterY,
    double OffsetX,
    double OffsetY,
    CoreMatrix Matrix,
    CoreMatrix PreviousMatrix);

/// <summary>
/// UI framework independent matrix changed event data raised by <see cref="PanAndZoomEngine"/>.
/// </summary>
/// <param name="Matrix">The current transformation matrix.</param>
/// <param name="PreviousMatrix">The previous transformation matrix.</param>
/// <param name="ZoomX">The current zoom ratio for x axis.</param>
/// <param name="ZoomY">The current zoom ratio for y axis.</param>
/// <param name="OffsetX">The current pan offset for x axis.</param>
/// <param name="OffsetY">The current pan offset for y axis.</param>
/// <param name="PreviousZoomX">The previous zoom ratio for x axis.</param>
/// <param name="PreviousZoomY">The previous zoom ratio for y axis.</param>
/// <param name="PreviousOffsetX">The previous pan offset for x axis.</param>
/// <param name="PreviousOffsetY">The previous pan offset for y axis.</param>
/// <param name="Operation">The operation that caused the matrix change.</param>
public sealed record CoreMatrixChangedEventArgs(
    CoreMatrix Matrix,
    CoreMatrix PreviousMatrix,
    double ZoomX,
    double ZoomY,
    double OffsetX,
    double OffsetY,
    double PreviousZoomX,
    double PreviousZoomY,
    double PreviousOffsetX,
    double PreviousOffsetY,
    string Operation);

/// <summary>
/// UI framework independent stretch mode event data raised by <see cref="PanAndZoomEngine"/>.
/// </summary>
/// <param name="StretchMode">The current stretch mode.</param>
/// <param name="PreviousStretchMode">The previous stretch mode.</param>
/// <param name="Matrix">The current transformation matrix.</param>
/// <param name="PreviousMatrix">The previous transformation matrix.</param>
/// <param name="ZoomX">The current zoom ratio for x axis.</param>
/// <param name="ZoomY">The current zoom ratio for y axis.</param>
/// <param name="OffsetX">The current pan offset for x axis.</param>
/// <param name="OffsetY">The current pan offset for y axis.</param>
/// <param name="PanelWidth">The panel width.</param>
/// <param name="PanelHeight">The panel height.</param>
/// <param name="ElementWidth">The element width.</param>
/// <param name="ElementHeight">The element height.</param>
public sealed record CoreStretchModeChangedEventArgs(
    StretchMode StretchMode,
    StretchMode PreviousStretchMode,
    CoreMatrix Matrix,
    CoreMatrix PreviousMatrix,
    double ZoomX,
    double ZoomY,
    double OffsetX,
    double OffsetY,
    double PanelWidth,
    double PanelHeight,
    double ElementWidth,
    double ElementHeight);

/// <summary>
/// UI framework independent gesture event data raised by <see cref="PanAndZoomEngine"/>.
/// </summary>
/// <param name="GestureType">The gesture type (for example <c>Pinch</c> or <c>Scroll</c>).</param>
/// <param name="ZoomX">The current zoom ratio for x axis.</param>
/// <param name="ZoomY">The current zoom ratio for y axis.</param>
/// <param name="OffsetX">The current pan offset for x axis.</param>
/// <param name="OffsetY">The current pan offset for y axis.</param>
/// <param name="CenterX">The gesture center x coordinate.</param>
/// <param name="CenterY">The gesture center y coordinate.</param>
/// <param name="Delta">The gesture delta.</param>
/// <param name="Matrix">The current transformation matrix.</param>
/// <param name="PreviousMatrix">The previous transformation matrix.</param>
public sealed record CoreGestureEventArgs(
    string GestureType,
    double ZoomX,
    double ZoomY,
    double OffsetX,
    double OffsetY,
    double CenterX,
    double CenterY,
    double Delta,
    CoreMatrix Matrix,
    CoreMatrix PreviousMatrix);

/// <summary>
/// UI framework independent view history entry.
/// </summary>
/// <param name="Matrix">The transformation matrix.</param>
/// <param name="Stretch">The stretch mode.</param>
/// <param name="Timestamp">The time when the state was recorded.</param>
public readonly record struct CoreViewState(CoreMatrix Matrix, StretchMode Stretch, DateTime Timestamp);

/// <summary>
/// UI framework independent named saved view.
/// </summary>
/// <param name="Name">The view name.</param>
/// <param name="Matrix">The transformation matrix.</param>
/// <param name="Stretch">The stretch mode.</param>
/// <param name="Description">The optional description.</param>
/// <param name="Timestamp">The time when the view was saved.</param>
public readonly record struct CoreSavedView(string Name, CoreMatrix Matrix, StretchMode Stretch, string? Description, DateTime Timestamp);

/// <summary>
/// UI framework independent snapshot of the complete pan and zoom state.
/// </summary>
public sealed class CoreZoomBorderState
{
    /// <summary>
    /// Gets or sets the transformation matrix.
    /// </summary>
    public CoreMatrix Matrix { get; set; }

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
