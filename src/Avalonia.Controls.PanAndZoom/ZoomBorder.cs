// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using static System.Math;
using PanAndZoom;

namespace Avalonia.Controls.PanAndZoom
{
    /// <summary>
    /// Pan and zoom control for Avalonia.
    /// </summary>
    public class ZoomBorder : Border, IPanAndZoom
    {
        private IControl _element;
        private Point _pan;
        private Point _previous;
        private Matrix _matrix;
        private bool _isPanning;
        private static StretchMode[] _autoFitModes = (StretchMode[])Enum.GetValues(typeof(StretchMode));

        /// <summary>
        /// Gets available stretch modes.
        /// </summary>
        public static StretchMode[] StretchModes => _autoFitModes;

        /// <inheritdoc/>
        public Action<double, double, double, double> InvalidatedChild { get; set; }

        /// <inheritdoc/>
        public double ZoomSpeed
        {
            get { return GetValue(ZoomSpeedProperty); }
            set { SetValue(ZoomSpeedProperty, value); }
        }

        /// <inheritdoc/>
        public StretchMode Stretch
        {
            get { return GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <summary>
        /// Identifies the <seealso cref="ZoomSpeed"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<double> ZoomSpeedProperty =
            AvaloniaProperty.Register<ZoomBorder, double>(nameof(ZoomSpeed), 1.2, false, BindingMode.TwoWay);

        /// <summary>
        /// Identifies the <seealso cref="Stretch"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<StretchMode> StretchProperty =
            AvaloniaProperty.Register<ZoomBorder, StretchMode>(nameof(Stretch), StretchMode.Uniform, false, BindingMode.TwoWay);

        static ZoomBorder()
        {
            AffectsArrange(ZoomSpeedProperty, StretchProperty);
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

            this.GetObservable(ChildProperty).Subscribe(ChildChanged);
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
                AutoFit(
                    size.Width,
                    size.Height,
                    _element.Bounds.Width,
                    _element.Bounds.Height);
            }

            return size;
        }

        private void PanAndZoom_DetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            Unload();
        }

        private void Border_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            Wheel(e);
        }

        private void Border_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            Pressed(e);
        }

        private void Border_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            Released(e);
        }

        private void Border_PointerMoved(object sender, PointerEventArgs e)
        {
            Moved(e);
        }

        private void ChildChanged(IControl element)
        {
            if (element != null && element != _element && _element != null)
            {
                Unload();
            }

            if (element != null && element != _element)
            {
                Initialize(element);
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

        private void Wheel(PointerWheelEventArgs e)
        {
            if (_element != null && e.Device.Captured == null)
            {
                Point point = e.GetPosition(_element);
                point = FixInvalidPointPosition(point);
                ZoomDeltaTo(e.Delta.Y, point.X, point.Y);
            }
        }

        private void Pressed(PointerPressedEventArgs e)
        {
            switch (e.MouseButton)
            {
                case MouseButton.Right:
                    {
                        if (_element != null && e.Device.Captured == null && _isPanning == false)
                        {
                            Point point = e.GetPosition(_element);
                            point = FixInvalidPointPosition(point);
                            StartPan(point.X, point.Y);
                            e.Device.Capture(_element);
                            _isPanning = true;
                        }
                    }
                    break;
            }
        }

        private void Released(PointerReleasedEventArgs e)
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
                                _isPanning = false;
                            }
                        }
                        break;
                }
            }
        }

        private void Moved(PointerEventArgs e)
        {
            if (_element != null && e.Device.Captured == _element && _isPanning == true)
            {
                Point point = e.GetPosition(_element);
                point = FixInvalidPointPosition(point);
                PanTo(point.X, point.Y);
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void ZoomTo(double zoom, double x, double y)
        {
            _matrix = MatrixHelper.ScaleAtPrepend(_matrix, zoom, zoom, x, y);
            Invalidate();
        }

        /// <inheritdoc/>
        public void ZoomDeltaTo(double delta, double x, double y)
        {
            ZoomTo(delta > 0 ? ZoomSpeed : 1 / ZoomSpeed, x, y);
        }

        /// <inheritdoc/>
        public void StartPan(double x, double y)
        {
            _pan = new Point();
            _previous = new Point(x, y);
        }

        /// <inheritdoc/>
        public void PanTo(double x, double y)
        {
            double dx = x - _previous.X;
            double dy = y - _previous.Y;
            Point delta = new Point(dx, dy);
            _previous = new Point(x, y);
            _pan = new Point(_pan.X + delta.X, _pan.Y + delta.Y);
            _matrix = MatrixHelper.TranslatePrepend(_matrix, _pan.X, _pan.Y);
            Invalidate();
        }

        /// <inheritdoc/>
        public void Fill(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            if (_element != null)
            {
                double zx = panelWidth / elementWidth;
                double zy = panelHeight / elementHeight;
                double cx = elementWidth / 2.0;
                double cy = elementHeight / 2.0;
                _matrix = MatrixHelper.ScaleAt(zx, zy, cx, cy);
                Invalidate();
            }
        }

        /// <inheritdoc/>
        public void Uniform(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            if (_element != null)
            {
                double zx = panelWidth / elementWidth;
                double zy = panelHeight / elementHeight;
                double zoom = Min(zx, zy);
                double cx = elementWidth / 2.0;
                double cy = elementHeight / 2.0;
                _matrix = MatrixHelper.ScaleAt(zoom, zoom, cx, cy);
                Invalidate();
            }
        }

        /// <inheritdoc/>
        public void UniformToFill(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            if (_element != null)
            {
                double zx = panelWidth / elementWidth;
                double zy = panelHeight / elementHeight;
                double zoom = Max(zx, zy);
                double cx = elementWidth / 2.0;
                double cy = elementHeight / 2.0;
                _matrix = MatrixHelper.ScaleAt(zoom, zoom, cx, cy);
                Invalidate();
            }
        }

        /// <inheritdoc/>
        public void AutoFit(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            if (_element != null)
            {
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

                }
                Invalidate();
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void Reset()
        {
            _matrix = MatrixHelper.Identity;
            Invalidate();
        }

        /// <inheritdoc/>
        public void Fill()
        {
            Fill(
                this.Bounds.Width,
                this.Bounds.Height,
                _element.Bounds.Width,
                _element.Bounds.Height);
        }

        /// <inheritdoc/>
        public void Uniform()
        {
            Uniform(
                this.Bounds.Width,
                this.Bounds.Height,
                _element.Bounds.Width,
                _element.Bounds.Height);
        }

        /// <inheritdoc/>
        public void UniformToFill()
        {
            UniformToFill(
                this.Bounds.Width,
                this.Bounds.Height,
                _element.Bounds.Width,
                _element.Bounds.Height);
        }

        /// <inheritdoc/>
        public void AutoFit()
        {
            if (_element != null)
            {
                AutoFit(
                    this.Bounds.Width,
                    this.Bounds.Height,
                    _element.Bounds.Width,
                    _element.Bounds.Height);
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
