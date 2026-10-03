// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;

namespace PanAndZoom;

public partial class ZoomBorder
{
    /// <summary>
    /// Adapts <see cref="ZoomBorder"/> to the UI framework independent <see cref="PanAndZoomEngine"/>.
    /// </summary>
    private sealed class EngineHost : IPanAndZoomHost, IPanAndZoomSettings
    {
        private readonly ZoomBorder _owner;

        public EngineHost(ZoomBorder owner)
        {
            _owner = owner;
        }

        public bool HasElement => _owner._element != null;

        public CoreSize ViewportSize => _owner.GetViewportSize();

        public CoreSize ElementSize => _owner.GetElementSize();

        public CorePoint ElementLayoutOffset => _owner.GetElementLayoutOffset();

        public (double minZoom, double maxZoom) CalculateAutoZoomLimits() => _owner.CalculateAutoZoomLimits();

        public CoreRect GetContentBounds() => _owner.GetContentBounds().ToCore();

        public bool ValidateTransform(CoreMatrix matrix) => _owner.ValidateTransform(matrix.ToUno());

        public void OnResized(CoreSize oldSize, CoreSize newSize) => _owner.OnResized(oldSize.ToUno(), newSize.ToUno());

        public void UpdateViewProperties(double zoomX, double zoomY, double offsetX, double offsetY) =>
            _owner.UpdateViewProperties(zoomX, zoomY, offsetX, offsetY);

        public void ApplyTransform(CoreMatrix renderMatrix, bool skipTransitions) =>
            _owner.ApplyElementTransform(renderMatrix, skipTransitions);

        public void OnIsPanningChanged(bool isPanning) => _owner.OnIsPanningChanged(isPanning);

        public void OnZoomIndicatorVisibilityChanged(bool isVisible) =>
            _owner.SetValue(IsZoomIndicatorVisibleProperty, isVisible);

        public void StartZoomIndicatorTimer(TimeSpan duration) => _owner.StartZoomIndicatorTimer(duration);

        ButtonName IPanAndZoomSettings.PanButton => _owner.PanButton;
        double IPanAndZoomSettings.ZoomSpeed
        {
            get => _owner.ZoomSpeed;
            set => _owner.ZoomSpeed = value;
        }

        double IPanAndZoomSettings.PowerFactor => _owner.PowerFactor;
        double IPanAndZoomSettings.TransitionThreshold => _owner.TransitionThreshold;
        StretchMode IPanAndZoomSettings.Stretch
        {
            get => _owner.Stretch;
            set => _owner.Stretch = value;
        }

        bool IPanAndZoomSettings.EnableConstrains
        {
            get => _owner.EnableConstrains;
            set => _owner.EnableConstrains = value;
        }

        double IPanAndZoomSettings.MinZoomX
        {
            get => _owner.MinZoomX;
            set => _owner.MinZoomX = value;
        }

        double IPanAndZoomSettings.MaxZoomX
        {
            get => _owner.MaxZoomX;
            set => _owner.MaxZoomX = value;
        }

        double IPanAndZoomSettings.MinZoomY
        {
            get => _owner.MinZoomY;
            set => _owner.MinZoomY = value;
        }

        double IPanAndZoomSettings.MaxZoomY
        {
            get => _owner.MaxZoomY;
            set => _owner.MaxZoomY = value;
        }

        double IPanAndZoomSettings.MinOffsetX => _owner.MinOffsetX;
        double IPanAndZoomSettings.MaxOffsetX => _owner.MaxOffsetX;
        double IPanAndZoomSettings.MinOffsetY => _owner.MinOffsetY;
        double IPanAndZoomSettings.MaxOffsetY => _owner.MaxOffsetY;
        bool IPanAndZoomSettings.EnablePan
        {
            get => _owner.EnablePan;
            set => _owner.EnablePan = value;
        }

        bool IPanAndZoomSettings.EnableZoom
        {
            get => _owner.EnableZoom;
            set => _owner.EnableZoom = value;
        }

        bool IPanAndZoomSettings.EnableGestureZoom => _owner.EnableGestureZoom;
        bool IPanAndZoomSettings.EnableGestureRotation => _owner.EnableGestureRotation;
        bool IPanAndZoomSettings.EnableGestureTranslation => _owner.EnableGestureTranslation;
        bool IPanAndZoomSettings.EnableGestures => _owner.EnableGestures;
        TimeSpan IPanAndZoomSettings.AnimationDuration
        {
            get => _owner.AnimationDuration;
            set => _owner.AnimationDuration = value;
        }

        bool IPanAndZoomSettings.EnableAnimations
        {
            get => _owner.EnableAnimations;
            set => _owner.EnableAnimations = value;
        }

        bool IPanAndZoomSettings.EnableDoubleClickZoom => _owner.EnableDoubleClickZoom;
        DoubleClickZoomMode IPanAndZoomSettings.DoubleClickZoomMode => _owner.DoubleClickZoomMode;
        double IPanAndZoomSettings.DoubleClickZoomFactor => _owner.DoubleClickZoomFactor;
        ContentBoundsMode IPanAndZoomSettings.BoundsMode => _owner.BoundsMode;
        CoreThickness IPanAndZoomSettings.BoundsPadding => _owner.BoundsPadding.ToCore();
        double IPanAndZoomSettings.MinimumVisibleContentPercentage => _owner.MinimumVisibleContentPercentage;
        ResizeBehaviorMode IPanAndZoomSettings.ResizeBehavior => _owner.ResizeBehavior;
        WheelBehaviorMode IPanAndZoomSettings.WheelBehavior => _owner.WheelBehavior;
        WheelBehaviorMode IPanAndZoomSettings.WheelWithCtrl => _owner.WheelWithCtrl;
        WheelBehaviorMode IPanAndZoomSettings.WheelWithShift => _owner.WheelWithShift;
        double IPanAndZoomSettings.WheelZoomSensitivity => _owner.WheelZoomSensitivity;
        double IPanAndZoomSettings.WheelPanSensitivity => _owner.WheelPanSensitivity;
        bool IPanAndZoomSettings.EnableKeyboardNavigation => _owner.EnableKeyboardNavigation;
        double IPanAndZoomSettings.KeyboardPanStep => _owner.KeyboardPanStep;
        double IPanAndZoomSettings.KeyboardZoomStep => _owner.KeyboardZoomStep;
        bool IPanAndZoomSettings.EnableViewHistory => _owner.EnableViewHistory;
        int IPanAndZoomSettings.ViewHistorySize => _owner.ViewHistorySize;
        CoreThickness IPanAndZoomSettings.CenterPadding => _owner.CenterPadding.ToCore();
        bool IPanAndZoomSettings.EnableDiscreteZoomLevels => _owner.EnableDiscreteZoomLevels;
        double[]? IPanAndZoomSettings.DiscreteZoomLevels => _owner.DiscreteZoomLevels;
        bool IPanAndZoomSettings.AutoCalculateMinZoom => _owner.AutoCalculateMinZoom;
        bool IPanAndZoomSettings.AutoCalculateMaxZoom => _owner.AutoCalculateMaxZoom;
        double IPanAndZoomSettings.MaxZoomPixelSize => _owner.MaxZoomPixelSize;
        bool IPanAndZoomSettings.ShowZoomIndicator => _owner.ShowZoomIndicator;
        ZoomIndicatorPosition IPanAndZoomSettings.ZoomIndicatorPosition => _owner.ZoomIndicatorPosition;
        string IPanAndZoomSettings.ZoomIndicatorFormat => _owner.ZoomIndicatorFormat;
        TimeSpan IPanAndZoomSettings.ZoomIndicatorAutoHideDuration => _owner.ZoomIndicatorAutoHideDuration;
        bool IPanAndZoomSettings.EnableSnapToGrid => _owner.EnableSnapToGrid;
        double IPanAndZoomSettings.GridSize => _owner.GridSize;
        double IPanAndZoomSettings.Rotation
        {
            get => _owner.Rotation;
            set => _owner.Rotation = value;
        }

        double IPanAndZoomSettings.MinRotation => _owner.MinRotation;
        double IPanAndZoomSettings.MaxRotation => _owner.MaxRotation;
        bool IPanAndZoomSettings.EnableRotationSnapping => _owner.EnableRotationSnapping;
        double IPanAndZoomSettings.RotationSnapAngle => _owner.RotationSnapAngle;
        bool IPanAndZoomSettings.EnableSimultaneousPanZoom => _owner.EnableSimultaneousPanZoom;
        int IPanAndZoomSettings.MinimumTouchPoints => _owner.MinimumTouchPoints;
        int IPanAndZoomSettings.MaximumTouchPoints => _owner.MaximumTouchPoints;
        TimeSpan IPanAndZoomSettings.GestureRecognitionDelay => _owner.GestureRecognitionDelay;
        string IPanAndZoomSettings.ZoomLevelDescription
        {
            get => _owner.ZoomLevelDescription;
            set => _owner.ZoomLevelDescription = value;
        }

        string IPanAndZoomSettings.PanPositionDescription
        {
            get => _owner.PanPositionDescription;
            set => _owner.PanPositionDescription = value;
        }

    }
}
