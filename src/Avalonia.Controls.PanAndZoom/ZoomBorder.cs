// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using static System.Math;
using PanAndZoom;
using System.Diagnostics;

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

        /// <inheritdoc/>
        public double ZoomX => _matrix.M11;

        /// <inheritdoc/>
        public double ZoomY => _matrix.M22;

        /// <inheritdoc/>
        public double OffsetX => _matrix.M31;

        /// <inheritdoc/>
        public double OffsetY => _matrix.M32;

        /// <inheritdoc/>
        public bool EnableConstrains { get; set; }

        /// <inheritdoc/>
        public double MinZoomX { get; set; }

        /// <inheritdoc/>
        public double MaxZoomX { get; set; }

        /// <inheritdoc/>
        public double MinZoomY { get; set; }

        /// <inheritdoc/>
        public double MaxZoomY { get; set; }

        /// <inheritdoc/>
        public double MinOffsetX { get; set; }

        /// <inheritdoc/>
        public double MaxOffsetX { get; set; }

        /// <inheritdoc/>
        public double MinOffsetY { get; set; }

        /// <inheritdoc/>
        public double MaxOffsetY { get; set; }

        /// <inheritdoc/>
        public bool EnableInput
        {
            get { return GetValue(EnableInputProperty); }
            set { SetValue(EnableInputProperty, value); }
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

        /// <summary>
        /// Identifies the <seealso cref="EnableInput"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<bool> EnableInputProperty =
            AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableInput), true, false, BindingMode.TwoWay);

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
            Defaults();

            EnableConstrains = true;

            MinZoomX = double.NegativeInfinity;
            MaxZoomX = double.PositiveInfinity;
            MinZoomY = double.NegativeInfinity;
            MaxZoomY = double.PositiveInfinity;
            MinOffsetX = double.NegativeInfinity;
            MaxOffsetX = double.PositiveInfinity;
            MinOffsetY = double.NegativeInfinity;
            MaxOffsetY = double.PositiveInfinity;

            Focusable = true;
            Background = Brushes.Transparent;

            AttachedToVisualTree += PanAndZoom_AttachedToVisualTree;
            DetachedFromVisualTree += PanAndZoom_DetachedFromVisualTree;

            this.GetObservable(ChildProperty).Subscribe(ChildChanged);
        }

        private void Defaults()
        {
            _isPanning = false;
            _matrix = MatrixHelper.Identity;
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

        private void PanAndZoom_AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            Debug.WriteLine($"AttachedToVisualTree: {this.Name}");
            ChildChanged(base.Child);
        }

        private void PanAndZoom_DetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            Debug.WriteLine($"DetachedFromVisualTree: {this.Name}");
            DetachElement();
        }

        private void Border_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            if (EnableInput)
            {
                Wheel(e);
            }
        }

        private void Border_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (EnableInput)
            {
                Pressed(e);
            }
        }

        private void Border_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (EnableInput)
            {
                Released(e);
            }
        }

        private void Border_PointerMoved(object sender, PointerEventArgs e)
        {
            if (EnableInput)
            {
                Moved(e);
            }
        }

        private void ChildChanged(IControl element)
        {
            if (element != null && element != _element && _element != null)
            {
                DetachElement();
            }

            if (element != null && element != _element)
            {
                AttachElement(element);
            }
        }

        private void AttachElement(IControl element)
        {
            if (element != null)
            {
                Defaults();
                _element = element;
                this.PointerWheelChanged += Border_PointerWheelChanged;
                this.PointerPressed += Border_PointerPressed;
                this.PointerReleased += Border_PointerReleased;
                this.PointerMoved += Border_PointerMoved;
            }
        }

        private void DetachElement()
        {
            if (_element != null)
            {
                this.PointerWheelChanged -= Border_PointerWheelChanged;
                this.PointerPressed -= Border_PointerPressed;
                this.PointerReleased -= Border_PointerReleased;
                this.PointerMoved -= Border_PointerMoved;
                _element.RenderTransform = null;
                _element = null;
                Defaults();
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

        private double Constrain(double value, double minimum, double maximum)
        {
            if (minimum > maximum)
                throw new ArgumentException($"Parameter {nameof(minimum)} is greater than {nameof(maximum)}.");

            if (maximum < minimum)
                throw new ArgumentException($"Parameter {nameof(maximum)} is lower than {nameof(minimum)}.");

            return Math.Min(Math.Max(value, minimum), maximum);
        }

        private void Constrain()
        {
            double zoomX = Constrain(_matrix.M11, MinZoomX, MaxZoomX);
            double zoomY = Constrain(_matrix.M22, MinZoomY, MaxZoomY);
            double offsetX = Constrain(_matrix.M31, MinOffsetX, MaxOffsetX);
            double offsetY = Constrain(_matrix.M32, MinOffsetY, MaxOffsetY);
            _matrix = new Matrix(zoomX, 0.0, 0.0, zoomY, offsetX, offsetY);
        }

        /// <inheritdoc/>
        public void Invalidate()
        {
            if (_element != null)
            {
                if (EnableConstrains == true)
                {
                    Constrain();
                }
                Debug.WriteLine($"Zoom: {_matrix.M11} {_matrix.M22} Offset: {_matrix.M31} {_matrix.M32}");
                this.InvalidatedChild?.Invoke(_matrix.M11, _matrix.M22, _matrix.M31, _matrix.M32);
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

        private Matrix GetMatrix(double panelWidth, double panelHeight, double elementWidth, double elementHeight, StretchMode mode)
        {
            double zx = panelWidth / elementWidth;
            double zy = panelHeight / elementHeight;
            double cx = elementWidth / 2.0;
            double cy = elementHeight / 2.0;
            double zoom = 1.0;
            switch (mode)
            {
                case StretchMode.Fill:
                    return MatrixHelper.ScaleAt(zx, zy, cx, cy);
                case StretchMode.Uniform:
                    zoom = Min(zx, zy);
                    return MatrixHelper.ScaleAt(zoom, zoom, cx, cy);
                case StretchMode.UniformToFill:
                    zoom = Max(zx, zy);
                    return MatrixHelper.ScaleAt(zoom, zoom, cx, cy);
            }
            return MatrixHelper.Identity;
        }

        /// <inheritdoc/>
        public void Fill(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            Debug.WriteLine($"Fill: {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
            if (_element != null)
            {
                _matrix = GetMatrix(panelWidth, panelHeight, elementWidth, elementHeight, StretchMode.Fill);
                Invalidate();
            }
        }

        /// <inheritdoc/>
        public void Uniform(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            Debug.WriteLine($"Uniform: {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
            if (_element != null)
            {
                _matrix = GetMatrix(panelWidth, panelHeight, elementWidth, elementHeight, StretchMode.Uniform);
                Invalidate();
            }
        }

        /// <inheritdoc/>
        public void UniformToFill(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            Debug.WriteLine($"UniformToFill: {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
            if (_element != null)
            {
                _matrix = GetMatrix(panelWidth, panelHeight, elementWidth, elementHeight, StretchMode.UniformToFill);
                Invalidate();
            }
        }

        /// <inheritdoc/>
        public void AutoFit(double panelWidth, double panelHeight, double elementWidth, double elementHeight)
        {
            Debug.WriteLine($"AutoFit: {panelWidth}x{panelHeight} {elementWidth}x{elementHeight}");
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
            Fill(this.Bounds.Width, this.Bounds.Height, _element.Bounds.Width, _element.Bounds.Height);
        }

        /// <inheritdoc/>
        public void Uniform()
        {
            Uniform(this.Bounds.Width, this.Bounds.Height, _element.Bounds.Width, _element.Bounds.Height);
        }

        /// <inheritdoc/>
        public void UniformToFill()
        {
            UniformToFill(this.Bounds.Width, this.Bounds.Height, _element.Bounds.Width, _element.Bounds.Height);
        }

        /// <inheritdoc/>
        public void AutoFit()
        {
            if (_element != null)
            {
                AutoFit(this.Bounds.Width, this.Bounds.Height, _element.Bounds.Width, _element.Bounds.Height);
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
