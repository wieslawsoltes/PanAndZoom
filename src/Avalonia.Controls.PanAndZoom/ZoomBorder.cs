// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using static System.Math;

namespace Avalonia.Controls.PanAndZoom
{
    /// <summary>
    /// Pan and zoom control for Avalonia.
    /// </summary>
    public class ZoomBorder : Border
    {
        private static AutoFitMode[] _autoFitModes = (AutoFitMode[])Enum.GetValues(typeof(AutoFitMode));

        /// <summary>
        /// Gets available auto-fit modes.
        /// </summary>
        public static AutoFitMode[] AutoFitModes => _autoFitModes;

        private IControl _element;
        private Point _pan;
        private Point _previous;
        private Matrix _matrix;
        private bool _isPanning;

        /// <summary>
        /// Gets or sets invalidate action for border child element.
        /// </summary>
        public Action<double, double, double, double> InvalidatedChild { get; set; }

        /// <summary>
        /// Gets or sets zoom speed ratio.
        /// </summary>
        public double ZoomSpeed
        {
            get { return GetValue(ZoomSpeedProperty); }
            set { SetValue(ZoomSpeedProperty, value); }
        }

        /// <summary>
        /// Gets or sets auto-fir mode.
        /// </summary>
        public AutoFitMode AutoFitMode
        {
            get { return GetValue(AutoFitModeProperty); }
            set { SetValue(AutoFitModeProperty, value); }
        }

        /// <summary>
        /// Identifies the <seealso cref="ZoomSpeed"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<double> ZoomSpeedProperty =
            AvaloniaProperty.Register<ZoomBorder, double>(nameof(ZoomSpeed), 1.2, false, BindingMode.TwoWay);

        /// <summary>
        /// Identifies the <seealso cref="AutoFitMode"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<AutoFitMode> AutoFitModeProperty =
            AvaloniaProperty.Register<ZoomBorder, AutoFitMode>(nameof(AutoFitMode), AutoFitMode.Extent, false, BindingMode.TwoWay);

        static ZoomBorder()
        {
            AffectsArrange(ZoomSpeedProperty, AutoFitModeProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomBorder"/> class.
        /// </summary>
        public ZoomBorder()
            : base()
        {
            _isPanning = false;
            _matrix = MatrixHelper.Identity;

            Focusable = true;
            Background = Brushes.Transparent;

            DetachedFromVisualTree += PanAndZoom_DetachedFromVisualTree;

            this.GetObservable(ChildProperty).Subscribe(value =>
            {
                if (value != null && value != _element && _element != null)
                {
                    Unload();
                }

                if (value != null && value != _element)
                {
                    Initialize(value);
                }
            });
        }

        private void PanAndZoom_DetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            if (_element != null)
            {
                Unload();
            }
        }

        private void Initialize(IControl element)
        {
            if (element != null)
            {
                _element = element;
                this.PointerWheelChanged += Border_PointerWheelChanged;
                this.PointerPressed += Border_PointerPressed;
                this.PointerReleased += Border_PointerReleased;
                this.PointerMoved += Border_PointerMoved;
            }
        }

        private void Unload()
        {
            if (_element != null)
            {
                this.PointerWheelChanged -= Border_PointerWheelChanged;
                this.PointerPressed -= Border_PointerPressed;
                this.PointerReleased -= Border_PointerReleased;
                this.PointerMoved -= Border_PointerMoved;
                _element.RenderTransform = null;
                _element = null;
            }
        }

        private void Border_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            if (_element != null && e.Device.Captured == null)
            {
                Point point = e.GetPosition(_element);
                point = FixInvalidPointPosition(point);
                ZoomDeltaTo(e.Delta.Y, point);
                //e.Handled = true;
            }
        }

        private void Border_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            switch (e.MouseButton)
            {
                case MouseButton.Right:
                    {
                        if (_element != null && e.Device.Captured == null && _isPanning == false)
                        {
                            Point point = e.GetPosition(_element);
                            point = FixInvalidPointPosition(point);
                            StartPan(point);
                            e.Device.Capture(_element);
                            //e.Handled = true;
                            _isPanning = true;
                        }
                    }
                    break;
            }
        }

        private void Border_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (_element != null)
            {
                switch (e.MouseButton)
                {
                    case MouseButton.Right:
                        {
                            if (_element != null && e.Device.Captured == _element && _isPanning == true)
                            {
                                e.Device.Capture(null);
                                //e.Handled = true;
                                _isPanning = false;
                            }
                        }
                        break;
                }
            }
        }

        private void Border_PointerMoved(object sender, PointerEventArgs e)
        {
            if (_element != null && e.Device.Captured == _element && _isPanning == true)
            {
                Point point = e.GetPosition(_element);
                point = FixInvalidPointPosition(point);
                PanTo(point);
                //e.Handled = true;
            }
        }

        /// <summary>
        /// Arranges the control's child.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = base.ArrangeOverride(finalSize);

            if (_element != null && _element.IsMeasureValid)
            {
                AutoFit(new Rect(0.0, 0.0, finalSize.Width, finalSize.Height), _element.Bounds);
            }

            return size;
        }

        /// <summary>
        /// Invalidate child element.
        /// </summary>
        public void Invalidate()
        {
            if (_element != null)
            {
                this.InvalidatedChild?.Invoke(_matrix.M11, _matrix.M12, _matrix.M31, _matrix.M32);
                _element.RenderTransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Relative);
                _element.RenderTransform = new MatrixTransform(_matrix);
                _element.InvalidateVisual();
            }
        }

        /// <summary>
        /// Zoom to provided zoom ratio and provided center point.
        /// </summary>
        /// <param name="zoom">The zoom ratio.</param>
        /// <param name="point">The center point.</param>
        public void ZoomTo(double zoom, Point point)
        {
            _matrix = MatrixHelper.ScaleAtPrepend(_matrix, zoom, zoom, point.X, point.Y);

            Invalidate();
        }

        /// <summary>
        /// Zoom to provided zoom delta ratio and provided center point.
        /// </summary>
        /// <param name="delta">The zoom delta ratio.</param>
        /// <param name="point">The center point.</param>
        public void ZoomDeltaTo(double delta, Point point)
        {
            ZoomTo(delta > 0 ? ZoomSpeed : 1 / ZoomSpeed, point);
        }

        /// <summary>
        /// Set pan origin.
        /// </summary>
        /// <param name="point">The pan origin position.</param>
        public void StartPan(Point point)
        {
            _pan = new Point();
            _previous = new Point(point.X, point.Y);
        }

        /// <summary>
        /// Pan control to provided position.
        /// </summary>
        /// <param name="point">The pan destination position.</param>
        public void PanTo(Point point)
        {
            Point delta = new Point(point.X - _previous.X, point.Y - _previous.Y);
            _previous = new Point(point.X, point.Y);

            _pan = new Point(_pan.X + delta.X, _pan.Y + delta.Y);
            _matrix = MatrixHelper.TranslatePrepend(_matrix, _pan.X, _pan.Y);

            Invalidate();
        }

        /// <summary>
        /// Zoom and pan to panel extents while maintaining aspect ratio.
        /// </summary>
        /// <param name="panelSize">The panel size.</param>
        /// <param name="elementSize">The element size.</param>
        public void Extent(Rect panelSize, Rect elementSize)
        {
            if (_element != null)
            {
                double pw = panelSize.Width;
                double ph = panelSize.Height;
                double ew = elementSize.Width;
                double eh = elementSize.Height;
                double zx = pw / ew;
                double zy = ph / eh;
                double zoom = Min(zx, zy);
                double cx = ew / 2.0;
                double cy = eh / 2.0;

                _matrix = MatrixHelper.ScaleAt(zoom, zoom, cx, cy);

                Invalidate();
            }
        }

        /// <summary>
        /// Zoom and pan to fill panel.
        /// </summary>
        /// <param name="panelSize">The panel size.</param>
        /// <param name="elementSize">The element size.</param>
        public void Fill(Rect panelSize, Rect elementSize)
        {
            if (_element != null)
            {
                double pw = panelSize.Width;
                double ph = panelSize.Height;
                double ew = elementSize.Width;
                double eh = elementSize.Height;
                double zx = pw / ew;
                double zy = ph / eh;

                _matrix = MatrixHelper.ScaleAt(zx, zy, ew / 2.0, eh / 2.0);

                Invalidate();
            }
        }

        /// <summary>
        /// Zoom and pan child elemnt inside panel using auto-fit mode.
        /// </summary>
        /// <param name="panelSize">The panel size.</param>
        /// <param name="elementSize">The element size.</param>
        public void AutoFit(Rect panelSize, Rect elementSize)
        {
            if (_element != null)
            {
                switch (AutoFitMode)
                {
                    case AutoFitMode.Extent:
                        Extent(panelSize, elementSize);
                        break;
                    case AutoFitMode.Fill:
                        Fill(panelSize, elementSize);
                        break;
                }

                Invalidate();
            }
        }

        /// <summary>
        /// Toggle next auto-fit mode.
        /// </summary>
        public void ToggleAutoFitMode()
        {
            switch (AutoFitMode)
            {
                case AutoFitMode.None:
                    AutoFitMode = AutoFitMode.Extent;
                    break;
                case AutoFitMode.Extent:
                    AutoFitMode = AutoFitMode.Fill;
                    break;
                case AutoFitMode.Fill:
                    AutoFitMode = AutoFitMode.None;
                    break;
            }
        }

        /// <summary>
        /// Reset pan and zoom matrix.
        /// </summary>
        public void Reset()
        {
            _matrix = MatrixHelper.Identity;

            Invalidate();
        }

        /// <summary>
        /// Zoom and pan to panel extents while maintaining aspect ratio.
        /// </summary>
        public void Extent()
        {
            Extent(this.Bounds, _element.Bounds);
        }

        /// <summary>
        /// Zoom and pan to fill panel.
        /// </summary>
        public void Fill()
        {
            Fill(this.Bounds, _element.Bounds);
        }

        /// <summary>
        /// Zoom and pan child elemnt inside panel using auto-fit mode.
        /// </summary>
        public void AutoFit()
        {
            if (_element != null)
            {
                AutoFit(this.Bounds, _element.Bounds);
            }
        }

        /// <summary>
        /// Fix point position using current render transform matrix.
        /// </summary>
        /// <param name="point">The point to fix.</param>
        /// <returns>The fixed point.</returns>
        public Point FixInvalidPointPosition(Point point)
        {
            return MatrixHelper.TransformPoint(_matrix.Invert(), point);
        }
    }
}
