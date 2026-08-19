// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Transformation;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Pan and zoom control for Avalonia.
/// </summary>
public partial class ZoomBorder
{
    /// <summary>
    /// Gets available stretch modes.
    /// </summary>
    public static StretchMode[] StretchModes { get; } = (StretchMode[])Enum.GetValues(typeof(StretchMode));

    /// <summary>
    /// Gets available button names.
    /// </summary>
    public static ButtonName[] ButtonNames { get; } = (ButtonName[])Enum.GetValues(typeof(ButtonName));

    /// <summary>
    /// Identifies the <seealso cref="PanButton"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<ButtonName> PanButtonProperty =
        AvaloniaProperty.Register<ZoomBorder, ButtonName>(nameof(PanButton), ButtonName.Middle, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="ZoomSpeed"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> ZoomSpeedProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(ZoomSpeed), 1.2, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="PowerFactor"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> PowerFactorProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(PowerFactor), 1, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="TransitionThreshold"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> TransitionThresholdProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(TransitionThreshold), 0.5, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="Stretch"/> avalonia property.
    /// </summary>
    public static readonly  StyledProperty<StretchMode> StretchProperty =
        AvaloniaProperty.Register<ZoomBorder, StretchMode>(nameof(Stretch), StretchMode.Uniform, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="ZoomX"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, double> ZoomXProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, double>(nameof(ZoomX), o => o.ZoomX, null, 1.0);

    /// <summary>
    /// Identifies the <seealso cref="ZoomY"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, double> ZoomYProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, double>(nameof(ZoomY), o => o.ZoomY, null, 1.0);

    /// <summary>
    /// Identifies the <seealso cref="OffsetX"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, double> OffsetXProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, double>(nameof(OffsetX), o => o.OffsetX, null, 0.0);

    /// <summary>
    /// Identifies the <seealso cref="OffsetY"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, double> OffsetYProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, double>(nameof(OffsetY), o => o.OffsetY, null, 0.0);

    /// <summary>
    /// Identifies the <seealso cref="EnableConstrains"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableConstrainsProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableConstrains), true, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MinZoomX"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MinZoomXProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(MinZoomX), double.NegativeInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MaxZoomX"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MaxZoomXProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(MaxZoomX), double.PositiveInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MinZoomY"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MinZoomYProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(MinZoomY), double.NegativeInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MaxZoomY"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MaxZoomYProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(MaxZoomY), double.PositiveInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MinOffsetX"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MinOffsetXProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(MinOffsetX), double.NegativeInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MaxOffsetX"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MaxOffsetXProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(MaxOffsetX), double.PositiveInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MinOffsetY"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MinOffsetYProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(MinOffsetY), double.NegativeInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MaxOffsetY"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MaxOffsetYProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(MaxOffsetY), double.PositiveInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="EnablePan"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnablePanProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnablePan), true, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="EnableZoom"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableZoomProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableZoom), true, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="EnableGestureZoom"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableGestureZoomProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableGestureZoom), true, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="EnableGestureRotation"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableGestureRotationProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableGestureRotation), true, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="EnableGestureTranslation"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableGestureTranslationProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableGestureTranslation), true, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="EnableGestures"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableGesturesProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableGestures), true, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="AnimationDuration"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<TimeSpan> AnimationDurationProperty =
        AvaloniaProperty.Register<ZoomBorder, TimeSpan>(nameof(AnimationDuration), TimeSpan.FromMilliseconds(300), false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="EnableAnimations"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableAnimationsProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableAnimations), false, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="EnableDoubleClickZoom"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableDoubleClickZoomProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableDoubleClickZoom), true, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="DoubleClickZoomMode"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<DoubleClickZoomMode> DoubleClickZoomModeProperty =
        AvaloniaProperty.Register<ZoomBorder, DoubleClickZoomMode>(nameof(DoubleClickZoomMode), DoubleClickZoomMode.ZoomInOut, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="DoubleClickZoomFactor"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> DoubleClickZoomFactorProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(DoubleClickZoomFactor), 2.0, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="BoundsMode"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<ContentBoundsMode> BoundsModeProperty =
        AvaloniaProperty.Register<ZoomBorder, ContentBoundsMode>(nameof(BoundsMode), ContentBoundsMode.Unrestricted, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="BoundsPadding"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<Thickness> BoundsPaddingProperty =
        AvaloniaProperty.Register<ZoomBorder, Thickness>(nameof(BoundsPadding), new Thickness(0), false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MinimumVisibleContentPercentage"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MinimumVisibleContentPercentageProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(MinimumVisibleContentPercentage), 0.1, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="ResizeBehavior"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<ResizeBehaviorMode> ResizeBehaviorProperty =
        AvaloniaProperty.Register<ZoomBorder, ResizeBehaviorMode>(nameof(ResizeBehavior), ResizeBehaviorMode.None, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="WheelBehavior"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<WheelBehaviorMode> WheelBehaviorProperty =
        AvaloniaProperty.Register<ZoomBorder, WheelBehaviorMode>(nameof(WheelBehavior), WheelBehaviorMode.Zoom, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="WheelWithCtrl"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<WheelBehaviorMode> WheelWithCtrlProperty =
        AvaloniaProperty.Register<ZoomBorder, WheelBehaviorMode>(nameof(WheelWithCtrl), WheelBehaviorMode.Zoom, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="WheelWithShift"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<WheelBehaviorMode> WheelWithShiftProperty =
        AvaloniaProperty.Register<ZoomBorder, WheelBehaviorMode>(nameof(WheelWithShift), WheelBehaviorMode.PanHorizontal, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="WheelZoomSensitivity"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> WheelZoomSensitivityProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(WheelZoomSensitivity), 1.0, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="WheelPanSensitivity"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> WheelPanSensitivityProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(WheelPanSensitivity), 1.0, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="EnableKeyboardNavigation"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableKeyboardNavigationProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableKeyboardNavigation), true, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="KeyboardPanStep"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> KeyboardPanStepProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(KeyboardPanStep), 50.0, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="KeyboardZoomStep"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> KeyboardZoomStepProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(KeyboardZoomStep), 1.1, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="EnableViewHistory"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableViewHistoryProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableViewHistory), true, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="ViewHistorySize"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<int> ViewHistorySizeProperty =
        AvaloniaProperty.Register<ZoomBorder, int>(nameof(ViewHistorySize), 50, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="CenterPadding"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<Thickness> CenterPaddingProperty =
        AvaloniaProperty.Register<ZoomBorder, Thickness>(nameof(CenterPadding), new Thickness(0), false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="EnableDiscreteZoomLevels"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableDiscreteZoomLevelsProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableDiscreteZoomLevels), false, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="DiscreteZoomLevels"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double[]?> DiscreteZoomLevelsProperty =
        AvaloniaProperty.Register<ZoomBorder, double[]?>(nameof(DiscreteZoomLevels), new[] { 0.25, 0.5, 0.75, 1.0, 1.5, 2.0, 3.0, 4.0, 6.0, 8.0 }, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="AutoCalculateMinZoom"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> AutoCalculateMinZoomProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(AutoCalculateMinZoom), false, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="AutoCalculateMaxZoom"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> AutoCalculateMaxZoomProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(AutoCalculateMaxZoom), false, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MaxZoomPixelSize"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MaxZoomPixelSizeProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(MaxZoomPixelSize), 4.0, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="ShowZoomIndicator"/> avalonia property.
    /// When true, a zoom indicator will be displayed temporarily after zoom operations.
    /// </summary>
    public static readonly StyledProperty<bool> ShowZoomIndicatorProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(ShowZoomIndicator), false, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="ZoomIndicatorPosition"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<ZoomIndicatorPosition> ZoomIndicatorPositionProperty =
        AvaloniaProperty.Register<ZoomBorder, ZoomIndicatorPosition>(nameof(ZoomIndicatorPosition), ZoomIndicatorPosition.BottomRight, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="ZoomIndicatorFormat"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<string> ZoomIndicatorFormatProperty =
        AvaloniaProperty.Register<ZoomBorder, string>(nameof(ZoomIndicatorFormat), "{0:P0}", false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="ZoomIndicatorAutoHideDuration"/> avalonia property.
    /// Controls how long the zoom indicator remains visible before auto-hiding.
    /// </summary>
    public static readonly StyledProperty<TimeSpan> ZoomIndicatorAutoHideDurationProperty =
        AvaloniaProperty.Register<ZoomBorder, TimeSpan>(nameof(ZoomIndicatorAutoHideDuration), TimeSpan.FromSeconds(2), false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="IsZoomIndicatorVisible"/> avalonia property.
    /// This is a read-only property that indicates whether the zoom indicator is currently visible.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, bool> IsZoomIndicatorVisibleProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, bool>(
            nameof(IsZoomIndicatorVisible),
            o => o.IsZoomIndicatorVisible);

    /// <summary>
    /// Identifies the <seealso cref="ZoomInCommand"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, ICommand> ZoomInCommandProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, ICommand>(
            nameof(ZoomInCommand),
            o => o.ZoomInCommand);

    /// <summary>
    /// Identifies the <seealso cref="ZoomOutCommand"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, ICommand> ZoomOutCommandProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, ICommand>(
            nameof(ZoomOutCommand),
            o => o.ZoomOutCommand);

    /// <summary>
    /// Identifies the <seealso cref="ResetCommand"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, ICommand> ResetCommandProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, ICommand>(
            nameof(ResetCommand),
            o => o.ResetCommand);

    /// <summary>
    /// Identifies the <seealso cref="FitCommand"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, ICommand> FitCommandProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, ICommand>(
            nameof(FitCommand),
            o => o.FitCommand);

    /// <summary>
    /// Identifies the <seealso cref="FillCommand"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, ICommand> FillCommandProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, ICommand>(
            nameof(FillCommand),
            o => o.FillCommand);

    /// <summary>
    /// Identifies the <seealso cref="UniformCommand"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, ICommand> UniformCommandProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, ICommand>(
            nameof(UniformCommand),
            o => o.UniformCommand);

    /// <summary>
    /// Identifies the <seealso cref="UniformToFillCommand"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, ICommand> UniformToFillCommandProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, ICommand>(
            nameof(UniformToFillCommand),
            o => o.UniformToFillCommand);

    /// <summary>
    /// Identifies the <seealso cref="NavigateBackCommand"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, ICommand> NavigateBackCommandProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, ICommand>(
            nameof(NavigateBackCommand),
            o => o.NavigateBackCommand);

    /// <summary>
    /// Identifies the <seealso cref="NavigateForwardCommand"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, ICommand> NavigateForwardCommandProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, ICommand>(
            nameof(NavigateForwardCommand),
            o => o.NavigateForwardCommand);

    /// <summary>
    /// Identifies the <seealso cref="ToggleStretchCommand"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, ICommand> ToggleStretchCommandProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, ICommand>(
            nameof(ToggleStretchCommand),
            o => o.ToggleStretchCommand);

    /// <summary>
    /// Identifies the <seealso cref="ShowGrid"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(ShowGrid), false, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="EnableSnapToGrid"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableSnapToGridProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableSnapToGrid), false, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="GridSize"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> GridSizeProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(GridSize), 50.0, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="GridBrush"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> GridBrushProperty =
        AvaloniaProperty.Register<ZoomBorder, IBrush?>(nameof(GridBrush), null, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="GridThickness"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> GridThicknessProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(GridThickness), 1.0, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="GridOpacity"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> GridOpacityProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(GridOpacity), 0.3, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MajorGridInterval"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<int> MajorGridIntervalProperty =
        AvaloniaProperty.Register<ZoomBorder, int>(nameof(MajorGridInterval), 5, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MajorGridBrush"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> MajorGridBrushProperty =
        AvaloniaProperty.Register<ZoomBorder, IBrush?>(nameof(MajorGridBrush), null, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MajorGridThickness"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MajorGridThicknessProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(MajorGridThickness), 2.0, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="Rotation"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> RotationProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(Rotation), 0.0, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MinRotation"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MinRotationProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(MinRotation), -180.0, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MaxRotation"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MaxRotationProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(MaxRotation), 180.0, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="EnableRotationSnapping"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableRotationSnappingProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableRotationSnapping), false, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="RotationSnapAngle"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> RotationSnapAngleProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(nameof(RotationSnapAngle), 45.0, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="EnableSimultaneousPanZoom"/> avalonia property.
    /// When true, allows pan and zoom gestures to occur simultaneously. When false, only one gesture type is active at a time.
    /// </summary>
    public static readonly StyledProperty<bool> EnableSimultaneousPanZoomProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableSimultaneousPanZoom), true, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MinimumTouchPoints"/> avalonia property.
    /// Controls the minimum number of touch points required to activate gestures.
    /// </summary>
    public static readonly StyledProperty<int> MinimumTouchPointsProperty =
        AvaloniaProperty.Register<ZoomBorder, int>(nameof(MinimumTouchPoints), 1, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="MaximumTouchPoints"/> avalonia property.
    /// Controls the maximum number of touch points that will be tracked for gestures.
    /// </summary>
    public static readonly StyledProperty<int> MaximumTouchPointsProperty =
        AvaloniaProperty.Register<ZoomBorder, int>(nameof(MaximumTouchPoints), 2, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="GestureRecognitionDelay"/> avalonia property.
    /// Controls the delay before a gesture is recognized, allowing time to accumulate touch points.
    /// Default is zero (no delay). Set a positive value to enable gesture recognition delay.
    /// </summary>
    public static readonly StyledProperty<TimeSpan> GestureRecognitionDelayProperty =
        AvaloniaProperty.Register<ZoomBorder, TimeSpan>(nameof(GestureRecognitionDelay), TimeSpan.Zero, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="ZoomLevelDescription"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<string> ZoomLevelDescriptionProperty =
        AvaloniaProperty.Register<ZoomBorder, string>(nameof(ZoomLevelDescription), string.Empty, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="PanPositionDescription"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<string> PanPositionDescriptionProperty =
        AvaloniaProperty.Register<ZoomBorder, string>(nameof(PanPositionDescription), string.Empty, false, BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <seealso cref="UseHighContrastMode"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> UseHighContrastModeProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(nameof(UseHighContrastMode), false, false, BindingMode.TwoWay);

    static ZoomBorder()
    {
        AffectsArrange<ZoomBorder>(
            ZoomSpeedProperty,
            StretchProperty,
            EnableConstrainsProperty,
            EnablePanProperty,
            EnableZoomProperty,
            EnableGestureZoomProperty,
            EnableGestureRotationProperty,
            EnableGestureTranslationProperty,
            EnableGesturesProperty,
            MinZoomXProperty,
            MaxZoomXProperty,
            MinZoomYProperty,
            MaxZoomYProperty,
            MinOffsetXProperty,
            MaxOffsetXProperty,
            MinOffsetYProperty,
            MaxOffsetYProperty);
    }

    private Control? _element;
    private Point _pan;
    private Point _previous;
    private Matrix _matrix;
    private TransformOperations.Builder _transformBuilder;
    private bool _isPanning;
    private volatile bool _updating = false;
    private double _zoomX = 1.0;
    private double _zoomY = 1.0;
    private double _offsetX = 0.0;
    private double _offsetY = 0.0;
    private bool _captured = false;
    private Size _sizeBeforeResize;
    private double _doubleClickZoomThreshold = 1.5;
    private List<ViewState> _viewHistory = new List<ViewState>();
    private int _viewHistoryIndex = -1;
    private bool _isNavigating = false;
    private Dictionary<string, SavedView> _savedViews = new Dictionary<string, SavedView>();

    // Commands
    private ZoomBorderCommand? _zoomInCommand;
    private ZoomBorderCommand? _zoomOutCommand;
    private ZoomBorderCommand? _resetCommand;
    private ZoomBorderCommand? _fitCommand;
    private ZoomBorderCommand? _fillCommand;
    private ZoomBorderCommand? _uniformCommand;
    private ZoomBorderCommand? _uniformToFillCommand;
    private ZoomBorderCommand? _navigateBackCommand;
    private ZoomBorderCommand? _navigateForwardCommand;
    private ZoomBorderCommand? _toggleStretchCommand;

    /// <summary>
    /// Zoom changed event.
    /// </summary>
    public event ZoomChangedEventHandler? ZoomChanged;

    /// <summary>
    /// View history changed event.
    /// </summary>
    public event EventHandler? ViewHistoryChanged;

    /// <summary>
    /// Pan started event.
    /// </summary>
    public event PanEventHandler? PanStarted;

    /// <summary>
    /// Pan continued event.
    /// </summary>
    public event PanEventHandler? PanContinued;

    /// <summary>
    /// Pan ended event.
    /// </summary>
    public event PanEventHandler? PanEnded;

    /// <summary>
    /// Zoom started event.
    /// </summary>
    public event ZoomEventHandler? ZoomStarted;

    /// <summary>
    /// Zoom ended event.
    /// </summary>
    public event ZoomEventHandler? ZoomEnded;

    /// <summary>
    /// Zoom delta changed event.
    /// </summary>
    public event ZoomEventHandler? ZoomDeltaChanged;

    /// <summary>
    /// Matrix changed event.
    /// </summary>
    public event MatrixChangedEventHandler? MatrixChanged;

    /// <summary>
    /// Matrix reset event.
    /// </summary>
    public event MatrixChangedEventHandler? MatrixReset;

    /// <summary>
    /// Stretch mode changed event.
    /// </summary>
    public event StretchModeChangedEventHandler? StretchModeChanged;

    /// <summary>
    /// Auto fit applied event.
    /// </summary>
    public event StretchModeChangedEventHandler? AutoFitApplied;

    /// <summary>
    /// Gesture started event.
    /// </summary>
    public event GestureEventHandler? GestureStarted;

    /// <summary>
    /// Gesture ended event.
    /// </summary>
    public event GestureEventHandler? GestureEnded;

    /// <summary>
    /// Gets or sets pan input button.
    /// </summary>
    public ButtonName PanButton
    {
        get => GetValue(PanButtonProperty);
        set => SetValue(PanButtonProperty, value);
    }

    /// <summary>
    /// Gets or sets zoom speed ratio.
    /// </summary>
    public double ZoomSpeed
    {
        get => GetValue(ZoomSpeedProperty);
        set => SetValue(ZoomSpeedProperty, value);
    }

    /// <summary>
    /// Gets or sets the power factor used to transform the mouse wheel delta value.
    /// </summary>
    public double PowerFactor
    {
        get => GetValue(PowerFactorProperty);
        set => SetValue(PowerFactorProperty, value);
    }

    /// <summary>
    /// Gets or sets the threshold below which zoom operations will skip all transitions.
    /// </summary>
    public double TransitionThreshold
    {
        get => GetValue(TransitionThresholdProperty);
        set => SetValue(TransitionThresholdProperty, value);
    }

    /// <summary>
    /// Gets or sets stretch mode.
    /// </summary>
    public StretchMode Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <summary>
    /// Gets the render transform matrix.
    /// </summary>
    public Matrix Matrix => _matrix;

    /// <summary>
    /// Gets the zoom ratio for x axis.
    /// </summary>
    public double ZoomX => _zoomX;

    /// <summary>
    /// Gets the zoom ratio for y axis.
    /// </summary>
    public double ZoomY => _zoomY;

    /// <summary>
    /// Gets the pan offset for x axis.
    /// </summary>
    public double OffsetX => _offsetX;

    /// <summary>
    /// Gets the pan offset for y axis.
    /// </summary>
    public double OffsetY => _offsetY;

    /// <summary>
    /// Gets or sets flag indicating whether zoom ratio and pan offset constrains are applied.
    /// </summary>
    public bool EnableConstrains
    {
        get => GetValue(EnableConstrainsProperty);
        set => SetValue(EnableConstrainsProperty, value);
    }

    /// <summary>
    /// Gets or sets minimum zoom ratio for x axis.
    /// </summary>
    public double MinZoomX
    {
        get => GetValue(MinZoomXProperty);
        set => SetValue(MinZoomXProperty, value);
    }

    /// <summary>
    /// Gets or sets maximum zoom ratio for x axis.
    /// </summary>
    public double MaxZoomX
    {
        get => GetValue(MaxZoomXProperty);
        set => SetValue(MaxZoomXProperty, value);
    }

    /// <summary>
    /// Gets or sets minimum zoom ratio for y axis.
    /// </summary>
    public double MinZoomY
    {
        get => GetValue(MinZoomYProperty);
        set => SetValue(MinZoomYProperty, value);
    }

    /// <summary>
    /// Gets or sets maximum zoom ratio for y axis.
    /// </summary>
    public double MaxZoomY
    {
        get => GetValue(MaxZoomYProperty);
        set => SetValue(MaxZoomYProperty, value);
    }

    /// <summary>
    /// Gets or sets minimum offset for x axis.
    /// </summary>
    public double MinOffsetX
    {
        get => GetValue(MinOffsetXProperty);
        set => SetValue(MinOffsetXProperty, value);
    }

    /// <summary>
    /// Gets or sets maximum offset for x axis.
    /// </summary>
    public double MaxOffsetX
    {
        get => GetValue(MaxOffsetXProperty);
        set => SetValue(MaxOffsetXProperty, value);
    }

    /// <summary>
    /// Gets or sets minimum offset for y axis.
    /// </summary>
    public double MinOffsetY
    {
        get => GetValue(MinOffsetYProperty);
        set => SetValue(MinOffsetYProperty, value);
    }

    /// <summary>
    /// Gets or sets maximum offset for y axis.
    /// </summary>
    public double MaxOffsetY
    {
        get => GetValue(MaxOffsetYProperty);
        set => SetValue(MaxOffsetYProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether pan input events are processed.
    /// </summary>
    public bool EnablePan
    {
        get => GetValue(EnablePanProperty);
        set => SetValue(EnablePanProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether input zoom events are processed.
    /// </summary>
    public bool EnableZoom
    {
        get => GetValue(EnableZoomProperty);
        set => SetValue(EnableZoomProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether zoom gesture is enabled.
    /// </summary>
    public bool EnableGestureZoom
    {
        get => GetValue(EnableGestureZoomProperty);
        set => SetValue(EnableGestureZoomProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether rotation gesture is enabled.
    /// </summary>
    public bool EnableGestureRotation
    {
        get => GetValue(EnableGestureRotationProperty);
        set => SetValue(EnableGestureRotationProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether translation (pan) gesture is enabled.
    /// </summary>
    public bool EnableGestureTranslation
    {
        get => GetValue(EnableGestureTranslationProperty);
        set => SetValue(EnableGestureTranslationProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether gestures are enabled.
    /// </summary>
    public bool EnableGestures
    {
        get => GetValue(EnableGesturesProperty);
        set => SetValue(EnableGesturesProperty, value);
    }

    /// <summary>
    /// Gets or sets the duration of animations for zoom and pan operations.
    /// </summary>
    public TimeSpan AnimationDuration
    {
        get => GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether animations are enabled for zoom and pan operations.
    /// </summary>
    public bool EnableAnimations
    {
        get => GetValue(EnableAnimationsProperty);
        set => SetValue(EnableAnimationsProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether double-click zoom is enabled.
    /// </summary>
    public bool EnableDoubleClickZoom
    {
        get => GetValue(EnableDoubleClickZoomProperty);
        set => SetValue(EnableDoubleClickZoomProperty, value);
    }

    /// <summary>
    /// Gets or sets the double-click zoom behavior mode.
    /// </summary>
    public DoubleClickZoomMode DoubleClickZoomMode
    {
        get => GetValue(DoubleClickZoomModeProperty);
        set => SetValue(DoubleClickZoomModeProperty, value);
    }

    /// <summary>
    /// Gets or sets the zoom factor for double-click zoom operations.
    /// </summary>
    public double DoubleClickZoomFactor
    {
        get => GetValue(DoubleClickZoomFactorProperty);
        set => SetValue(DoubleClickZoomFactorProperty, value);
    }

    /// <summary>
    /// Gets or sets the content bounds restriction mode.
    /// </summary>
    public ContentBoundsMode BoundsMode
    {
        get => GetValue(BoundsModeProperty);
        set => SetValue(BoundsModeProperty, value);
    }

    /// <summary>
    /// Gets or sets the padding around content bounds.
    /// </summary>
    public Thickness BoundsPadding
    {
        get => GetValue(BoundsPaddingProperty);
        set => SetValue(BoundsPaddingProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum percentage of content that must remain visible.
    /// </summary>
    public double MinimumVisibleContentPercentage
    {
        get => GetValue(MinimumVisibleContentPercentageProperty);
        set => SetValue(MinimumVisibleContentPercentageProperty, value);
    }

    /// <summary>
    /// Gets or sets the behavior when the control is resized.
    /// </summary>
    public ResizeBehaviorMode ResizeBehavior
    {
        get => GetValue(ResizeBehaviorProperty);
        set => SetValue(ResizeBehaviorProperty, value);
    }

    /// <summary>
    /// Gets or sets the default mouse wheel behavior (no modifiers).
    /// </summary>
    public WheelBehaviorMode WheelBehavior
    {
        get => GetValue(WheelBehaviorProperty);
        set => SetValue(WheelBehaviorProperty, value);
    }

    /// <summary>
    /// Gets or sets the mouse wheel behavior when Ctrl key is pressed.
    /// </summary>
    public WheelBehaviorMode WheelWithCtrl
    {
        get => GetValue(WheelWithCtrlProperty);
        set => SetValue(WheelWithCtrlProperty, value);
    }

    /// <summary>
    /// Gets or sets the mouse wheel behavior when Shift key is pressed.
    /// </summary>
    public WheelBehaviorMode WheelWithShift
    {
        get => GetValue(WheelWithShiftProperty);
        set => SetValue(WheelWithShiftProperty, value);
    }

    /// <summary>
    /// Gets or sets the zoom sensitivity for mouse wheel operations.
    /// </summary>
    public double WheelZoomSensitivity
    {
        get => GetValue(WheelZoomSensitivityProperty);
        set => SetValue(WheelZoomSensitivityProperty, value);
    }

    /// <summary>
    /// Gets or sets the pan sensitivity for mouse wheel operations.
    /// </summary>
    public double WheelPanSensitivity
    {
        get => GetValue(WheelPanSensitivityProperty);
        set => SetValue(WheelPanSensitivityProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether keyboard navigation is enabled.
    /// </summary>
    public bool EnableKeyboardNavigation
    {
        get => GetValue(EnableKeyboardNavigationProperty);
        set => SetValue(EnableKeyboardNavigationProperty, value);
    }

    /// <summary>
    /// Gets or sets the pan step distance for keyboard navigation.
    /// </summary>
    public double KeyboardPanStep
    {
        get => GetValue(KeyboardPanStepProperty);
        set => SetValue(KeyboardPanStepProperty, value);
    }

    /// <summary>
    /// Gets or sets the zoom step factor for keyboard navigation.
    /// </summary>
    public double KeyboardZoomStep
    {
        get => GetValue(KeyboardZoomStepProperty);
        set => SetValue(KeyboardZoomStepProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether view history is enabled.
    /// </summary>
    public bool EnableViewHistory
    {
        get => GetValue(EnableViewHistoryProperty);
        set => SetValue(EnableViewHistoryProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum number of view states to store in history.
    /// </summary>
    public int ViewHistorySize
    {
        get => GetValue(ViewHistorySizeProperty);
        set => SetValue(ViewHistorySizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the padding to apply when centering on a point or element.
    /// </summary>
    public Thickness CenterPadding
    {
        get => GetValue(CenterPaddingProperty);
        set => SetValue(CenterPaddingProperty, value);
    }

    /// <summary>
    /// Gets or sets flag indicating whether discrete zoom levels are enabled.
    /// </summary>
    public bool EnableDiscreteZoomLevels
    {
        get => GetValue(EnableDiscreteZoomLevelsProperty);
        set => SetValue(EnableDiscreteZoomLevelsProperty, value);
    }

    /// <summary>
    /// Gets or sets the array of discrete zoom levels.
    /// </summary>
    public double[]? DiscreteZoomLevels
    {
        get => GetValue(DiscreteZoomLevelsProperty);
        set => SetValue(DiscreteZoomLevelsProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically calculate minimum zoom.
    /// </summary>
    public bool AutoCalculateMinZoom
    {
        get => GetValue(AutoCalculateMinZoomProperty);
        set => SetValue(AutoCalculateMinZoomProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically calculate maximum zoom.
    /// </summary>
    public bool AutoCalculateMaxZoom
    {
        get => GetValue(AutoCalculateMaxZoomProperty);
        set => SetValue(AutoCalculateMaxZoomProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum zoom pixel size (1 content pixel = N screen pixels).
    /// </summary>
    public double MaxZoomPixelSize
    {
        get => GetValue(MaxZoomPixelSizeProperty);
        set => SetValue(MaxZoomPixelSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show the zoom indicator.
    /// </summary>
    public bool ShowZoomIndicator
    {
        get => GetValue(ShowZoomIndicatorProperty);
        set => SetValue(ShowZoomIndicatorProperty, value);
    }

    /// <summary>
    /// Gets or sets the position of the zoom indicator.
    /// </summary>
    public ZoomIndicatorPosition ZoomIndicatorPosition
    {
        get => GetValue(ZoomIndicatorPositionProperty);
        set => SetValue(ZoomIndicatorPositionProperty, value);
    }

    /// <summary>
    /// Gets or sets the format string for the zoom indicator.
    /// </summary>
    public string ZoomIndicatorFormat
    {
        get => GetValue(ZoomIndicatorFormatProperty);
        set => SetValue(ZoomIndicatorFormatProperty, value);
    }

    /// <summary>
    /// Gets or sets the auto-hide duration for the zoom indicator.
    /// </summary>
    public TimeSpan ZoomIndicatorAutoHideDuration
    {
        get => GetValue(ZoomIndicatorAutoHideDurationProperty);
        set => SetValue(ZoomIndicatorAutoHideDurationProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether the zoom indicator is currently visible.
    /// This property is read-only and controlled by the auto-hide timer.
    /// </summary>
    public bool IsZoomIndicatorVisible => _zoomIndicatorVisible;

    /// <summary>
    /// Gets or sets a value indicating whether to show the grid.
    /// </summary>
    public bool ShowGrid
    {
        get => GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to enable snap to grid.
    /// </summary>
    public bool EnableSnapToGrid
    {
        get => GetValue(EnableSnapToGridProperty);
        set => SetValue(EnableSnapToGridProperty, value);
    }

    /// <summary>
    /// Gets or sets the grid size.
    /// </summary>
    public double GridSize
    {
        get => GetValue(GridSizeProperty);
        set => SetValue(GridSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the grid brush.
    /// </summary>
    public IBrush? GridBrush
    {
        get => GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the grid thickness.
    /// </summary>
    public double GridThickness
    {
        get => GetValue(GridThicknessProperty);
        set => SetValue(GridThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets the grid opacity.
    /// </summary>
    public double GridOpacity
    {
        get => GetValue(GridOpacityProperty);
        set => SetValue(GridOpacityProperty, value);
    }

    /// <summary>
    /// Gets or sets the major grid interval.
    /// </summary>
    public int MajorGridInterval
    {
        get => GetValue(MajorGridIntervalProperty);
        set => SetValue(MajorGridIntervalProperty, value);
    }

    /// <summary>
    /// Gets or sets the major grid brush.
    /// </summary>
    public IBrush? MajorGridBrush
    {
        get => GetValue(MajorGridBrushProperty);
        set => SetValue(MajorGridBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the major grid thickness.
    /// </summary>
    public double MajorGridThickness
    {
        get => GetValue(MajorGridThicknessProperty);
        set => SetValue(MajorGridThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets the current rotation angle in degrees.
    /// </summary>
    public double Rotation
    {
        get => GetValue(RotationProperty);
        set => SetValue(RotationProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum rotation angle in degrees.
    /// </summary>
    public double MinRotation
    {
        get => GetValue(MinRotationProperty);
        set => SetValue(MinRotationProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum rotation angle in degrees.
    /// </summary>
    public double MaxRotation
    {
        get => GetValue(MaxRotationProperty);
        set => SetValue(MaxRotationProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether rotation snapping is enabled.
    /// </summary>
    public bool EnableRotationSnapping
    {
        get => GetValue(EnableRotationSnappingProperty);
        set => SetValue(EnableRotationSnappingProperty, value);
    }

    /// <summary>
    /// Gets or sets the rotation snap angle in degrees.
    /// </summary>
    public double RotationSnapAngle
    {
        get => GetValue(RotationSnapAngleProperty);
        set => SetValue(RotationSnapAngleProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether simultaneous pan and zoom is enabled.
    /// When true, allows pan and zoom gestures to occur at the same time.
    /// </summary>
    public bool EnableSimultaneousPanZoom
    {
        get => GetValue(EnableSimultaneousPanZoomProperty);
        set => SetValue(EnableSimultaneousPanZoomProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum number of touch points required to activate gestures.
    /// </summary>
    public int MinimumTouchPoints
    {
        get => GetValue(MinimumTouchPointsProperty);
        set => SetValue(MinimumTouchPointsProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum number of touch points that will be tracked.
    /// </summary>
    public int MaximumTouchPoints
    {
        get => GetValue(MaximumTouchPointsProperty);
        set => SetValue(MaximumTouchPointsProperty, value);
    }

    /// <summary>
    /// Gets or sets the gesture recognition delay before recognizing a gesture.
    /// </summary>
    public TimeSpan GestureRecognitionDelay
    {
        get => GetValue(GestureRecognitionDelayProperty);
        set => SetValue(GestureRecognitionDelayProperty, value);
    }

    /// <summary>
    /// Gets or sets the zoom level description for accessibility.
    /// </summary>
    public string ZoomLevelDescription
    {
        get => GetValue(ZoomLevelDescriptionProperty);
        set => SetValue(ZoomLevelDescriptionProperty, value);
    }

    /// <summary>
    /// Gets or sets the pan position description for accessibility.
    /// </summary>
    public string PanPositionDescription
    {
        get => GetValue(PanPositionDescriptionProperty);
        set => SetValue(PanPositionDescriptionProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use high contrast mode.
    /// </summary>
    public bool UseHighContrastMode
    {
        get => GetValue(UseHighContrastModeProperty);
        set => SetValue(UseHighContrastModeProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether the control can navigate back in view history.
    /// </summary>
    public bool CanNavigateBack => EnableViewHistory && _viewHistoryIndex > 0;

    /// <summary>
    /// Gets a value indicating whether the control can navigate forward in view history.
    /// </summary>
    public bool CanNavigateForward => EnableViewHistory && _viewHistoryIndex < _viewHistory.Count - 1;

    /// <summary>
    /// Gets the command to zoom in.
    /// </summary>
    public ICommand ZoomInCommand => _zoomInCommand ??= new ZoomBorderCommand(() => ZoomIn(ShouldSkipTransitions()), () => EnableZoom && _element != null);

    /// <summary>
    /// Gets the command to zoom out.
    /// </summary>
    public ICommand ZoomOutCommand => _zoomOutCommand ??= new ZoomBorderCommand(() => ZoomOut(ShouldSkipTransitions()), () => EnableZoom && _element != null);

    /// <summary>
    /// Gets the command to reset the view.
    /// </summary>
    public ICommand ResetCommand => _resetCommand ??= new ZoomBorderCommand(() => ResetMatrix(ShouldSkipTransitions()));

    /// <summary>
    /// Gets the command to fit content to viewport.
    /// </summary>
    public ICommand FitCommand => _fitCommand ??= new ZoomBorderCommand(() => AutoFit(ShouldSkipTransitions()), () => _element != null);

    /// <summary>
    /// Gets the command to fill viewport.
    /// </summary>
    public ICommand FillCommand => _fillCommand ??= new ZoomBorderCommand(() => Fill(ShouldSkipTransitions()), () => _element != null);

    /// <summary>
    /// Gets the command to apply uniform stretch.
    /// </summary>
    public ICommand UniformCommand => _uniformCommand ??= new ZoomBorderCommand(() => Uniform(ShouldSkipTransitions()), () => _element != null);

    /// <summary>
    /// Gets the command to apply uniform to fill stretch.
    /// </summary>
    public ICommand UniformToFillCommand => _uniformToFillCommand ??= new ZoomBorderCommand(() => UniformToFill(ShouldSkipTransitions()), () => _element != null);

    /// <summary>
    /// Gets the command to navigate back in view history.
    /// </summary>
    public ICommand NavigateBackCommand => _navigateBackCommand ??= new ZoomBorderCommand(() => NavigateBack(ShouldAnimate()), () => CanNavigateBack);

    /// <summary>
    /// Gets the command to navigate forward in view history.
    /// </summary>
    public ICommand NavigateForwardCommand => _navigateForwardCommand ??= new ZoomBorderCommand(() => NavigateForward(ShouldAnimate()), () => CanNavigateForward);

    /// <summary>
    /// Gets the command to toggle stretch mode.
    /// </summary>
    public ICommand ToggleStretchCommand => _toggleStretchCommand ??= new ZoomBorderCommand(ToggleStretchMode);

    /// <summary>
    /// Raises CanExecuteChanged on all commands to refresh their enabled state.
    /// </summary>
    internal void RaiseCommandsCanExecuteChanged()
    {
        _zoomInCommand?.RaiseCanExecuteChanged();
        _zoomOutCommand?.RaiseCanExecuteChanged();
        _resetCommand?.RaiseCanExecuteChanged();
        _fitCommand?.RaiseCanExecuteChanged();
        _fillCommand?.RaiseCanExecuteChanged();
        _uniformCommand?.RaiseCanExecuteChanged();
        _uniformToFillCommand?.RaiseCanExecuteChanged();
        _navigateBackCommand?.RaiseCanExecuteChanged();
        _navigateForwardCommand?.RaiseCanExecuteChanged();
        _toggleStretchCommand?.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// Raises CanExecuteChanged on navigation commands to refresh their enabled state.
    /// </summary>
    internal void RaiseNavigationCommandsCanExecuteChanged()
    {
        _navigateBackCommand?.RaiseCanExecuteChanged();
        _navigateForwardCommand?.RaiseCanExecuteChanged();
    }
}

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
    public Matrix Matrix { get; set; }

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
