// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace PanAndZoom;

/// <summary>
/// Pan and zoom control for Uno Platform (WinUI).
/// </summary>
public partial class ZoomBorder
{
    /// <summary>
    /// Identifies the <see cref="PanButton"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PanButtonProperty =
        DependencyProperty.Register(nameof(PanButton), typeof(ButtonName), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.PanButton));

    /// <summary>
    /// Identifies the <see cref="ZoomSpeed"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ZoomSpeedProperty =
        DependencyProperty.Register(nameof(ZoomSpeed), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.ZoomSpeed, OnArrangePropertyChanged));

    /// <summary>
    /// Identifies the <see cref="PowerFactor"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PowerFactorProperty =
        DependencyProperty.Register(nameof(PowerFactor), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.PowerFactor));

    /// <summary>
    /// Identifies the <see cref="TransitionThreshold"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TransitionThresholdProperty =
        DependencyProperty.Register(nameof(TransitionThreshold), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.TransitionThreshold));

    /// <summary>
    /// Identifies the <see cref="Stretch"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty StretchProperty =
        DependencyProperty.Register(nameof(Stretch), typeof(StretchMode), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.Stretch, OnStretchPropertyChanged));

    /// <summary>
    /// Identifies the <see cref="EnableConstrains"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EnableConstrainsProperty =
        DependencyProperty.Register(nameof(EnableConstrains), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.EnableConstrains, OnArrangePropertyChanged));

    /// <summary>
    /// Identifies the <see cref="MinZoomX"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MinZoomXProperty =
        DependencyProperty.Register(nameof(MinZoomX), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MinZoomX, OnArrangePropertyChanged));

    /// <summary>
    /// Identifies the <see cref="MaxZoomX"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MaxZoomXProperty =
        DependencyProperty.Register(nameof(MaxZoomX), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MaxZoomX, OnArrangePropertyChanged));

    /// <summary>
    /// Identifies the <see cref="MinZoomY"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MinZoomYProperty =
        DependencyProperty.Register(nameof(MinZoomY), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MinZoomY, OnArrangePropertyChanged));

    /// <summary>
    /// Identifies the <see cref="MaxZoomY"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MaxZoomYProperty =
        DependencyProperty.Register(nameof(MaxZoomY), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MaxZoomY, OnArrangePropertyChanged));

    /// <summary>
    /// Identifies the <see cref="MinOffsetX"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MinOffsetXProperty =
        DependencyProperty.Register(nameof(MinOffsetX), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MinOffsetX, OnArrangePropertyChanged));

    /// <summary>
    /// Identifies the <see cref="MaxOffsetX"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MaxOffsetXProperty =
        DependencyProperty.Register(nameof(MaxOffsetX), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MaxOffsetX, OnArrangePropertyChanged));

    /// <summary>
    /// Identifies the <see cref="MinOffsetY"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MinOffsetYProperty =
        DependencyProperty.Register(nameof(MinOffsetY), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MinOffsetY, OnArrangePropertyChanged));

    /// <summary>
    /// Identifies the <see cref="MaxOffsetY"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MaxOffsetYProperty =
        DependencyProperty.Register(nameof(MaxOffsetY), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MaxOffsetY, OnArrangePropertyChanged));

    /// <summary>
    /// Identifies the <see cref="EnablePan"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EnablePanProperty =
        DependencyProperty.Register(nameof(EnablePan), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.EnablePan, OnArrangePropertyChanged));

    /// <summary>
    /// Identifies the <see cref="EnableZoom"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EnableZoomProperty =
        DependencyProperty.Register(nameof(EnableZoom), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.EnableZoom, OnArrangePropertyChanged));

    /// <summary>
    /// Identifies the <see cref="EnableGestureZoom"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EnableGestureZoomProperty =
        DependencyProperty.Register(nameof(EnableGestureZoom), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.EnableGestureZoom, OnArrangePropertyChanged));

    /// <summary>
    /// Identifies the <see cref="EnableGestureRotation"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EnableGestureRotationProperty =
        DependencyProperty.Register(nameof(EnableGestureRotation), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.EnableGestureRotation, OnArrangePropertyChanged));

    /// <summary>
    /// Identifies the <see cref="EnableGestureTranslation"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EnableGestureTranslationProperty =
        DependencyProperty.Register(nameof(EnableGestureTranslation), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.EnableGestureTranslation, OnArrangePropertyChanged));

    /// <summary>
    /// Identifies the <see cref="EnableGestures"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EnableGesturesProperty =
        DependencyProperty.Register(nameof(EnableGestures), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.EnableGestures, OnGesturesPropertyChanged));

    /// <summary>
    /// Identifies the <see cref="AnimationDuration"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty AnimationDurationProperty =
        DependencyProperty.Register(nameof(AnimationDuration), typeof(TimeSpan), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.AnimationDuration));

    /// <summary>
    /// Identifies the <see cref="EnableAnimations"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EnableAnimationsProperty =
        DependencyProperty.Register(nameof(EnableAnimations), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.EnableAnimations));

    /// <summary>
    /// Identifies the <see cref="EnableDoubleClickZoom"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EnableDoubleClickZoomProperty =
        DependencyProperty.Register(nameof(EnableDoubleClickZoom), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.EnableDoubleClickZoom));

    /// <summary>
    /// Identifies the <see cref="DoubleClickZoomMode"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty DoubleClickZoomModeProperty =
        DependencyProperty.Register(nameof(DoubleClickZoomMode), typeof(DoubleClickZoomMode), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.DoubleClickZoomMode));

    /// <summary>
    /// Identifies the <see cref="DoubleClickZoomFactor"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty DoubleClickZoomFactorProperty =
        DependencyProperty.Register(nameof(DoubleClickZoomFactor), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.DoubleClickZoomFactor));

    /// <summary>
    /// Identifies the <see cref="BoundsMode"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BoundsModeProperty =
        DependencyProperty.Register(nameof(BoundsMode), typeof(ContentBoundsMode), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.BoundsMode, OnBoundsPropertyChanged));

    /// <summary>
    /// Identifies the <see cref="BoundsPadding"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BoundsPaddingProperty =
        DependencyProperty.Register(nameof(BoundsPadding), typeof(Thickness), typeof(ZoomBorder), new PropertyMetadata(new Thickness(0), OnBoundsPropertyChanged));

    /// <summary>
    /// Identifies the <see cref="MinimumVisibleContentPercentage"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MinimumVisibleContentPercentageProperty =
        DependencyProperty.Register(nameof(MinimumVisibleContentPercentage), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MinimumVisibleContentPercentage, OnBoundsPropertyChanged));

    /// <summary>
    /// Identifies the <see cref="ResizeBehavior"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ResizeBehaviorProperty =
        DependencyProperty.Register(nameof(ResizeBehavior), typeof(ResizeBehaviorMode), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.ResizeBehavior));

    /// <summary>
    /// Identifies the <see cref="WheelBehavior"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty WheelBehaviorProperty =
        DependencyProperty.Register(nameof(WheelBehavior), typeof(WheelBehaviorMode), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.WheelBehavior));

    /// <summary>
    /// Identifies the <see cref="WheelWithCtrl"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty WheelWithCtrlProperty =
        DependencyProperty.Register(nameof(WheelWithCtrl), typeof(WheelBehaviorMode), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.WheelWithCtrl));

    /// <summary>
    /// Identifies the <see cref="WheelWithShift"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty WheelWithShiftProperty =
        DependencyProperty.Register(nameof(WheelWithShift), typeof(WheelBehaviorMode), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.WheelWithShift));

    /// <summary>
    /// Identifies the <see cref="WheelZoomSensitivity"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty WheelZoomSensitivityProperty =
        DependencyProperty.Register(nameof(WheelZoomSensitivity), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.WheelZoomSensitivity));

    /// <summary>
    /// Identifies the <see cref="WheelPanSensitivity"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty WheelPanSensitivityProperty =
        DependencyProperty.Register(nameof(WheelPanSensitivity), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.WheelPanSensitivity));

    /// <summary>
    /// Identifies the <see cref="EnableKeyboardNavigation"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EnableKeyboardNavigationProperty =
        DependencyProperty.Register(nameof(EnableKeyboardNavigation), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.EnableKeyboardNavigation));

    /// <summary>
    /// Identifies the <see cref="KeyboardPanStep"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty KeyboardPanStepProperty =
        DependencyProperty.Register(nameof(KeyboardPanStep), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.KeyboardPanStep));

    /// <summary>
    /// Identifies the <see cref="KeyboardZoomStep"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty KeyboardZoomStepProperty =
        DependencyProperty.Register(nameof(KeyboardZoomStep), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.KeyboardZoomStep));

    /// <summary>
    /// Identifies the <see cref="EnableViewHistory"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EnableViewHistoryProperty =
        DependencyProperty.Register(nameof(EnableViewHistory), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.EnableViewHistory));

    /// <summary>
    /// Identifies the <see cref="ViewHistorySize"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ViewHistorySizeProperty =
        DependencyProperty.Register(nameof(ViewHistorySize), typeof(int), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.ViewHistorySize));

    /// <summary>
    /// Identifies the <see cref="CenterPadding"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CenterPaddingProperty =
        DependencyProperty.Register(nameof(CenterPadding), typeof(Thickness), typeof(ZoomBorder), new PropertyMetadata(new Thickness(0)));

    /// <summary>
    /// Identifies the <see cref="EnableDiscreteZoomLevels"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EnableDiscreteZoomLevelsProperty =
        DependencyProperty.Register(nameof(EnableDiscreteZoomLevels), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.EnableDiscreteZoomLevels));

    /// <summary>
    /// Identifies the <see cref="DiscreteZoomLevels"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty DiscreteZoomLevelsProperty =
        DependencyProperty.Register(nameof(DiscreteZoomLevels), typeof(double[]), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.DiscreteZoomLevels));

    /// <summary>
    /// Identifies the <see cref="AutoCalculateMinZoom"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty AutoCalculateMinZoomProperty =
        DependencyProperty.Register(nameof(AutoCalculateMinZoom), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.AutoCalculateMinZoom));

    /// <summary>
    /// Identifies the <see cref="AutoCalculateMaxZoom"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty AutoCalculateMaxZoomProperty =
        DependencyProperty.Register(nameof(AutoCalculateMaxZoom), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.AutoCalculateMaxZoom));

    /// <summary>
    /// Identifies the <see cref="MaxZoomPixelSize"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MaxZoomPixelSizeProperty =
        DependencyProperty.Register(nameof(MaxZoomPixelSize), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MaxZoomPixelSize));

    /// <summary>
    /// Identifies the <see cref="ShowZoomIndicator"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ShowZoomIndicatorProperty =
        DependencyProperty.Register(nameof(ShowZoomIndicator), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.ShowZoomIndicator));

    /// <summary>
    /// Identifies the <see cref="ZoomIndicatorPosition"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ZoomIndicatorPositionProperty =
        DependencyProperty.Register(nameof(ZoomIndicatorPosition), typeof(ZoomIndicatorPosition), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.ZoomIndicatorPosition));

    /// <summary>
    /// Identifies the <see cref="ZoomIndicatorFormat"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ZoomIndicatorFormatProperty =
        DependencyProperty.Register(nameof(ZoomIndicatorFormat), typeof(string), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.ZoomIndicatorFormat));

    /// <summary>
    /// Identifies the <see cref="ZoomIndicatorAutoHideDuration"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ZoomIndicatorAutoHideDurationProperty =
        DependencyProperty.Register(nameof(ZoomIndicatorAutoHideDuration), typeof(TimeSpan), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.ZoomIndicatorAutoHideDuration));

    /// <summary>
    /// Identifies the <see cref="ShowGrid"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ShowGridProperty =
        DependencyProperty.Register(nameof(ShowGrid), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.ShowGrid));

    /// <summary>
    /// Identifies the <see cref="EnableSnapToGrid"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EnableSnapToGridProperty =
        DependencyProperty.Register(nameof(EnableSnapToGrid), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.EnableSnapToGrid));

    /// <summary>
    /// Identifies the <see cref="GridSize"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty GridSizeProperty =
        DependencyProperty.Register(nameof(GridSize), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.GridSize));

    /// <summary>
    /// Identifies the <see cref="GridBrush"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty GridBrushProperty =
        DependencyProperty.Register(nameof(GridBrush), typeof(Brush), typeof(ZoomBorder), new PropertyMetadata(null));

    /// <summary>
    /// Identifies the <see cref="GridThickness"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty GridThicknessProperty =
        DependencyProperty.Register(nameof(GridThickness), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.GridThickness));

    /// <summary>
    /// Identifies the <see cref="GridOpacity"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty GridOpacityProperty =
        DependencyProperty.Register(nameof(GridOpacity), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.GridOpacity));

    /// <summary>
    /// Identifies the <see cref="MajorGridInterval"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MajorGridIntervalProperty =
        DependencyProperty.Register(nameof(MajorGridInterval), typeof(int), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MajorGridInterval));

    /// <summary>
    /// Identifies the <see cref="MajorGridBrush"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MajorGridBrushProperty =
        DependencyProperty.Register(nameof(MajorGridBrush), typeof(Brush), typeof(ZoomBorder), new PropertyMetadata(null));

    /// <summary>
    /// Identifies the <see cref="MajorGridThickness"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MajorGridThicknessProperty =
        DependencyProperty.Register(nameof(MajorGridThickness), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MajorGridThickness));

    /// <summary>
    /// Identifies the <see cref="Rotation"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty RotationProperty =
        DependencyProperty.Register(nameof(Rotation), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.Rotation));

    /// <summary>
    /// Identifies the <see cref="MinRotation"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MinRotationProperty =
        DependencyProperty.Register(nameof(MinRotation), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MinRotation));

    /// <summary>
    /// Identifies the <see cref="MaxRotation"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MaxRotationProperty =
        DependencyProperty.Register(nameof(MaxRotation), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MaxRotation));

    /// <summary>
    /// Identifies the <see cref="EnableRotationSnapping"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EnableRotationSnappingProperty =
        DependencyProperty.Register(nameof(EnableRotationSnapping), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.EnableRotationSnapping));

    /// <summary>
    /// Identifies the <see cref="RotationSnapAngle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty RotationSnapAngleProperty =
        DependencyProperty.Register(nameof(RotationSnapAngle), typeof(double), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.RotationSnapAngle));

    /// <summary>
    /// Identifies the <see cref="EnableSimultaneousPanZoom"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EnableSimultaneousPanZoomProperty =
        DependencyProperty.Register(nameof(EnableSimultaneousPanZoom), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.EnableSimultaneousPanZoom));

    /// <summary>
    /// Identifies the <see cref="MinimumTouchPoints"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MinimumTouchPointsProperty =
        DependencyProperty.Register(nameof(MinimumTouchPoints), typeof(int), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MinimumTouchPoints));

    /// <summary>
    /// Identifies the <see cref="MaximumTouchPoints"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MaximumTouchPointsProperty =
        DependencyProperty.Register(nameof(MaximumTouchPoints), typeof(int), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.MaximumTouchPoints));

    /// <summary>
    /// Identifies the <see cref="GestureRecognitionDelay"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty GestureRecognitionDelayProperty =
        DependencyProperty.Register(nameof(GestureRecognitionDelay), typeof(TimeSpan), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.GestureRecognitionDelay));

    /// <summary>
    /// Identifies the <see cref="ZoomLevelDescription"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ZoomLevelDescriptionProperty =
        DependencyProperty.Register(nameof(ZoomLevelDescription), typeof(string), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.ZoomLevelDescription));

    /// <summary>
    /// Identifies the <see cref="PanPositionDescription"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PanPositionDescriptionProperty =
        DependencyProperty.Register(nameof(PanPositionDescription), typeof(string), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.PanPositionDescription));

    /// <summary>
    /// Identifies the <see cref="UseHighContrastMode"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty UseHighContrastModeProperty =
        DependencyProperty.Register(nameof(UseHighContrastMode), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(ZoomBorderDefaults.UseHighContrastMode));

    /// <summary>
    /// Gets or sets pan input button.
    /// </summary>
    public ButtonName PanButton
    {
        get => (ButtonName)GetValue(PanButtonProperty);
        set => SetValue(PanButtonProperty, value);
    }

    /// <summary>
    /// Gets or sets zoom speed ratio.
    /// </summary>
    public double ZoomSpeed
    {
        get => (double)GetValue(ZoomSpeedProperty);
        set => SetValue(ZoomSpeedProperty, value);
    }

    /// <summary>
    /// Gets or sets the power factor used to transform the mouse wheel delta value.
    /// </summary>
    public double PowerFactor
    {
        get => (double)GetValue(PowerFactorProperty);
        set => SetValue(PowerFactorProperty, value);
    }

    /// <summary>
    /// Gets or sets the threshold below which zoom operations will skip all transitions.
    /// </summary>
    public double TransitionThreshold
    {
        get => (double)GetValue(TransitionThresholdProperty);
        set => SetValue(TransitionThresholdProperty, value);
    }

    /// <summary>
    /// Gets or sets stretch mode.
    /// </summary>
    public StretchMode Stretch
    {
        get => (StretchMode)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether zoom ratio and pan offset constrains are applied.
    /// </summary>
    public bool EnableConstrains
    {
        get => (bool)GetValue(EnableConstrainsProperty);
        set => SetValue(EnableConstrainsProperty, value);
    }

    /// <summary>
    /// Gets or sets minimum zoom ratio for x axis.
    /// </summary>
    public double MinZoomX
    {
        get => (double)GetValue(MinZoomXProperty);
        set => SetValue(MinZoomXProperty, value);
    }

    /// <summary>
    /// Gets or sets maximum zoom ratio for x axis.
    /// </summary>
    public double MaxZoomX
    {
        get => (double)GetValue(MaxZoomXProperty);
        set => SetValue(MaxZoomXProperty, value);
    }

    /// <summary>
    /// Gets or sets minimum zoom ratio for y axis.
    /// </summary>
    public double MinZoomY
    {
        get => (double)GetValue(MinZoomYProperty);
        set => SetValue(MinZoomYProperty, value);
    }

    /// <summary>
    /// Gets or sets maximum zoom ratio for y axis.
    /// </summary>
    public double MaxZoomY
    {
        get => (double)GetValue(MaxZoomYProperty);
        set => SetValue(MaxZoomYProperty, value);
    }

    /// <summary>
    /// Gets or sets minimum offset for x axis.
    /// </summary>
    public double MinOffsetX
    {
        get => (double)GetValue(MinOffsetXProperty);
        set => SetValue(MinOffsetXProperty, value);
    }

    /// <summary>
    /// Gets or sets maximum offset for x axis.
    /// </summary>
    public double MaxOffsetX
    {
        get => (double)GetValue(MaxOffsetXProperty);
        set => SetValue(MaxOffsetXProperty, value);
    }

    /// <summary>
    /// Gets or sets minimum offset for y axis.
    /// </summary>
    public double MinOffsetY
    {
        get => (double)GetValue(MinOffsetYProperty);
        set => SetValue(MinOffsetYProperty, value);
    }

    /// <summary>
    /// Gets or sets maximum offset for y axis.
    /// </summary>
    public double MaxOffsetY
    {
        get => (double)GetValue(MaxOffsetYProperty);
        set => SetValue(MaxOffsetYProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether pan input events are processed.
    /// </summary>
    public bool EnablePan
    {
        get => (bool)GetValue(EnablePanProperty);
        set => SetValue(EnablePanProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether input zoom events are processed.
    /// </summary>
    public bool EnableZoom
    {
        get => (bool)GetValue(EnableZoomProperty);
        set => SetValue(EnableZoomProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether zoom gesture is enabled.
    /// </summary>
    public bool EnableGestureZoom
    {
        get => (bool)GetValue(EnableGestureZoomProperty);
        set => SetValue(EnableGestureZoomProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether rotation gesture is enabled.
    /// </summary>
    public bool EnableGestureRotation
    {
        get => (bool)GetValue(EnableGestureRotationProperty);
        set => SetValue(EnableGestureRotationProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether translation (pan) gesture is enabled.
    /// </summary>
    public bool EnableGestureTranslation
    {
        get => (bool)GetValue(EnableGestureTranslationProperty);
        set => SetValue(EnableGestureTranslationProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether gestures are enabled.
    /// </summary>
    public bool EnableGestures
    {
        get => (bool)GetValue(EnableGesturesProperty);
        set => SetValue(EnableGesturesProperty, value);
    }

    /// <summary>
    /// Gets or sets the duration of animations for zoom and pan operations.
    /// </summary>
    public TimeSpan AnimationDuration
    {
        get => (TimeSpan)GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether animations are enabled for zoom and pan operations.
    /// </summary>
    public bool EnableAnimations
    {
        get => (bool)GetValue(EnableAnimationsProperty);
        set => SetValue(EnableAnimationsProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether double-click zoom is enabled.
    /// </summary>
    public bool EnableDoubleClickZoom
    {
        get => (bool)GetValue(EnableDoubleClickZoomProperty);
        set => SetValue(EnableDoubleClickZoomProperty, value);
    }

    /// <summary>
    /// Gets or sets the double-click zoom behavior mode.
    /// </summary>
    public DoubleClickZoomMode DoubleClickZoomMode
    {
        get => (DoubleClickZoomMode)GetValue(DoubleClickZoomModeProperty);
        set => SetValue(DoubleClickZoomModeProperty, value);
    }

    /// <summary>
    /// Gets or sets the zoom factor for double-click zoom operations.
    /// </summary>
    public double DoubleClickZoomFactor
    {
        get => (double)GetValue(DoubleClickZoomFactorProperty);
        set => SetValue(DoubleClickZoomFactorProperty, value);
    }

    /// <summary>
    /// Gets or sets the content bounds restriction mode.
    /// </summary>
    public ContentBoundsMode BoundsMode
    {
        get => (ContentBoundsMode)GetValue(BoundsModeProperty);
        set => SetValue(BoundsModeProperty, value);
    }

    /// <summary>
    /// Gets or sets the padding around content bounds.
    /// </summary>
    public Thickness BoundsPadding
    {
        get => (Thickness)GetValue(BoundsPaddingProperty);
        set => SetValue(BoundsPaddingProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum percentage of content that must remain visible.
    /// </summary>
    public double MinimumVisibleContentPercentage
    {
        get => (double)GetValue(MinimumVisibleContentPercentageProperty);
        set => SetValue(MinimumVisibleContentPercentageProperty, value);
    }

    /// <summary>
    /// Gets or sets the behavior when the control is resized.
    /// </summary>
    public ResizeBehaviorMode ResizeBehavior
    {
        get => (ResizeBehaviorMode)GetValue(ResizeBehaviorProperty);
        set => SetValue(ResizeBehaviorProperty, value);
    }

    /// <summary>
    /// Gets or sets the default mouse wheel behavior (no modifiers).
    /// </summary>
    public WheelBehaviorMode WheelBehavior
    {
        get => (WheelBehaviorMode)GetValue(WheelBehaviorProperty);
        set => SetValue(WheelBehaviorProperty, value);
    }

    /// <summary>
    /// Gets or sets the mouse wheel behavior when Ctrl key is pressed.
    /// </summary>
    public WheelBehaviorMode WheelWithCtrl
    {
        get => (WheelBehaviorMode)GetValue(WheelWithCtrlProperty);
        set => SetValue(WheelWithCtrlProperty, value);
    }

    /// <summary>
    /// Gets or sets the mouse wheel behavior when Shift key is pressed.
    /// </summary>
    public WheelBehaviorMode WheelWithShift
    {
        get => (WheelBehaviorMode)GetValue(WheelWithShiftProperty);
        set => SetValue(WheelWithShiftProperty, value);
    }

    /// <summary>
    /// Gets or sets the zoom sensitivity for mouse wheel operations.
    /// </summary>
    public double WheelZoomSensitivity
    {
        get => (double)GetValue(WheelZoomSensitivityProperty);
        set => SetValue(WheelZoomSensitivityProperty, value);
    }

    /// <summary>
    /// Gets or sets the pan sensitivity for mouse wheel operations.
    /// </summary>
    public double WheelPanSensitivity
    {
        get => (double)GetValue(WheelPanSensitivityProperty);
        set => SetValue(WheelPanSensitivityProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether keyboard navigation is enabled.
    /// </summary>
    public bool EnableKeyboardNavigation
    {
        get => (bool)GetValue(EnableKeyboardNavigationProperty);
        set => SetValue(EnableKeyboardNavigationProperty, value);
    }

    /// <summary>
    /// Gets or sets the pan step distance for keyboard navigation.
    /// </summary>
    public double KeyboardPanStep
    {
        get => (double)GetValue(KeyboardPanStepProperty);
        set => SetValue(KeyboardPanStepProperty, value);
    }

    /// <summary>
    /// Gets or sets the zoom step factor for keyboard navigation.
    /// </summary>
    public double KeyboardZoomStep
    {
        get => (double)GetValue(KeyboardZoomStepProperty);
        set => SetValue(KeyboardZoomStepProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether view history is enabled.
    /// </summary>
    public bool EnableViewHistory
    {
        get => (bool)GetValue(EnableViewHistoryProperty);
        set => SetValue(EnableViewHistoryProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum number of view states to store in history.
    /// </summary>
    public int ViewHistorySize
    {
        get => (int)GetValue(ViewHistorySizeProperty);
        set => SetValue(ViewHistorySizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the padding to apply when centering on a point or element.
    /// </summary>
    public Thickness CenterPadding
    {
        get => (Thickness)GetValue(CenterPaddingProperty);
        set => SetValue(CenterPaddingProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether discrete zoom levels are enabled.
    /// </summary>
    public bool EnableDiscreteZoomLevels
    {
        get => (bool)GetValue(EnableDiscreteZoomLevelsProperty);
        set => SetValue(EnableDiscreteZoomLevelsProperty, value);
    }

    /// <summary>
    /// Gets or sets the array of discrete zoom levels.
    /// </summary>
    public double[]? DiscreteZoomLevels
    {
        get => (double[]?)GetValue(DiscreteZoomLevelsProperty);
        set => SetValue(DiscreteZoomLevelsProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically calculate minimum zoom.
    /// </summary>
    public bool AutoCalculateMinZoom
    {
        get => (bool)GetValue(AutoCalculateMinZoomProperty);
        set => SetValue(AutoCalculateMinZoomProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically calculate maximum zoom.
    /// </summary>
    public bool AutoCalculateMaxZoom
    {
        get => (bool)GetValue(AutoCalculateMaxZoomProperty);
        set => SetValue(AutoCalculateMaxZoomProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum zoom pixel size (1 content pixel = N screen pixels).
    /// </summary>
    public double MaxZoomPixelSize
    {
        get => (double)GetValue(MaxZoomPixelSizeProperty);
        set => SetValue(MaxZoomPixelSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show the zoom indicator.
    /// </summary>
    public bool ShowZoomIndicator
    {
        get => (bool)GetValue(ShowZoomIndicatorProperty);
        set => SetValue(ShowZoomIndicatorProperty, value);
    }

    /// <summary>
    /// Gets or sets the position of the zoom indicator.
    /// </summary>
    public ZoomIndicatorPosition ZoomIndicatorPosition
    {
        get => (ZoomIndicatorPosition)GetValue(ZoomIndicatorPositionProperty);
        set => SetValue(ZoomIndicatorPositionProperty, value);
    }

    /// <summary>
    /// Gets or sets the format string for the zoom indicator.
    /// </summary>
    public string ZoomIndicatorFormat
    {
        get => (string)GetValue(ZoomIndicatorFormatProperty);
        set => SetValue(ZoomIndicatorFormatProperty, value);
    }

    /// <summary>
    /// Gets or sets the auto-hide duration for the zoom indicator.
    /// </summary>
    public TimeSpan ZoomIndicatorAutoHideDuration
    {
        get => (TimeSpan)GetValue(ZoomIndicatorAutoHideDurationProperty);
        set => SetValue(ZoomIndicatorAutoHideDurationProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show the grid.
    /// </summary>
    public bool ShowGrid
    {
        get => (bool)GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to enable snap to grid.
    /// </summary>
    public bool EnableSnapToGrid
    {
        get => (bool)GetValue(EnableSnapToGridProperty);
        set => SetValue(EnableSnapToGridProperty, value);
    }

    /// <summary>
    /// Gets or sets the grid size.
    /// </summary>
    public double GridSize
    {
        get => (double)GetValue(GridSizeProperty);
        set => SetValue(GridSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the grid brush.
    /// </summary>
    public Brush? GridBrush
    {
        get => (Brush?)GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the grid thickness.
    /// </summary>
    public double GridThickness
    {
        get => (double)GetValue(GridThicknessProperty);
        set => SetValue(GridThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets the grid opacity.
    /// </summary>
    public double GridOpacity
    {
        get => (double)GetValue(GridOpacityProperty);
        set => SetValue(GridOpacityProperty, value);
    }

    /// <summary>
    /// Gets or sets the major grid interval.
    /// </summary>
    public int MajorGridInterval
    {
        get => (int)GetValue(MajorGridIntervalProperty);
        set => SetValue(MajorGridIntervalProperty, value);
    }

    /// <summary>
    /// Gets or sets the major grid brush.
    /// </summary>
    public Brush? MajorGridBrush
    {
        get => (Brush?)GetValue(MajorGridBrushProperty);
        set => SetValue(MajorGridBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the major grid thickness.
    /// </summary>
    public double MajorGridThickness
    {
        get => (double)GetValue(MajorGridThicknessProperty);
        set => SetValue(MajorGridThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets the current content rotation angle in degrees.
    /// </summary>
    /// <remarks>
    /// Hides <see cref="UIElement.Rotation"/> (the composition rotation of the control itself) to keep the
    /// API consistent with the Avalonia <c>ZoomBorder</c>. The value rotates the child content around its center.
    /// </remarks>
    public new double Rotation
    {
        get => (double)GetValue(RotationProperty);
        set => SetValue(RotationProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum rotation angle in degrees.
    /// </summary>
    public double MinRotation
    {
        get => (double)GetValue(MinRotationProperty);
        set => SetValue(MinRotationProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum rotation angle in degrees.
    /// </summary>
    public double MaxRotation
    {
        get => (double)GetValue(MaxRotationProperty);
        set => SetValue(MaxRotationProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether rotation snapping is enabled.
    /// </summary>
    public bool EnableRotationSnapping
    {
        get => (bool)GetValue(EnableRotationSnappingProperty);
        set => SetValue(EnableRotationSnappingProperty, value);
    }

    /// <summary>
    /// Gets or sets the rotation snap angle in degrees.
    /// </summary>
    public double RotationSnapAngle
    {
        get => (double)GetValue(RotationSnapAngleProperty);
        set => SetValue(RotationSnapAngleProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether simultaneous pan and zoom is enabled.
    /// When true, allows pan and zoom gestures to occur at the same time.
    /// </summary>
    public bool EnableSimultaneousPanZoom
    {
        get => (bool)GetValue(EnableSimultaneousPanZoomProperty);
        set => SetValue(EnableSimultaneousPanZoomProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum number of touch points required to activate gestures.
    /// </summary>
    public int MinimumTouchPoints
    {
        get => (int)GetValue(MinimumTouchPointsProperty);
        set => SetValue(MinimumTouchPointsProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum number of touch points that will be tracked.
    /// </summary>
    public int MaximumTouchPoints
    {
        get => (int)GetValue(MaximumTouchPointsProperty);
        set => SetValue(MaximumTouchPointsProperty, value);
    }

    /// <summary>
    /// Gets or sets the gesture recognition delay before recognizing a gesture.
    /// </summary>
    public TimeSpan GestureRecognitionDelay
    {
        get => (TimeSpan)GetValue(GestureRecognitionDelayProperty);
        set => SetValue(GestureRecognitionDelayProperty, value);
    }

    /// <summary>
    /// Gets or sets the zoom level description for accessibility.
    /// </summary>
    public string ZoomLevelDescription
    {
        get => (string)GetValue(ZoomLevelDescriptionProperty);
        set => SetValue(ZoomLevelDescriptionProperty, value);
    }

    /// <summary>
    /// Gets or sets the pan position description for accessibility.
    /// </summary>
    public string PanPositionDescription
    {
        get => (string)GetValue(PanPositionDescriptionProperty);
        set => SetValue(PanPositionDescriptionProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use high contrast mode.
    /// </summary>
    public bool UseHighContrastMode
    {
        get => (bool)GetValue(UseHighContrastModeProperty);
        set => SetValue(UseHighContrastModeProperty, value);
    }
}
