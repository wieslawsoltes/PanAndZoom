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
        private double _zoomX = 1.0;
        private double _zoomY = 1.0;
        private double _offsetX = 0.0;
        private double _offsetY = 0.0;
        private static StretchMode[] _autoFitModes = (StretchMode[])Enum.GetValues(typeof(StretchMode));
        private static ButtonName[] _buttonNames = (ButtonName[])Enum.GetValues(typeof(ButtonName));

        /// <summary>
        /// Gets available stretch modes.
        /// </summary>
        public static StretchMode[] StretchModes => _autoFitModes;

        /// <summary>
        /// Gets available button names.
        /// </summary>
        public static ButtonName[] ButtonNames => _buttonNames;

        /// <inheritdoc/>
        public Action<double, double, double, double> InvalidatedChild { get; set; }

        /// <inheritdoc/>
        public ButtonName PanButton
        {
            get => GetValue(PanButtonProperty);
            set => SetValue(PanButtonProperty, value);
        }

        /// <inheritdoc/>
        public double ZoomSpeed
        {
            get => GetValue(ZoomSpeedProperty);
            set => SetValue(ZoomSpeedProperty, value);
        }

        /// <inheritdoc/>
        public StretchMode Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        /// <inheritdoc/>
        public double ZoomX => _zoomX;

        /// <inheritdoc/>
        public double ZoomY => _zoomY;

        /// <inheritdoc/>
        public double OffsetX => _offsetX;

        /// <inheritdoc/>
        public double OffsetY => _offsetY;

        /// <inheritdoc/>
        public bool EnableConstrains
        {
            get => GetValue(EnableConstrainsProperty);
            set => SetValue(EnableConstrainsProperty, value);
        }

        /// <inheritdoc/>
        public double MinZoomX
        {
            get => GetValue(MinZoomXProperty);
            set => SetValue(MinZoomXProperty, value);
        }

        /// <inheritdoc/>
        public double MaxZoomX
        {
            get => GetValue(MaxZoomXProperty);
            set => SetValue(MaxZoomXProperty, value);
        }

        /// <inheritdoc/>
        public double MinZoomY
        {
            get => GetValue(MinZoomYProperty);
            set => SetValue(MinZoomYProperty, value);
        }

        /// <inheritdoc/>
        public double MaxZoomY
        {
            get => GetValue(MaxZoomYProperty);
            set => SetValue(MaxZoomYProperty, value);
        }

        /// <inheritdoc/>
        public double MinOffsetX
        {
            get => GetValue(MinOffsetXProperty);
            set => SetValue(MinOffsetXProperty, value);
        }

        /// <inheritdoc/>
        public double MaxOffsetX
        {
            get => GetValue(MaxOffsetXProperty);
            set => SetValue(MaxOffsetXProperty, value);
        }

        /// <inheritdoc/>
        public double MinOffsetY
        {
            get => GetValue(MinOffsetYProperty);
            set => SetValue(MinOffsetYProperty, value);
        }

        /// <inheritdoc/>
        public double MaxOffsetY
        {
            get => GetValue(MaxOffsetYProperty);
            set => SetValue(MaxOffsetYProperty, value);
        }

        /// <inheritdoc/>
        public bool EnableInput
        {
            get => GetValue(EnableInputProperty);
            set => SetValue(EnableInputProperty, value);
        }

        /// <summary>
        /// Identifies the <seealso cref="PanButton"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<ButtonName> PanButtonProperty =
            AvaloniaProperty.Register<ZoomBorder, ButtonName>(nameof(PanButton), ButtonName.Left, false, BindingMode.TwoWay);

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
        /// Identifies the <seealso cref="ZoomX"/> avalonia property.
        /// </summary>
        public static readonly DirectProperty<ZoomBorder, double> ZoomXProperty =
            AvaloniaProperty.RegisterDirect<ZoomBorder, double>(
                nameof(ZoomX),
                o => o.ZoomX,
                null,
                1.0);

        /// <summary>
        /// Identifies the <seealso cref="ZoomY"/> avalonia property.
        /// </summary>
        public static readonly DirectProperty<ZoomBorder, double> ZoomYProperty =
            AvaloniaProperty.RegisterDirect<ZoomBorder, double>(
                nameof(ZoomY),
                o => o.ZoomY,
                null,
                1.0);

        /// <summary>
        /// Identifies the <seealso cref="OffsetX"/> avalonia property.
        /// </summary>
        public static readonly DirectProperty<ZoomBorder, double> OffsetXProperty =
            AvaloniaProperty.RegisterDirect<ZoomBorder, double>(
                nameof(OffsetX),
                o => o.OffsetX,
                null,
                0.0);

        /// <summary>
        /// Identifies the <seealso cref="OffsetY"/> avalonia property.
        /// </summary>
        public static readonly DirectProperty<ZoomBorder, double> OffsetYProperty =
            AvaloniaProperty.RegisterDirect<ZoomBorder, double>(
                nameof(OffsetY),
                o => o.OffsetY,
                null,
                0.0);

        /// <summary>
        /// Identifies the <seealso cref="EnableConstrains"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<bool> EnableConstrainsProperty =
            AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableConstrains), true, false, BindingMode.TwoWay);

        /// <summary>
        /// Identifies the <seealso cref="MinZoomX"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<double> MinZoomXProperty =
            AvaloniaProperty.Register<ZoomBorder, double>(nameof(MinZoomX), double.NegativeInfinity, false, BindingMode.TwoWay);

        /// <summary>
        /// Identifies the <seealso cref="MaxZoomX"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<double> MaxZoomXProperty =
            AvaloniaProperty.Register<ZoomBorder, double>(nameof(MaxZoomX), double.PositiveInfinity, false, BindingMode.TwoWay);

        /// <summary>
        /// Identifies the <seealso cref="MinZoomY"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<double> MinZoomYProperty =
            AvaloniaProperty.Register<ZoomBorder, double>(nameof(MinZoomY), double.NegativeInfinity, false, BindingMode.TwoWay);

        /// <summary>
        /// Identifies the <seealso cref="MaxZoomY"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<double> MaxZoomYProperty =
            AvaloniaProperty.Register<ZoomBorder, double>(nameof(MaxZoomY), double.PositiveInfinity, false, BindingMode.TwoWay);

        /// <summary>
        /// Identifies the <seealso cref="MinOffsetX"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<double> MinOffsetXProperty =
            AvaloniaProperty.Register<ZoomBorder, double>(nameof(MinOffsetX), double.NegativeInfinity, false, BindingMode.TwoWay);

        /// <summary>
        /// Identifies the <seealso cref="MaxOffsetX"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<double> MaxOffsetXProperty =
            AvaloniaProperty.Register<ZoomBorder, double>(nameof(MaxOffsetX), double.PositiveInfinity, false, BindingMode.TwoWay);

        /// <summary>
        /// Identifies the <seealso cref="MinOffsetY"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<double> MinOffsetYProperty =
            AvaloniaProperty.Register<ZoomBorder, double>(nameof(MinOffsetY), double.NegativeInfinity, false, BindingMode.TwoWay);

        /// <summary>
        /// Identifies the <seealso cref="MaxOffsetY"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<double> MaxOffsetYProperty =
            AvaloniaProperty.Register<ZoomBorder, double>(nameof(MaxOffsetY), double.PositiveInfinity, false, BindingMode.TwoWay);

        /// <summary>
        /// Identifies the <seealso cref="EnableInput"/> avalonia property.
        /// </summary>
        public static AvaloniaProperty<bool> EnableInputProperty =
            AvaloniaProperty.Register<ZoomBorder, bool>(nameof(EnableInput), true, false, BindingMode.TwoWay);

        static ZoomBorder()
        {
            AffectsArrange<ZoomBorder>(
                ZoomSpeedProperty,
                StretchProperty,
                EnableConstrainsProperty,
                MinZoomXProperty,
                MaxZoomXProperty,
                MinZoomYProperty,
                MaxZoomYProperty,
                MinOffsetXProperty,
                MaxOffsetXProperty,
                MinOffsetYProperty,
                MaxOffsetYProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomBorder"/> class.
        /// </summary>
        public ZoomBorder()
            : base()
        {
            Defaults();

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
                var button = PanButton;
                if ((e.MouseButton == MouseButton.Left && button == ButtonName.Left)
                    || (e.MouseButton == MouseButton.Right && button == ButtonName.Right)
                    || (e.MouseButton == MouseButton.Middle && button == ButtonName.Middle))
                {
                    Pressed(e);
                }
            }
        }

        private void Border_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (EnableInput)
            {
                var button = PanButton;
                if ((e.MouseButton == MouseButton.Left && button == ButtonName.Left)
                    || (e.MouseButton == MouseButton.Right && button == ButtonName.Right)
                    || (e.MouseButton == MouseButton.Middle && button == ButtonName.Middle))
                {
                    Released(e);
                }
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
                ZoomDeltaTo(e.Delta.Y, point.X, point.Y);
            }
        }

        private void Pressed(PointerPressedEventArgs e)
        {
            if (_element != null && e.Device.Captured == null && _isPanning == false)
            {
                Point point = e.GetPosition(_element);
                StartPan(point.X, point.Y);
                e.Device.Capture(_element);
                _isPanning = true;
            }
        }

        private void Released(PointerReleasedEventArgs e)
        {
            if (_element != null && e.Device.Captured == _element && _isPanning == true)
            {
                e.Device.Capture(null);
                _isPanning = false;
            }
        }

        private void Moved(PointerEventArgs e)
        {
            if (_element != null && e.Device.Captured == _element && _isPanning == true)
            {
                Point point = e.GetPosition(_element);
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
                double oldZoomX = _zoomX;
                double oldZoomY = _zoomY;
                double oldOffsetX = _offsetX;
                double oldOffsetY = _offsetY;
                _zoomX = _matrix.M11;
                _zoomY = _matrix.M22;
                _offsetX = _matrix.M31;
                _offsetY = _matrix.M32;
                RaisePropertyChanged(ZoomXProperty, oldZoomX, _zoomX);
                RaisePropertyChanged(ZoomYProperty, oldZoomY, _zoomY);
                RaisePropertyChanged(OffsetXProperty, oldOffsetX, _offsetX);
                RaisePropertyChanged(OffsetYProperty, oldOffsetY, _offsetY);
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
    }
}
