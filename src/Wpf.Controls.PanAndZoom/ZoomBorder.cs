// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PanAndZoom;
using Wpf.MatrixExtensions;
using static System.Math;

namespace Wpf.Controls.PanAndZoom
{
    /// <summary>
    /// Pan and zoom control for WPF.
    /// </summary>
    public class ZoomBorder : Border, IPanAndZoom
    {
        private UIElement? _element;
        private Point _pan;
        private Point _previous;
        private Matrix _matrix;
        private bool _isPanning;
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
        public Action<double, double, double, double>? InvalidatedChild { get; set; }

        /// <inheritdoc/>
        public ButtonName PanButton
        {
            get => (ButtonName)GetValue(PanButtonProperty);
            set => SetValue(PanButtonProperty, value);
        }

        /// <inheritdoc/>
        public double ZoomSpeed
        {
            get => (double)GetValue(ZoomSpeedProperty);
            set => SetValue(ZoomSpeedProperty, value);
        }

        /// <inheritdoc/>
        public StretchMode Stretch
        {
            get => (StretchMode)GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        /// <inheritdoc/>
        public double ZoomX => (double)GetValue(ZoomXProperty);

        /// <inheritdoc/>
        public double ZoomY => (double)GetValue(ZoomYProperty);

        /// <inheritdoc/>
        public double OffsetX => (double)GetValue(OffsetXProperty);

        /// <inheritdoc/>
        public double OffsetY => (double)GetValue(OffsetYProperty);

        /// <inheritdoc/>
        public bool EnableConstrains
        {
            get => (bool)GetValue(EnableConstrainsProperty);
            set => SetValue(EnableConstrainsProperty, value);
        }

        /// <inheritdoc/>
        public double MinZoomX
        {
            get => (double)GetValue(MinZoomXProperty);
            set => SetValue(MinZoomXProperty, value);
        }

        /// <inheritdoc/>
        public double MaxZoomX
        {
            get => (double)GetValue(MaxZoomXProperty);
            set => SetValue(MaxZoomXProperty, value);
        }

        /// <inheritdoc/>
        public double MinZoomY
        {
            get => (double)GetValue(MinZoomYProperty);
            set => SetValue(MinZoomYProperty, value);
        }

        /// <inheritdoc/>
        public double MaxZoomY
        {
            get => (double)GetValue(MaxZoomYProperty);
            set => SetValue(MaxZoomYProperty, value);
        }

        /// <inheritdoc/>
        public double MinOffsetX
        {
            get => (double)GetValue(MinOffsetXProperty);
            set => SetValue(MinOffsetXProperty, value);
        }

        /// <inheritdoc/>
        public double MaxOffsetX
        {
            get => (double)GetValue(MaxOffsetXProperty);
            set => SetValue(MaxOffsetXProperty, value);
        }

        /// <inheritdoc/>
        public double MinOffsetY
        {
            get => (double)GetValue(MinOffsetYProperty);
            set => SetValue(MinOffsetYProperty, value);
        }

        /// <inheritdoc/>
        public double MaxOffsetY
        {
            get => (double)GetValue(MaxOffsetYProperty);
            set => SetValue(MaxOffsetYProperty, value);
        }

        /// <inheritdoc/>
        public bool EnableInput
        {
            get => (bool)GetValue(EnableInputProperty);
            set => SetValue(EnableInputProperty, value);
        }

        /// <inheritdoc/>
        public bool EnableGestureZoom
        {
            get => (bool)GetValue(EnableGestureZoomProperty);
            set => SetValue(EnableGestureZoomProperty, value);
        }

        /// <inheritdoc/>
        public bool EnableGestureRotation
        {
            get => (bool)GetValue(EnableGestureRotationProperty);
            set => SetValue(EnableGestureRotationProperty, value);
        }

        /// <inheritdoc/>
        public bool EnableGestureTranslation
        {
            get => (bool)GetValue(EnableGestureTranslationProperty);
            set => SetValue(EnableGestureTranslationProperty, value);
        }

        /// <summary>
        /// Identifies the <seealso cref="PanButton"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PanButtonProperty =
            DependencyProperty.Register(
                nameof(PanButton),
                typeof(ButtonName),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(ButtonName.Left, FrameworkPropertyMetadataOptions.None));

        /// <summary>
        /// Identifies the <seealso cref="ZoomSpeed"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomSpeedProperty =
            DependencyProperty.Register(
                nameof(ZoomSpeed),
                typeof(double),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(1.2, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Identifies the <seealso cref="Stretch"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register(
                nameof(Stretch),
                typeof(StretchMode),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(StretchMode.Uniform, FrameworkPropertyMetadataOptions.AffectsArrange));

        internal static readonly DependencyPropertyKey ZoomXPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ZoomX),
                typeof(double),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.None));

        internal static readonly DependencyPropertyKey ZoomYPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ZoomY),
                typeof(double),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.None));

        internal static readonly DependencyPropertyKey OffsetXPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(OffsetX),
                typeof(double),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.None));

        internal static readonly DependencyPropertyKey OffsetYPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(OffsetY),
                typeof(double),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.None));

        /// <summary>
        /// Identifies the <seealso cref="ZoomX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomXProperty = ZoomXPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <seealso cref="ZoomY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomYProperty = ZoomYPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <seealso cref="OffsetX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OffsetXProperty = OffsetXPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <seealso cref="OffsetY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OffsetYProperty = OffsetYPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <seealso cref="EnableConstrains"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableConstrainsProperty =
            DependencyProperty.Register(
                nameof(EnableConstrains),
                typeof(bool),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Identifies the <seealso cref="MinZoomX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinZoomXProperty =
            DependencyProperty.Register(
                nameof(MinZoomX),
                typeof(double),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(double.NegativeInfinity, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Identifies the <seealso cref="MaxZoomX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxZoomXProperty =
            DependencyProperty.Register(
                nameof(MaxZoomX),
                typeof(double),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(double.PositiveInfinity, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Identifies the <seealso cref="MinZoomY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinZoomYProperty =
            DependencyProperty.Register(
                nameof(MinZoomY),
                typeof(double),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(double.NegativeInfinity, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Identifies the <seealso cref="MaxZoomY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxZoomYProperty =
            DependencyProperty.Register(
                nameof(MaxZoomY),
                typeof(double),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(double.PositiveInfinity, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Identifies the <seealso cref="MinOffsetX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinOffsetXProperty =
            DependencyProperty.Register(
                nameof(MinOffsetX),
                typeof(double),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(double.NegativeInfinity, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Identifies the <seealso cref="MaxOffsetX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxOffsetXProperty =
            DependencyProperty.Register(
                nameof(MaxOffsetX),
                typeof(double),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(double.PositiveInfinity, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Identifies the <seealso cref="MinOffsetY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinOffsetYProperty =
            DependencyProperty.Register(
                nameof(MinOffsetY),
                typeof(double),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(double.NegativeInfinity, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Identifies the <seealso cref="MaxOffsetY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxOffsetYProperty =
            DependencyProperty.Register(
                nameof(MaxOffsetY),
                typeof(double),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(double.PositiveInfinity, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Identifies the <seealso cref="EnableInput"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableInputProperty =
            DependencyProperty.Register(
                nameof(EnableInput),
                typeof(bool),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Identifies the <seealso cref="EnableGestureZoom"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableGestureZoomProperty =
            DependencyProperty.Register(
                nameof(EnableGestureZoom),
                typeof(bool),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Identifies the <seealso cref="EnableGestureRotation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableGestureRotationProperty =
            DependencyProperty.Register(
                nameof(EnableGestureRotation),
                typeof(bool),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Identifies the <seealso cref="EnableGestureTranslation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableGestureTranslationProperty =
            DependencyProperty.Register(
                nameof(EnableGestureTranslation),
                typeof(bool),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets single child of a <see cref="ZoomBorder"/> control.
        /// </summary>
        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                base.Child = value;
                ChildChanged(value);
            }
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

            Loaded += PanAndZoom_Loaded;
            Unloaded += PanAndZoom_Unloaded;

            IsManipulationEnabled = true;
        }

        private void Defaults()
        {
            _isPanning = false;
            _matrix = Matrix.Identity;
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
                    _element.RenderSize.Width,
                    _element.RenderSize.Height);
            }

            return size;
        }

        private void PanAndZoom_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Loaded: {this.Name}");
            ChildChanged(base.Child);
        }

        private void PanAndZoom_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Unloaded: {this.Name}");
            DetachElement();
        }

        private void Border_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (EnableInput)
            {
                Wheel(e);
            }
        }

        private void Border_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (EnableInput)
            {
                var button = PanButton;
                if ((e.ChangedButton == MouseButton.Left && button == ButtonName.Left)
                    || (e.ChangedButton == MouseButton.Right && button == ButtonName.Right)
                    || (e.ChangedButton == MouseButton.Middle && button == ButtonName.Middle))
                {
                    Pressed(e);
                }
            }
        }

        private void Border_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (EnableInput)
            {
                var button = PanButton;
                if ((e.ChangedButton == MouseButton.Left && button == ButtonName.Left)
                    || (e.ChangedButton == MouseButton.Right && button == ButtonName.Right)
                    || (e.ChangedButton == MouseButton.Middle && button == ButtonName.Middle))
                {
                    Released(e);
                }
            }
        }

        private void Border_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (EnableInput)
            {
                Moved(e);
            }
        }

        private void Border_ManipulationStarting(object? sender, ManipulationStartingEventArgs e)
        {
            if (EnableInput && _element != null)
            {
                e.ManipulationContainer = this;
            }
        }

        private void Border_ManipulationDelta(object? sender, ManipulationDeltaEventArgs e)
        {
            if (EnableInput && _element != null)
            {
                var deltaManipulation = e.DeltaManipulation;
                double scale = ((deltaManipulation.Scale.X - 1) * ZoomSpeed) + 1;
                var matrix = ((MatrixTransform)_element.RenderTransform).Matrix;
                Point center = new Point(_element.RenderSize.Width / 2, _element.RenderSize.Height / 2);
                center = matrix.Transform(center);

                if (EnableGestureZoom)
                {
                    matrix.ScaleAt(scale, scale, center.X, center.Y);
                }

                if (EnableGestureRotation)
                {
                    matrix.RotateAt(e.DeltaManipulation.Rotation, center.X, center.Y);
                }

                if (EnableGestureTranslation)
                {
                    matrix.Translate(e.DeltaManipulation.Translation.X, e.DeltaManipulation.Translation.Y);
                }

                 ((MatrixTransform)_element.RenderTransform).Matrix = matrix;
                e.Handled = true;
            }
        }

        private void ChildChanged(UIElement element)
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

        private void AttachElement(UIElement element)
        {
            if (element != null)
            {
                Defaults();
                _element = element;
                this.Focus();
                this.PreviewMouseWheel += Border_PreviewMouseWheel;
                this.PreviewMouseDown += Border_PreviewMouseDown;
                this.PreviewMouseUp += Border_PreviewMouseUp;
                this.PreviewMouseMove += Border_PreviewMouseMove;
                this.ManipulationStarting += Border_ManipulationStarting;
                this.ManipulationDelta += Border_ManipulationDelta;
            }
        }

        private void DetachElement()
        {
            if (_element != null)
            {
                this.PreviewMouseWheel -= Border_PreviewMouseWheel;
                this.PreviewMouseDown -= Border_PreviewMouseDown;
                this.PreviewMouseUp -= Border_PreviewMouseUp;
                this.PreviewMouseMove -= Border_PreviewMouseMove;
                this.ManipulationStarting -= Border_ManipulationStarting;
                this.ManipulationDelta -= Border_ManipulationDelta;
                _element.RenderTransform = null;
                _element = null;
                Defaults();
            }
        }

        private void Wheel(MouseWheelEventArgs e)
        {
            if (_element != null && Mouse.Captured == null)
            {
                Point point = e.GetPosition(_element);
                ZoomDeltaTo((double)e.Delta, point.X, point.Y);
            }
        }

        private void Pressed(MouseButtonEventArgs e)
        {
            if (_element != null && Mouse.Captured == null && _isPanning == false)
            {
                Point point = e.GetPosition(_element);
                StartPan(point.X, point.Y);
                _element.CaptureMouse();
                _isPanning = true;
            }
        }

        private void Released(MouseButtonEventArgs e)
        {
            if (_element != null && _element.IsMouseCaptured == true && _isPanning == true)
            {
                _element.ReleaseMouseCapture();
                _isPanning = false;
            }
        }

        private void Moved(MouseEventArgs e)
        {
            if (_element != null && _element.IsMouseCaptured == true && _isPanning == true)
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
            double offsetX = Constrain(_matrix.OffsetX, MinOffsetX, MaxOffsetX);
            double offsetY = Constrain(_matrix.OffsetY, MinOffsetY, MaxOffsetY);
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
                Debug.WriteLine($"Zoom: {_matrix.M11} {_matrix.M22} Offset: {_matrix.OffsetX} {_matrix.OffsetY}");
                SetValue(ZoomXPropertyKey, _matrix.M11);
                SetValue(ZoomYPropertyKey, _matrix.M22);
                SetValue(OffsetXPropertyKey, _matrix.OffsetX);
                SetValue(OffsetYPropertyKey, _matrix.OffsetY);
                this.InvalidatedChild?.Invoke(_matrix.M11, _matrix.M22, _matrix.OffsetX, _matrix.OffsetY);
                _element.RenderTransformOrigin = new Point(0, 0);
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
            double cx = elementWidth < panelWidth ? elementWidth / 2.0 : 0.0;
            double cy = elementHeight < panelHeight ? elementHeight / 2.0 : 0.0;
            double zoom = 1.0;
            switch (mode)
            {
                case StretchMode.Fill:
                    {
                        if (elementWidth > panelWidth && elementHeight > panelHeight)
                        {
                            cx = (panelWidth - (elementWidth * zx)) / 2.0;
                            cy = (panelHeight - (elementHeight * zy)) / 2.0;
                            var matrix = MatrixHelper.ScaleAt(zx, zy, 0.0, 0.0);
                            matrix.OffsetX = cx;
                            matrix.OffsetY = cy;
                            return matrix;
                        }
                        else
                        {
                            return MatrixHelper.ScaleAt(zx, zy, cx, cy);
                        }
                    }
                case StretchMode.Uniform:
                    {
                        zoom = Min(zx, zy);
                        if (elementWidth > panelWidth && elementHeight > panelHeight)
                        {
                            cx = (panelWidth - (elementWidth * zoom)) / 2.0;
                            cy = (panelHeight - (elementHeight * zoom)) / 2.0;
                            var matrix = MatrixHelper.ScaleAt(zoom, zoom, 0.0, 0.0);
                            matrix.OffsetX = cx;
                            matrix.OffsetY = cy;
                            return matrix;
                        }
                        else
                        {
                            return MatrixHelper.ScaleAt(zoom, zoom, cx, cy);
                        }
                    }
                case StretchMode.UniformToFill:
                    {
                        zoom = Max(zx, zy);
                        if (elementWidth > panelWidth && elementHeight > panelHeight)
                        {
                            cx = (panelWidth - (elementWidth * zoom)) / 2.0;
                            cy = (panelHeight - (elementHeight * zoom)) / 2.0;
                            var matrix = MatrixHelper.ScaleAt(zoom, zoom, 0.0, 0.0);
                            matrix.OffsetX = cx;
                            matrix.OffsetY = cy;
                            return matrix;
                        }
                        else
                        {
                            return MatrixHelper.ScaleAt(zoom, zoom, cx, cy);
                        }
                    }
            }
            return Matrix.Identity;
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
            _matrix = Matrix.Identity;
            Invalidate();
        }

        /// <inheritdoc/>
        public void Fill()
        {
            if (_element != null)
            {
                Fill(this.RenderSize.Width, this.RenderSize.Height, _element.RenderSize.Width, _element.RenderSize.Height); 
            }
        }

        /// <inheritdoc/>
        public void Uniform()
        {
            if (_element != null)
            {
                Uniform(this.RenderSize.Width, this.RenderSize.Height, _element.RenderSize.Width, _element.RenderSize.Height); 
            }
        }

        /// <inheritdoc/>
        public void UniformToFill()
        {
            if (_element != null)
            {
                UniformToFill(this.RenderSize.Width, this.RenderSize.Height, _element.RenderSize.Width, _element.RenderSize.Height); 
            }
        }

        /// <inheritdoc/>
        public void AutoFit()
        {
            if (_element != null)
            {
                AutoFit(this.RenderSize.Width, this.RenderSize.Height, _element.RenderSize.Width, _element.RenderSize.Height);
            }
        }
    }
}
