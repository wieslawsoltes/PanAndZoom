// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;

namespace PanAndZoom.Core;

/// <summary>
/// Default values of the pan and zoom settings shared by all UI framework implementations.
/// </summary>
public static class ZoomBorderDefaults
{
    /// <summary>
    /// The default value of the pan input button.
    /// </summary>
    public const ButtonName PanButton = ButtonName.Middle;

    /// <summary>
    /// The default value of the zoom speed ratio.
    /// </summary>
    public const double ZoomSpeed = 1.2;

    /// <summary>
    /// The default value of the power factor used to transform the mouse wheel delta value.
    /// </summary>
    public const double PowerFactor = 1.0;

    /// <summary>
    /// The default value of the threshold below which zoom operations will skip all transitions.
    /// </summary>
    public const double TransitionThreshold = 0.5;

    /// <summary>
    /// The default value of the stretch mode.
    /// </summary>
    public const StretchMode Stretch = StretchMode.Uniform;

    /// <summary>
    /// The default value indicating whether zoom ratio and pan offset constrains are applied.
    /// </summary>
    public const bool EnableConstrains = true;

    /// <summary>
    /// The default value of the minimum zoom ratio for x axis.
    /// </summary>
    public const double MinZoomX = double.NegativeInfinity;

    /// <summary>
    /// The default value of the maximum zoom ratio for x axis.
    /// </summary>
    public const double MaxZoomX = double.PositiveInfinity;

    /// <summary>
    /// The default value of the minimum zoom ratio for y axis.
    /// </summary>
    public const double MinZoomY = double.NegativeInfinity;

    /// <summary>
    /// The default value of the maximum zoom ratio for y axis.
    /// </summary>
    public const double MaxZoomY = double.PositiveInfinity;

    /// <summary>
    /// The default value of the minimum offset for x axis.
    /// </summary>
    public const double MinOffsetX = double.NegativeInfinity;

    /// <summary>
    /// The default value of the maximum offset for x axis.
    /// </summary>
    public const double MaxOffsetX = double.PositiveInfinity;

    /// <summary>
    /// The default value of the minimum offset for y axis.
    /// </summary>
    public const double MinOffsetY = double.NegativeInfinity;

    /// <summary>
    /// The default value of the maximum offset for y axis.
    /// </summary>
    public const double MaxOffsetY = double.PositiveInfinity;

    /// <summary>
    /// The default value indicating whether pan input events are processed.
    /// </summary>
    public const bool EnablePan = true;

    /// <summary>
    /// The default value indicating whether zoom input events are processed.
    /// </summary>
    public const bool EnableZoom = true;

    /// <summary>
    /// The default value indicating whether the zoom gesture is enabled.
    /// </summary>
    public const bool EnableGestureZoom = true;

    /// <summary>
    /// The default value indicating whether the rotation gesture is enabled.
    /// </summary>
    public const bool EnableGestureRotation = true;

    /// <summary>
    /// The default value indicating whether the translation (pan) gesture is enabled.
    /// </summary>
    public const bool EnableGestureTranslation = true;

    /// <summary>
    /// The default value indicating whether gestures are enabled.
    /// </summary>
    public const bool EnableGestures = true;

    /// <summary>
    /// The default value of the duration of animations.
    /// </summary>
    public static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// The default value indicating whether animations are enabled.
    /// </summary>
    public const bool EnableAnimations = false;

    /// <summary>
    /// The default value indicating whether double-click zoom is enabled.
    /// </summary>
    public const bool EnableDoubleClickZoom = true;

    /// <summary>
    /// The default value of the double-click zoom behavior mode.
    /// </summary>
    public const DoubleClickZoomMode DoubleClickZoomMode = DoubleClickZoomMode.ZoomInOut;

    /// <summary>
    /// The default value of the zoom factor for double-click zoom operations.
    /// </summary>
    public const double DoubleClickZoomFactor = 2.0;

    /// <summary>
    /// The default value of the content bounds restriction mode.
    /// </summary>
    public const ContentBoundsMode BoundsMode = ContentBoundsMode.Unrestricted;

    /// <summary>
    /// The default value of the padding around content bounds.
    /// </summary>
    public static readonly CoreThickness BoundsPadding = new(0);

    /// <summary>
    /// The default value of the minimum percentage of content that must remain visible.
    /// </summary>
    public const double MinimumVisibleContentPercentage = 0.1;

    /// <summary>
    /// The default value of the behavior when the control is resized.
    /// </summary>
    public const ResizeBehaviorMode ResizeBehavior = ResizeBehaviorMode.None;

    /// <summary>
    /// The default value of the default mouse wheel behavior (no modifiers).
    /// </summary>
    public const WheelBehaviorMode WheelBehavior = WheelBehaviorMode.Zoom;

    /// <summary>
    /// The default value of the mouse wheel behavior when the Ctrl key is pressed.
    /// </summary>
    public const WheelBehaviorMode WheelWithCtrl = WheelBehaviorMode.Zoom;

    /// <summary>
    /// The default value of the mouse wheel behavior when the Shift key is pressed.
    /// </summary>
    public const WheelBehaviorMode WheelWithShift = WheelBehaviorMode.PanHorizontal;

    /// <summary>
    /// The default value of the zoom sensitivity for mouse wheel operations.
    /// </summary>
    public const double WheelZoomSensitivity = 1.0;

    /// <summary>
    /// The default value of the pan sensitivity for mouse wheel operations.
    /// </summary>
    public const double WheelPanSensitivity = 1.0;

    /// <summary>
    /// The default value indicating whether keyboard navigation is enabled.
    /// </summary>
    public const bool EnableKeyboardNavigation = true;

    /// <summary>
    /// The default value of the pan step distance for keyboard navigation.
    /// </summary>
    public const double KeyboardPanStep = 50.0;

    /// <summary>
    /// The default value of the zoom step factor for keyboard navigation.
    /// </summary>
    public const double KeyboardZoomStep = 1.1;

    /// <summary>
    /// The default value indicating whether view history is enabled.
    /// </summary>
    public const bool EnableViewHistory = true;

    /// <summary>
    /// The default value of the maximum number of view states stored in history.
    /// </summary>
    public const int ViewHistorySize = 50;

    /// <summary>
    /// The default value of the padding applied when centering on a point or element.
    /// </summary>
    public static readonly CoreThickness CenterPadding = new(0);

    /// <summary>
    /// The default value indicating whether discrete zoom levels are enabled.
    /// </summary>
    public const bool EnableDiscreteZoomLevels = false;

    /// <summary>
    /// The default discrete zoom levels.
    /// </summary>
    public static double[] DiscreteZoomLevels => new[] { 0.25, 0.5, 0.75, 1.0, 1.5, 2.0, 3.0, 4.0, 6.0, 8.0 };

    /// <summary>
    /// The default value indicating whether the minimum zoom is calculated automatically.
    /// </summary>
    public const bool AutoCalculateMinZoom = false;

    /// <summary>
    /// The default value indicating whether the maximum zoom is calculated automatically.
    /// </summary>
    public const bool AutoCalculateMaxZoom = false;

    /// <summary>
    /// The default value of the maximum zoom pixel size (1 content pixel = N screen pixels).
    /// </summary>
    public const double MaxZoomPixelSize = 4.0;

    /// <summary>
    /// The default value indicating whether the zoom indicator is shown after zoom operations.
    /// </summary>
    public const bool ShowZoomIndicator = false;

    /// <summary>
    /// The default value of the position of the zoom indicator.
    /// </summary>
    public const ZoomIndicatorPosition ZoomIndicatorPosition = ZoomIndicatorPosition.BottomRight;

    /// <summary>
    /// The default value of the format string for the zoom indicator.
    /// </summary>
    public const string ZoomIndicatorFormat = "{0:P0}";

    /// <summary>
    /// The default value of the auto-hide duration of the zoom indicator.
    /// </summary>
    public static readonly TimeSpan ZoomIndicatorAutoHideDuration = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The default value indicating whether snap to grid is enabled.
    /// </summary>
    public const bool EnableSnapToGrid = false;

    /// <summary>
    /// The default value of the grid size.
    /// </summary>
    public const double GridSize = 50.0;

    /// <summary>
    /// The default value of the current rotation angle in degrees.
    /// </summary>
    public const double Rotation = 0.0;

    /// <summary>
    /// The default value of the minimum rotation angle in degrees.
    /// </summary>
    public const double MinRotation = -180.0;

    /// <summary>
    /// The default value of the maximum rotation angle in degrees.
    /// </summary>
    public const double MaxRotation = 180.0;

    /// <summary>
    /// The default value indicating whether rotation snapping is enabled.
    /// </summary>
    public const bool EnableRotationSnapping = false;

    /// <summary>
    /// The default value of the rotation snap angle in degrees.
    /// </summary>
    public const double RotationSnapAngle = 45.0;

    /// <summary>
    /// The default value indicating whether simultaneous pan and zoom gestures are enabled.
    /// </summary>
    public const bool EnableSimultaneousPanZoom = true;

    /// <summary>
    /// The default value of the minimum number of touch points required to activate gestures.
    /// </summary>
    public const int MinimumTouchPoints = 1;

    /// <summary>
    /// The default value of the maximum number of touch points tracked for gestures.
    /// </summary>
    public const int MaximumTouchPoints = 2;

    /// <summary>
    /// The default value of the delay before a gesture is recognized.
    /// </summary>
    public static readonly TimeSpan GestureRecognitionDelay = TimeSpan.Zero;

    /// <summary>
    /// The default value of the zoom level description for accessibility.
    /// </summary>
    public const string ZoomLevelDescription = "";

    /// <summary>
    /// The default value of the pan position description for accessibility.
    /// </summary>
    public const string PanPositionDescription = "";

    /// <summary>
    /// The zoom ratio threshold used by <see cref="DoubleClickZoomMode.ZoomInOut"/> to decide between zoom in and reset.
    /// </summary>
    public const double DoubleClickZoomThreshold = 1.5;

    /// <summary>
    /// The default value indicating whether the grid is shown.
    /// </summary>
    public const bool ShowGrid = false;

    /// <summary>
    /// The default grid line thickness.
    /// </summary>
    public const double GridThickness = 1.0;

    /// <summary>
    /// The default grid opacity.
    /// </summary>
    public const double GridOpacity = 0.3;

    /// <summary>
    /// The default major grid line interval.
    /// </summary>
    public const int MajorGridInterval = 5;

    /// <summary>
    /// The default major grid line thickness.
    /// </summary>
    public const double MajorGridThickness = 2.0;

    /// <summary>
    /// The default value indicating whether the high contrast mode is used.
    /// </summary>
    public const bool UseHighContrastMode = false;
}
