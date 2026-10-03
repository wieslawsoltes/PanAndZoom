// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;

namespace PanAndZoom.Core;

/// <summary>
/// A plain <see cref="IPanAndZoomSettings"/> implementation initialized with <see cref="ZoomBorderDefaults"/>.
/// </summary>
public class PanAndZoomSettings : IPanAndZoomSettings
{
    /// <inheritdoc/>
    public ButtonName PanButton { get; set; } = ZoomBorderDefaults.PanButton;

    /// <inheritdoc/>
    public double ZoomSpeed { get; set; } = ZoomBorderDefaults.ZoomSpeed;

    /// <inheritdoc/>
    public double PowerFactor { get; set; } = ZoomBorderDefaults.PowerFactor;

    /// <inheritdoc/>
    public double TransitionThreshold { get; set; } = ZoomBorderDefaults.TransitionThreshold;

    /// <inheritdoc/>
    public StretchMode Stretch { get; set; } = ZoomBorderDefaults.Stretch;

    /// <inheritdoc/>
    public bool EnableConstrains { get; set; } = ZoomBorderDefaults.EnableConstrains;

    /// <inheritdoc/>
    public double MinZoomX { get; set; } = ZoomBorderDefaults.MinZoomX;

    /// <inheritdoc/>
    public double MaxZoomX { get; set; } = ZoomBorderDefaults.MaxZoomX;

    /// <inheritdoc/>
    public double MinZoomY { get; set; } = ZoomBorderDefaults.MinZoomY;

    /// <inheritdoc/>
    public double MaxZoomY { get; set; } = ZoomBorderDefaults.MaxZoomY;

    /// <inheritdoc/>
    public double MinOffsetX { get; set; } = ZoomBorderDefaults.MinOffsetX;

    /// <inheritdoc/>
    public double MaxOffsetX { get; set; } = ZoomBorderDefaults.MaxOffsetX;

    /// <inheritdoc/>
    public double MinOffsetY { get; set; } = ZoomBorderDefaults.MinOffsetY;

    /// <inheritdoc/>
    public double MaxOffsetY { get; set; } = ZoomBorderDefaults.MaxOffsetY;

    /// <inheritdoc/>
    public bool EnablePan { get; set; } = ZoomBorderDefaults.EnablePan;

    /// <inheritdoc/>
    public bool EnableZoom { get; set; } = ZoomBorderDefaults.EnableZoom;

    /// <inheritdoc/>
    public bool EnableGestureZoom { get; set; } = ZoomBorderDefaults.EnableGestureZoom;

    /// <inheritdoc/>
    public bool EnableGestureRotation { get; set; } = ZoomBorderDefaults.EnableGestureRotation;

    /// <inheritdoc/>
    public bool EnableGestureTranslation { get; set; } = ZoomBorderDefaults.EnableGestureTranslation;

    /// <inheritdoc/>
    public bool EnableGestures { get; set; } = ZoomBorderDefaults.EnableGestures;

    /// <inheritdoc/>
    public TimeSpan AnimationDuration { get; set; } = ZoomBorderDefaults.AnimationDuration;

    /// <inheritdoc/>
    public bool EnableAnimations { get; set; } = ZoomBorderDefaults.EnableAnimations;

    /// <inheritdoc/>
    public bool EnableDoubleClickZoom { get; set; } = ZoomBorderDefaults.EnableDoubleClickZoom;

    /// <inheritdoc/>
    public DoubleClickZoomMode DoubleClickZoomMode { get; set; } = ZoomBorderDefaults.DoubleClickZoomMode;

    /// <inheritdoc/>
    public double DoubleClickZoomFactor { get; set; } = ZoomBorderDefaults.DoubleClickZoomFactor;

    /// <inheritdoc/>
    public ContentBoundsMode BoundsMode { get; set; } = ZoomBorderDefaults.BoundsMode;

    /// <inheritdoc/>
    public CoreThickness BoundsPadding { get; set; } = ZoomBorderDefaults.BoundsPadding;

    /// <inheritdoc/>
    public double MinimumVisibleContentPercentage { get; set; } = ZoomBorderDefaults.MinimumVisibleContentPercentage;

    /// <inheritdoc/>
    public ResizeBehaviorMode ResizeBehavior { get; set; } = ZoomBorderDefaults.ResizeBehavior;

    /// <inheritdoc/>
    public WheelBehaviorMode WheelBehavior { get; set; } = ZoomBorderDefaults.WheelBehavior;

    /// <inheritdoc/>
    public WheelBehaviorMode WheelWithCtrl { get; set; } = ZoomBorderDefaults.WheelWithCtrl;

    /// <inheritdoc/>
    public WheelBehaviorMode WheelWithShift { get; set; } = ZoomBorderDefaults.WheelWithShift;

    /// <inheritdoc/>
    public double WheelZoomSensitivity { get; set; } = ZoomBorderDefaults.WheelZoomSensitivity;

    /// <inheritdoc/>
    public double WheelPanSensitivity { get; set; } = ZoomBorderDefaults.WheelPanSensitivity;

    /// <inheritdoc/>
    public bool EnableKeyboardNavigation { get; set; } = ZoomBorderDefaults.EnableKeyboardNavigation;

    /// <inheritdoc/>
    public double KeyboardPanStep { get; set; } = ZoomBorderDefaults.KeyboardPanStep;

    /// <inheritdoc/>
    public double KeyboardZoomStep { get; set; } = ZoomBorderDefaults.KeyboardZoomStep;

    /// <inheritdoc/>
    public bool EnableViewHistory { get; set; } = ZoomBorderDefaults.EnableViewHistory;

    /// <inheritdoc/>
    public int ViewHistorySize { get; set; } = ZoomBorderDefaults.ViewHistorySize;

    /// <inheritdoc/>
    public CoreThickness CenterPadding { get; set; } = ZoomBorderDefaults.CenterPadding;

    /// <inheritdoc/>
    public bool EnableDiscreteZoomLevels { get; set; } = ZoomBorderDefaults.EnableDiscreteZoomLevels;

    /// <inheritdoc/>
    public double[]? DiscreteZoomLevels { get; set; } = ZoomBorderDefaults.DiscreteZoomLevels;

    /// <inheritdoc/>
    public bool AutoCalculateMinZoom { get; set; } = ZoomBorderDefaults.AutoCalculateMinZoom;

    /// <inheritdoc/>
    public bool AutoCalculateMaxZoom { get; set; } = ZoomBorderDefaults.AutoCalculateMaxZoom;

    /// <inheritdoc/>
    public double MaxZoomPixelSize { get; set; } = ZoomBorderDefaults.MaxZoomPixelSize;

    /// <inheritdoc/>
    public bool ShowZoomIndicator { get; set; } = ZoomBorderDefaults.ShowZoomIndicator;

    /// <inheritdoc/>
    public ZoomIndicatorPosition ZoomIndicatorPosition { get; set; } = ZoomBorderDefaults.ZoomIndicatorPosition;

    /// <inheritdoc/>
    public string ZoomIndicatorFormat { get; set; } = ZoomBorderDefaults.ZoomIndicatorFormat;

    /// <inheritdoc/>
    public TimeSpan ZoomIndicatorAutoHideDuration { get; set; } = ZoomBorderDefaults.ZoomIndicatorAutoHideDuration;

    /// <inheritdoc/>
    public bool EnableSnapToGrid { get; set; } = ZoomBorderDefaults.EnableSnapToGrid;

    /// <inheritdoc/>
    public double GridSize { get; set; } = ZoomBorderDefaults.GridSize;

    /// <inheritdoc/>
    public double Rotation { get; set; } = ZoomBorderDefaults.Rotation;

    /// <inheritdoc/>
    public double MinRotation { get; set; } = ZoomBorderDefaults.MinRotation;

    /// <inheritdoc/>
    public double MaxRotation { get; set; } = ZoomBorderDefaults.MaxRotation;

    /// <inheritdoc/>
    public bool EnableRotationSnapping { get; set; } = ZoomBorderDefaults.EnableRotationSnapping;

    /// <inheritdoc/>
    public double RotationSnapAngle { get; set; } = ZoomBorderDefaults.RotationSnapAngle;

    /// <inheritdoc/>
    public bool EnableSimultaneousPanZoom { get; set; } = ZoomBorderDefaults.EnableSimultaneousPanZoom;

    /// <inheritdoc/>
    public int MinimumTouchPoints { get; set; } = ZoomBorderDefaults.MinimumTouchPoints;

    /// <inheritdoc/>
    public int MaximumTouchPoints { get; set; } = ZoomBorderDefaults.MaximumTouchPoints;

    /// <inheritdoc/>
    public TimeSpan GestureRecognitionDelay { get; set; } = ZoomBorderDefaults.GestureRecognitionDelay;

    /// <inheritdoc/>
    public string ZoomLevelDescription { get; set; } = ZoomBorderDefaults.ZoomLevelDescription;

    /// <inheritdoc/>
    public string PanPositionDescription { get; set; } = ZoomBorderDefaults.PanPositionDescription;
}
