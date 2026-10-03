// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;

namespace PanAndZoom.Core;

/// <summary>
/// Provides the pan and zoom behavior settings consumed by <see cref="PanAndZoomEngine"/>.
/// </summary>
/// <remarks>
/// UI framework controls implement this interface on top of their bindable properties so the
/// engine always reads the current value. <see cref="PanAndZoomSettings"/> is a plain implementation.
/// </remarks>
public interface IPanAndZoomSettings
{
    /// <summary>
    /// Gets the pan input button.
    /// </summary>
    ButtonName PanButton { get; }

    /// <summary>
    /// Gets or sets the zoom speed ratio.
    /// </summary>
    double ZoomSpeed { get; set; }

    /// <summary>
    /// Gets the power factor used to transform the mouse wheel delta value.
    /// </summary>
    double PowerFactor { get; }

    /// <summary>
    /// Gets the threshold below which zoom operations will skip all transitions.
    /// </summary>
    double TransitionThreshold { get; }

    /// <summary>
    /// Gets or sets the stretch mode.
    /// </summary>
    StretchMode Stretch { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether zoom ratio and pan offset constrains are applied.
    /// </summary>
    bool EnableConstrains { get; set; }

    /// <summary>
    /// Gets or sets the minimum zoom ratio for x axis.
    /// </summary>
    double MinZoomX { get; set; }

    /// <summary>
    /// Gets or sets the maximum zoom ratio for x axis.
    /// </summary>
    double MaxZoomX { get; set; }

    /// <summary>
    /// Gets or sets the minimum zoom ratio for y axis.
    /// </summary>
    double MinZoomY { get; set; }

    /// <summary>
    /// Gets or sets the maximum zoom ratio for y axis.
    /// </summary>
    double MaxZoomY { get; set; }

    /// <summary>
    /// Gets the minimum offset for x axis.
    /// </summary>
    double MinOffsetX { get; }

    /// <summary>
    /// Gets the maximum offset for x axis.
    /// </summary>
    double MaxOffsetX { get; }

    /// <summary>
    /// Gets the minimum offset for y axis.
    /// </summary>
    double MinOffsetY { get; }

    /// <summary>
    /// Gets the maximum offset for y axis.
    /// </summary>
    double MaxOffsetY { get; }

    /// <summary>
    /// Gets or sets a value indicating whether pan input events are processed.
    /// </summary>
    bool EnablePan { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether zoom input events are processed.
    /// </summary>
    bool EnableZoom { get; set; }

    /// <summary>
    /// Gets a value indicating whether the zoom gesture is enabled.
    /// </summary>
    bool EnableGestureZoom { get; }

    /// <summary>
    /// Gets a value indicating whether the rotation gesture is enabled.
    /// </summary>
    bool EnableGestureRotation { get; }

    /// <summary>
    /// Gets a value indicating whether the translation (pan) gesture is enabled.
    /// </summary>
    bool EnableGestureTranslation { get; }

    /// <summary>
    /// Gets a value indicating whether gestures are enabled.
    /// </summary>
    bool EnableGestures { get; }

    /// <summary>
    /// Gets or sets the duration of animations.
    /// </summary>
    TimeSpan AnimationDuration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether animations are enabled.
    /// </summary>
    bool EnableAnimations { get; set; }

    /// <summary>
    /// Gets a value indicating whether double-click zoom is enabled.
    /// </summary>
    bool EnableDoubleClickZoom { get; }

    /// <summary>
    /// Gets the double-click zoom behavior mode.
    /// </summary>
    DoubleClickZoomMode DoubleClickZoomMode { get; }

    /// <summary>
    /// Gets the zoom factor for double-click zoom operations.
    /// </summary>
    double DoubleClickZoomFactor { get; }

    /// <summary>
    /// Gets the content bounds restriction mode.
    /// </summary>
    ContentBoundsMode BoundsMode { get; }

    /// <summary>
    /// Gets the padding around content bounds.
    /// </summary>
    CoreThickness BoundsPadding { get; }

    /// <summary>
    /// Gets the minimum percentage of content that must remain visible.
    /// </summary>
    double MinimumVisibleContentPercentage { get; }

    /// <summary>
    /// Gets the behavior when the control is resized.
    /// </summary>
    ResizeBehaviorMode ResizeBehavior { get; }

    /// <summary>
    /// Gets the default mouse wheel behavior (no modifiers).
    /// </summary>
    WheelBehaviorMode WheelBehavior { get; }

    /// <summary>
    /// Gets the mouse wheel behavior when the Ctrl key is pressed.
    /// </summary>
    WheelBehaviorMode WheelWithCtrl { get; }

    /// <summary>
    /// Gets the mouse wheel behavior when the Shift key is pressed.
    /// </summary>
    WheelBehaviorMode WheelWithShift { get; }

    /// <summary>
    /// Gets the zoom sensitivity for mouse wheel operations.
    /// </summary>
    double WheelZoomSensitivity { get; }

    /// <summary>
    /// Gets the pan sensitivity for mouse wheel operations.
    /// </summary>
    double WheelPanSensitivity { get; }

    /// <summary>
    /// Gets a value indicating whether keyboard navigation is enabled.
    /// </summary>
    bool EnableKeyboardNavigation { get; }

    /// <summary>
    /// Gets the pan step distance for keyboard navigation.
    /// </summary>
    double KeyboardPanStep { get; }

    /// <summary>
    /// Gets the zoom step factor for keyboard navigation.
    /// </summary>
    double KeyboardZoomStep { get; }

    /// <summary>
    /// Gets a value indicating whether view history is enabled.
    /// </summary>
    bool EnableViewHistory { get; }

    /// <summary>
    /// Gets the maximum number of view states stored in history.
    /// </summary>
    int ViewHistorySize { get; }

    /// <summary>
    /// Gets the padding applied when centering on a point or element.
    /// </summary>
    CoreThickness CenterPadding { get; }

    /// <summary>
    /// Gets a value indicating whether discrete zoom levels are enabled.
    /// </summary>
    bool EnableDiscreteZoomLevels { get; }

    /// <summary>
    /// Gets the discrete zoom levels.
    /// </summary>
    double[]? DiscreteZoomLevels { get; }

    /// <summary>
    /// Gets a value indicating whether the minimum zoom is calculated automatically.
    /// </summary>
    bool AutoCalculateMinZoom { get; }

    /// <summary>
    /// Gets a value indicating whether the maximum zoom is calculated automatically.
    /// </summary>
    bool AutoCalculateMaxZoom { get; }

    /// <summary>
    /// Gets the maximum zoom pixel size (1 content pixel = N screen pixels).
    /// </summary>
    double MaxZoomPixelSize { get; }

    /// <summary>
    /// Gets a value indicating whether the zoom indicator is shown after zoom operations.
    /// </summary>
    bool ShowZoomIndicator { get; }

    /// <summary>
    /// Gets the position of the zoom indicator.
    /// </summary>
    ZoomIndicatorPosition ZoomIndicatorPosition { get; }

    /// <summary>
    /// Gets the format string for the zoom indicator.
    /// </summary>
    string ZoomIndicatorFormat { get; }

    /// <summary>
    /// Gets the auto-hide duration of the zoom indicator.
    /// </summary>
    TimeSpan ZoomIndicatorAutoHideDuration { get; }

    /// <summary>
    /// Gets a value indicating whether snap to grid is enabled.
    /// </summary>
    bool EnableSnapToGrid { get; }

    /// <summary>
    /// Gets the grid size.
    /// </summary>
    double GridSize { get; }

    /// <summary>
    /// Gets or sets the current rotation angle in degrees.
    /// </summary>
    double Rotation { get; set; }

    /// <summary>
    /// Gets the minimum rotation angle in degrees.
    /// </summary>
    double MinRotation { get; }

    /// <summary>
    /// Gets the maximum rotation angle in degrees.
    /// </summary>
    double MaxRotation { get; }

    /// <summary>
    /// Gets a value indicating whether rotation snapping is enabled.
    /// </summary>
    bool EnableRotationSnapping { get; }

    /// <summary>
    /// Gets the rotation snap angle in degrees.
    /// </summary>
    double RotationSnapAngle { get; }

    /// <summary>
    /// Gets a value indicating whether simultaneous pan and zoom gestures are enabled.
    /// </summary>
    bool EnableSimultaneousPanZoom { get; }

    /// <summary>
    /// Gets the minimum number of touch points required to activate gestures.
    /// </summary>
    int MinimumTouchPoints { get; }

    /// <summary>
    /// Gets the maximum number of touch points tracked for gestures.
    /// </summary>
    int MaximumTouchPoints { get; }

    /// <summary>
    /// Gets the delay before a gesture is recognized.
    /// </summary>
    TimeSpan GestureRecognitionDelay { get; }

    /// <summary>
    /// Gets or sets the zoom level description for accessibility.
    /// </summary>
    string ZoomLevelDescription { get; set; }

    /// <summary>
    /// Gets or sets the pan position description for accessibility.
    /// </summary>
    string PanPositionDescription { get; set; }
}
