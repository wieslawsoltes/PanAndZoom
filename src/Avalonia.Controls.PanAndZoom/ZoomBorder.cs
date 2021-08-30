using System;
using System.Diagnostics;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using static System.Math;

namespace Avalonia.Controls.PanAndZoom
{
    /// <summary>
    /// Pan and zoom control for Avalonia.
    /// </summary>
    public partial class ZoomBorder : Border
    {
        [Conditional("DEBUG")]
        internal static void Log(string message) => Debug.WriteLine(message);

        private static double ClampValue(double value, double minimum, double maximum)
        {
            if (minimum > maximum)
                throw new ArgumentException($"Parameter {nameof(minimum)} is greater than {nameof(maximum)}.");

            if (maximum < minimum)
                throw new ArgumentException($"Parameter {nameof(maximum)} is lower than {nameof(minimum)}.");

            return Min(Max(value, minimum), maximum);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomBorder"/> class.
        /// </summary>
        public ZoomBorder()
            : base()
        {
            _isPanning = false;
            _matrix = Matrix.Identity;
            _captured = false;

            Focusable = true;
            Background = Brushes.Transparent;

            AttachedToVisualTree += PanAndZoom_AttachedToVisualTree;
            DetachedFromVisualTree += PanAndZoom_DetachedFromVisualTree;

            this.GetObservable(ChildProperty).Subscribe(ChildChanged);
            this.GetObservable(BoundsProperty).Subscribe(BoundsChanged);
        }

        /// <summary>
        /// Arranges the control's child.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = base.ArrangeOverride(finalSize);

            if (_element == null || !_element.IsMeasureValid)
            {
                return size;
            }

            AutoFit(size.Width, size.Height, _element.Bounds.Width, _element.Bounds.Height);

            return size;
        }

        private void PanAndZoom_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Log($"[AttachedToVisualTree] {Name}");
            ChildChanged(Child);
        }

        private void PanAndZoom_DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Log($"[DetachedFromVisualTree] {Name}");
            DetachElement();
        }

        private void Border_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (!EnableZoom)
            {
                return;
            }
            Wheel(e);
        }

        private void Border_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            Pressed(e);
        }

        private void Border_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            Released(e);
        }

        private void Border_PointerMoved(object? sender, PointerEventArgs e)
        {
            Moved(e);
        }

        private void BoundsChanged(Rect bounds)
        {
            Log($"[BoundsChanged] {bounds}");

            // TODO: InvalidateScrollable();
        }

        private void ChildChanged(IControl? element)
        {
            Log($"[ChildChanged] {element}");

            if (element != null && element != _element && _element != null)
            {
                DetachElement();
            }

            if (element != null && element != _element)
            {
                AttachElement(element);
            }
        }

        private void AttachElement(IControl? element)
        {
            if (element == null)
            {
                return;
            }
            _element = element;
            PointerWheelChanged += Border_PointerWheelChanged;
            PointerPressed += Border_PointerPressed;
            PointerReleased += Border_PointerReleased;
            PointerMoved += Border_PointerMoved;
        }

        private void DetachElement()
        {
            if (_element == null)
            {
                return;
            }
            PointerWheelChanged -= Border_PointerWheelChanged;
            PointerPressed -= Border_PointerPressed;
            PointerReleased -= Border_PointerReleased;
            PointerMoved -= Border_PointerMoved;
            _element.RenderTransform = null;
            _element = null;
        }

        private void Wheel(PointerWheelEventArgs e)
        {
            if (_element == null || _captured != false)
            {
                return;
            }
            var point = e.GetPosition(_element);
            ZoomDeltaTo(e.Delta.Y, point.X, point.Y, true);
        }

        private void Pressed(PointerPressedEventArgs e)
        {
            if (!EnablePan)
            {
                return;
            }
            var button = PanButton;
            var properties = e.GetCurrentPoint(this).Properties;
            if ((!properties.IsLeftButtonPressed || button != ButtonName.Left)
                && (!properties.IsRightButtonPressed || button != ButtonName.Right)
                && (!properties.IsMiddleButtonPressed || button != ButtonName.Middle))
            {
                return;
            }
            if (_element != null && _captured == false && _isPanning == false)
            {
                var point = e.GetPosition(_element);
                BeginPanTo(point.X, point.Y);
                _captured = true;
                _isPanning = true;
            }
        }

        private void Released(PointerReleasedEventArgs e)
        {
            if (!EnablePan)
            {
                return;
            }
            if (_element == null || _captured != true || _isPanning != true)
            {
                return;
            }
            _captured = false;
            _isPanning = false;
        }

        private void Moved(PointerEventArgs e)
        {
            if (!EnablePan)
            {
                return;
            }
            if (_element == null || _captured != true || _isPanning != true)
            {
                return;
            }
            var point = e.GetPosition(_element);
            ContinuePanTo(point.X, point.Y, true, true);
        }

        /// <summary>
        /// Raises <see cref="ZoomChanged"/> event.
        /// </summary>
        /// <param name="e">Zoom changed event arguments.</param>
        protected virtual void OnZoomChanged(ZoomChangedEventArgs e)
        {
            ZoomChanged?.Invoke(this, e);
        }

        private void RaiseZoomChanged()
        {
            var args = new ZoomChangedEventArgs(_zoomX, _zoomY, _offsetX, _offsetY);
            OnZoomChanged(args);
        }

        private void Constrain()
        {
            var zoomX = ClampValue(_matrix.M11, MinZoomX, MaxZoomX);
            var zoomY = ClampValue(_matrix.M22, MinZoomY, MaxZoomY);
            var offsetX = ClampValue(_matrix.M31, MinOffsetX, MaxOffsetX);
            var offsetY = ClampValue(_matrix.M32, MinOffsetY, MaxOffsetY);
            _matrix = new Matrix(zoomX, 0.0, 0.0, zoomY, offsetX, offsetY);
        }

        /// <summary>
        /// Invalidate pan and zoom control.
        /// </summary>
        /// <param name="invalidateScroll">The flag indicating whether to invalidate scroll.</param>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void Invalidate(bool invalidateScroll, bool skipTransitions = false)
        {
            if (_element == null)
            {
                return;
            }

            lock (_lock)
            {
                if (EnableConstrains)
                {
                    Constrain();
                }

                InvalidateProperties();
                InvalidateElement(skipTransitions);

                if (invalidateScroll)
                {
                    InvalidateScrollable();
                }

                RaiseZoomChanged();
            }
        }

        /// <summary>
        /// Invalidate properties.
        /// </summary>
        private void InvalidateProperties()
        {
            var oldZoomX = _zoomX;
            var oldZoomY = _zoomY;
            var oldOffsetX = _offsetX;
            var oldOffsetY = _offsetY;

            _zoomX = _matrix.M11;
            _zoomY = _matrix.M22;
            _offsetX = _matrix.M31;
            _offsetY = _matrix.M32;

            RaisePropertyChanged(ZoomXProperty, oldZoomX, _zoomX);
            RaisePropertyChanged(ZoomYProperty, oldZoomY, _zoomY);
            RaisePropertyChanged(OffsetXProperty, oldOffsetX, _offsetX);
            RaisePropertyChanged(OffsetYProperty, oldOffsetY, _offsetY);
        }

        /// <summary>
        /// Invalidate child element.
        /// </summary>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        private void InvalidateElement(bool skipTransitions)
        {
            if (_element == null)
            {
                return;
            }

            Animation.Transitions? backupTransitions = null;

            if (skipTransitions)
            {
                Animation.Animatable? anim = _element as Animation.Animatable;

                if (anim != null)
                {
                    backupTransitions = anim.Transitions;
                    anim.Transitions = null;
                }
            }

            _element.RenderTransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Relative);
            _transformBuilder = new TransformOperations.Builder(1);
            _transformBuilder.AppendMatrix(_matrix);
            _element.RenderTransform = _transformBuilder.Build();

            if (skipTransitions && backupTransitions != null)
            {
                Animation.Animatable? anim = _element as Animation.Animatable;

                if (anim != null)
                {
                    anim.Transitions = backupTransitions;
                }
            }

            _element.InvalidateVisual();
        }

        /// <summary>
        /// Set pan and zoom matrix.
        /// </summary>
        /// <param name="matrix">The matrix to set as current.</param>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void SetMatrix(Matrix matrix, bool skipTransitions = false)
        {
            _matrix = matrix;
            Invalidate(true);
        }

        /// <summary>
        /// Reset pan and zoom matrix.
        /// </summary>
        public void ResetMatrix()
        {
            ResetMatrix(false);
        }

        /// <summary>
        /// Reset pan and zoom matrix.
        /// </summary>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void ResetMatrix(bool skipTransitions)
        {
            SetMatrix(Matrix.Identity, skipTransitions);
        }

        /// <summary>
        /// Zoom to provided zoom value and provided center point.
        /// </summary>
        /// <param name="zoom">The zoom value.</param>
        /// <param name="x">The center point x axis coordinate.</param>
        /// <param name="y">The center point y axis coordinate.</param>
        /// <param name="invalidateScroll">The flag indicating whether to invalidate scroll.</param>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void Zoom(double zoom, double x, double y, bool invalidateScroll, bool skipTransitions = false)
        {
            _matrix = MatrixHelper.ScaleAt(zoom, zoom, x, y);
            Invalidate(invalidateScroll, skipTransitions);
        }

        /// <summary>
        /// Zoom to provided zoom ratio and provided center point.
        /// </summary>
        /// <param name="ratio">The zoom ratio.</param>
        /// <param name="x">The center point x axis coordinate.</param>
        /// <param name="y">The center point y axis coordinate.</param>
        /// <param name="invalidateScroll">The flag indicating whether to invalidate scroll.</param>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void ZoomTo(double ratio, double x, double y, bool invalidateScroll, bool skipTransitions = false)
        {
            _matrix = MatrixHelper.ScaleAtPrepend(_matrix, ratio, ratio, x, y);
            Invalidate(invalidateScroll, skipTransitions);
        }

        /// <summary>
        /// Zoom in one step positive delta ratio and panel center point.
        /// </summary>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void ZoomIn(bool skipTransitions = false)
        {
            if (_element == null)
            {
                return;
            }
            var x = _element.Bounds.Width / 2.0;
            var y = _element.Bounds.Height / 2.0;
            ZoomTo(ZoomSpeed, x, y, true, skipTransitions);
        }

        /// <summary>
        /// Zoom out one step positive delta ratio and panel center point.
        /// </summary>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void ZoomOut(bool skipTransitions = false)
        {
            if (_element == null)
            {
                return;
            }
            var x = _element.Bounds.Width / 2.0;
            var y = _element.Bounds.Height / 2.0;
            ZoomTo(1 / ZoomSpeed, x, y, true, skipTransitions);
        }

        /// <summary>
        /// Zoom to provided zoom delta ratio and provided center point.
        /// </summary>
        /// <param name="delta">The zoom delta ratio.</param>
        /// <param name="x">The center point x axis coordinate.</param>
        /// <param name="y">The center point y axis coordinate.</param>
        /// <param name="invalidateScroll">The flag indicating whether to invalidate scroll.</param>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void ZoomDeltaTo(double delta, double x, double y, bool invalidateScroll, bool skipTransitions = false)
        {
            ZoomTo(delta > 0 ? ZoomSpeed : 1 / ZoomSpeed, x, y, invalidateScroll, skipTransitions);
        }

        /// <summary>
        /// Pan control to provided delta.
        /// </summary>
        /// <param name="dx">The target x axis delta.</param>
        /// <param name="dy">The target y axis delta.</param>
        /// <param name="invalidateScroll">The flag indicating whether to invalidate scroll.</param>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void PanDelta(double dx, double dy, bool invalidateScroll, bool skipTransitions = false)
        {
            _matrix = MatrixHelper.ScaleAndTranslate(_zoomX, _zoomY, _matrix.M31 + dx, _matrix.M32 + dy);
            Invalidate(invalidateScroll, skipTransitions);
        }

        /// <summary>
        /// Pan control to provided target point.
        /// </summary>
        /// <param name="x">The target point x axis coordinate.</param>
        /// <param name="y">The target point y axis coordinate.</param>
        /// <param name="invalidateScroll">The flag indicating whether to invalidate scroll.</param>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void Pan(double x, double y, bool invalidateScroll, bool skipTransitions = false)
        {
            _matrix = MatrixHelper.ScaleAndTranslate(_zoomX, _zoomY, x, y);
            Invalidate(invalidateScroll, skipTransitions);
        }

        /// <summary>
        /// Set pan origin.
        /// </summary>
        /// <param name="x">The origin point x axis coordinate.</param>
        /// <param name="y">The origin point y axis coordinate.</param>
        public void BeginPanTo(double x, double y)
        {
            _pan = new Point();
            _previous = new Point(x, y);
        }

        /// <summary>
        /// Continue pan to provided target point.
        /// </summary>
        /// <param name="x">The target point x axis coordinate.</param>
        /// <param name="y">The target point y axis coordinate.</param>
        /// <param name="invalidateScroll">The flag indicating whether to invalidate scroll.</param>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void ContinuePanTo(double x, double y, bool invalidateScroll, bool skipTransitions = false)
        {
            var dx = x - _previous.X;
            var dy = y - _previous.Y;
            var delta = new Point(dx, dy);
            _previous = new Point(x, y);
            _pan = new Point(_pan.X + delta.X, _pan.Y + delta.Y);
            _matrix = MatrixHelper.TranslatePrepend(_matrix, _pan.X, _pan.Y);
            Invalidate(invalidateScroll, skipTransitions);
        }

        /// <summary>
        /// Zoom and pan.
        /// </summary>
        /// <param name="panelWidth">The panel width.</param>
        /// <param name="panelHeight">The panel height.</param>
        /// <param name="elementWidth">The element width.</param>
        /// <param name="elementHeight">The element height.</param>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void None(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
        {
            Log($"[None] {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
            if (_element == null)
            {
                return;
            }
            _matrix = ZoomHelper.CalculateMatrix(panelWidth, panelHeight, elementWidth, elementHeight, StretchMode.None);
            Invalidate(true, skipTransitions);
        }

        /// <summary>
        /// Zoom and pan to fill panel.
        /// </summary>
        /// <param name="panelWidth">The panel width.</param>
        /// <param name="panelHeight">The panel height.</param>
        /// <param name="elementWidth">The element width.</param>
        /// <param name="elementHeight">The element height.</param>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void Fill(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
        {
            Log($"[Fill] {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
            if (_element == null)
            {
                return;
            }
            _matrix = ZoomHelper.CalculateMatrix(panelWidth, panelHeight, elementWidth, elementHeight, StretchMode.Fill);
            Invalidate(true, skipTransitions);
        }

        /// <summary>
        /// Zoom and pan to panel extents while maintaining aspect ratio.
        /// </summary>
        /// <param name="panelWidth">The panel width.</param>
        /// <param name="panelHeight">The panel height.</param>
        /// <param name="elementWidth">The element width.</param>
        /// <param name="elementHeight">The element height.</param>
        public void Uniform(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            Uniform(panelWidth, panelHeight, elementWidth, elementHeight, false);
        }

        /// <summary>
        /// Zoom and pan to panel extents while maintaining aspect ratio.
        /// </summary>
        /// <param name="panelWidth">The panel width.</param>
        /// <param name="panelHeight">The panel height.</param>
        /// <param name="elementWidth">The element width.</param>
        /// <param name="elementHeight">The element height.</param>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void Uniform(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions)
        {
            Log($"[Uniform] {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
            if (_element == null)
            {
                return;
            }
            _matrix = ZoomHelper.CalculateMatrix(panelWidth, panelHeight, elementWidth, elementHeight, StretchMode.Uniform);
            Invalidate(true, skipTransitions);
        }

        /// <summary>
        /// Zoom and pan to panel extents while maintaining aspect ratio. If aspect of panel is different panel is filled.
        /// </summary>
        /// <param name="panelWidth">The panel width.</param>
        /// <param name="panelHeight">The panel height.</param>
        /// <param name="elementWidth">The element width.</param>
        /// <param name="elementHeight">The element height.</param>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void UniformToFill(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
        {
            Log($"[UniformToFill] {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
            if (_element == null)
            {
                return;
            }
            _matrix = ZoomHelper.CalculateMatrix(panelWidth, panelHeight, elementWidth, elementHeight, StretchMode.UniformToFill);
            Invalidate(true, skipTransitions);
        }

        /// <summary>
        /// Zoom and pan child element inside panel using stretch mode.
        /// </summary>
        /// <param name="panelWidth">The panel width.</param>
        /// <param name="panelHeight">The panel height.</param>
        /// <param name="elementWidth">The element width.</param>
        /// <param name="elementHeight">The element height.</param>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void AutoFit(double panelWidth, double panelHeight, double elementWidth, double elementHeight, bool skipTransitions = false)
        {
            Log($"[AutoFit] {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
            if (_element == null)
            {
                return;
            }
            switch (Stretch)
            {
                case StretchMode.Fill:
                    Fill(panelWidth, panelHeight, elementWidth, elementHeight);
                    break;
                case StretchMode.Uniform:
                    Uniform(panelWidth, panelHeight, elementWidth, elementHeight);
                    break;
                case StretchMode.UniformToFill:
                    UniformToFill(panelWidth, panelHeight, elementWidth, elementHeight);
                    break;
                case StretchMode.None:
                    break;
            }
            Invalidate(true, skipTransitions);
        }

        /// <summary>
        /// Set next stretch mode.
        /// </summary>
        public void ToggleStretchMode()
        {
            switch (Stretch)
            {
                case StretchMode.None:
                    Stretch = StretchMode.Fill;
                    break;
                case StretchMode.Fill:
                    Stretch = StretchMode.Uniform;
                    break;
                case StretchMode.Uniform:
                    Stretch = StretchMode.UniformToFill;
                    break;
                case StretchMode.UniformToFill:
                    Stretch = StretchMode.None;
                    break;
            }
        }

        /// <summary>
        /// Zoom and pan.
        /// </summary>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void None(bool skipTransitions = false)
        {
            if (_element == null)
            {
                return;
            }
            None(Bounds.Width, Bounds.Height, _element.Bounds.Width, _element.Bounds.Height, skipTransitions);
        }

        /// <summary>
        /// Zoom and pan to fill panel.
        /// </summary>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void Fill(bool skipTransitions = false)
        {
            if (_element == null)
            {
                return;
            }
            Fill(Bounds.Width, Bounds.Height, _element.Bounds.Width, _element.Bounds.Height, skipTransitions);
        }

        /// <summary>
        /// Zoom and pan to panel extents while maintaining aspect ratio.
        /// </summary>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void Uniform(bool skipTransitions = false)
        {
            if (_element == null)
            {
                return;
            }
            Uniform(Bounds.Width, Bounds.Height, _element.Bounds.Width, _element.Bounds.Height, skipTransitions);
        }

        /// <summary>
        /// Zoom and pan to panel extents while maintaining aspect ratio. If aspect of panel is different panel is filled.
        /// </summary>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void UniformToFill(bool skipTransitions = false)
        {
            if (_element == null)
            {
                return;
            }
            UniformToFill(Bounds.Width, Bounds.Height, _element.Bounds.Width, _element.Bounds.Height, skipTransitions);
        }

        /// <summary>
        /// Zoom and pan child element inside panel using stretch mode.
        /// </summary>
        /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
        public void AutoFit(bool skipTransitions = false)
        {
            if (_element == null)
            {
                return;
            }
            AutoFit(Bounds.Width, Bounds.Height, _element.Bounds.Width, _element.Bounds.Height, skipTransitions);
        }
    }
}
